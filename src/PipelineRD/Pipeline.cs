using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;

using Polly;

using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using PipelineRD.Cache;
using Serilog;
using Serilog.Context;

namespace PipelineRD;

public class Pipeline<TContext, TRequest> : IPipeline<TContext, TRequest> where TContext : BaseContext
{
    public Queue<Handler<TContext, TRequest>> Handlers { get; private set; }
    public TContext Context { get; private set; }
    private static string Identifier => $"Pipeline<{typeof(TContext).Name}, {typeof(TRequest).Name}>";

    private readonly IServiceProvider _serviceProvider;
    private string _requestKey;
    private ICacheProvider _cacheProvider;
    private bool _useCache;
    private string _currentHandlerIdentifier;

    public Pipeline(IServiceProvider serviceProvider, TContext context = null, string requestKey = null) : this()
    {
        _serviceProvider = serviceProvider;
        _requestKey = requestKey;
        Context = context ?? serviceProvider.GetService<TContext>() ?? throw new PipelineException($"{typeof(TContext).Name} is not configured.");
    }

    protected Pipeline()
    {
        Handlers = new Queue<Handler<TContext, TRequest>>();
    }

    public IPipeline<TContext, TRequest> EnableCache(ICacheProvider cacheProvider = null)
    {
        if (_serviceProvider.GetService<IDistributedCache>() == null)
        {
            throw new PipelineException("IDistributedCache interface is not injected.");
        }

        _cacheProvider = (cacheProvider ?? _serviceProvider.GetService<ICacheProvider>()) ?? throw new PipelineException($"Interface ICacheProvider is not configured.");
        _useCache = true;
        return this;
    }

    public IPipeline<TContext, TRequest> DisableCache()
    {
        _useCache = false;
        return this;
    }

    public IPipeline<TContext, TRequest> WithRequestKey(string requestKey)
    {
        _requestKey = requestKey;
        return this;
    }

    public async Task<HandlerResult> Execute(TRequest request)
        => await Execute(request, string.Empty, string.Empty);

    public async Task<HandlerResult> Execute(TRequest request, string idempotencyKey)
        => await Execute(request, idempotencyKey, string.Empty);

    public async Task<HandlerResult> Execute(TRequest request, string idempotencyKey, string initialHandlerIdentifier)
    {
        var hash = GetRequestHash(request, idempotencyKey);

        initialHandlerIdentifier ??= string.Empty;

        if (_useCache)
        {
            var snapshot = _cacheProvider.Get<PipelineSnapshot<TContext>>(hash);
            if (snapshot != null)
            {
                if (snapshot.Success)
                {
                    return snapshot.Context.Result;
                }
                else
                {
                    Context = snapshot.Context;
                    initialHandlerIdentifier = snapshot.HandlerIdentifier;
                }
            }
        }

        var result = await ExecutePipeline(request, initialHandlerIdentifier);

        if (_useCache)
        {
            PipelineSnapshot<TContext> snapshot = new(
                result.IsSuccess,
                _currentHandlerIdentifier,
                Context
            );

            _cacheProvider.Add(snapshot, hash);
        }

        return result;
    }

    private async Task<HandlerResult> ExecutePipeline(TRequest request, string initialHandlerIdentifier)
    {
        try
        {
            while (!IsFinished())
            {
                var handler = DequeueHandler();

                handler.DefineContext(Context);

                if (HandlerHasRecovery(handler))
                {
                    await ExecuteRecoveryHandler(handler);
                }

                if (ExecuteInOrderCheck() || ExecuteFromHandlerCheck(handler))
                {
                    await ExecuteHandler(handler);
                }
            };
        } 
        catch (Exception ex)
        {
            if (Log.Logger != null)
            {
                using (LogContext.PushProperty("RequestKey", _requestKey))
                {
                    Log.Logger.Error(ex, string.Concat("[PipelineRD] Error in the handler ", _currentHandlerIdentifier));
                }
            }

            Context.Result = HandlerResult.InternalServerError(new HandlerError()
            {
                Source = ex.Source,
                Message = ex.GetBaseException().Message,
            });
        }
        

        return GetResult();

        bool ExecuteInOrderCheck ()
            => initialHandlerIdentifier == string.Empty;

        bool ExecuteFromHandlerCheck(Handler<TContext, TRequest> handler)
        {
            var result = initialHandlerIdentifier == handler.Identifier;
            // Reset the initial handler identifier to execute the next steps
            // in order if not empty
            if (result)
                initialHandlerIdentifier = string.Empty;
            return result;
        }

        async Task ExecuteHandler(Handler<TContext, TRequest> handler)
        {
            // Execute step based on condition if defined
            if (handler.Condition is null || handler.Condition.Compile().Invoke(handler.Context, request))
            {
                if (handler.Policy != null)
                {
                    Context.Result = await handler.Policy.ExecuteAsync(async () =>
                    {
                        return await handler.Handle(request);
                    });
                }
                else
                {
                    Context.Result = await handler.Handle(request);
                }
            }
        }

        bool HandlerHasRecovery(Handler<TContext, TRequest> handler)
            => handler.RecoveryHandler != null;

        async Task ExecuteRecoveryHandler(Handler<TContext, TRequest> handler)
            => await handler.RecoveryHandler.Handle(request);
    }

    public string GetRequestHash(TRequest request, string idempotencyKey)
    {
        return string.IsNullOrEmpty(idempotencyKey) ?
            GenerateRequestHash(request) :
            idempotencyKey;

        static string GenerateRequestHash(TRequest request)
        {
            var requestString = $"{Identifier}: {RequestToString(request)}";
            var encoding = new ASCIIEncoding();
            var key = encoding.GetBytes("072e77e426f92738a72fe23c4d1953b4");
            var hmac = new HMACSHA1(key);
            var bytes = hmac.ComputeHash(encoding.GetBytes(requestString));
            return Convert.ToBase64String(bytes);
        }

        static string RequestToString(TRequest request)
            => JsonSerializer.Serialize(request);
    }

    public IPipeline<TContext, TRequest> When(Expression<Func<TContext, TRequest, bool>> condition)
    {
        var step = Handlers.LastOrDefault();
        if (step != null)
        {
            step.DefineConditionToExecution(condition);
        }
        return this;
    }

    public IPipeline<TContext, TRequest> WithHandler<THandler>() where THandler : Handler<TContext, TRequest>
    {
        var handler = _serviceProvider.GetService<THandler>() ?? throw new PipelineException($"{typeof(THandler).Name} not found in the dependency container.");
        return WithHandler(handler);
    }

    public IPipeline<TContext, TRequest> WithHandler(Handler<TContext, TRequest> handler)
    {
        handler.DefineContext(Context);
        Handlers.Enqueue(handler);
        return this;
    }

    public IPipeline<TContext, TRequest> WithPolicy(AsyncPolicy<HandlerResult> policy)
    {
        var step = Handlers.LastOrDefault();
        if (policy != null && step != null)
        {
            step.DefinePolicy(policy);
        }

        return this;
    }

    public IPipeline<TContext, TRequest> WithRecovery<TRecoveryHandler>() where TRecoveryHandler : RecoveryHandler<TContext, TRequest>
    {
        var handler = _serviceProvider.GetService<TRecoveryHandler>() ?? throw new PipelineException($"Recovery {typeof(TRecoveryHandler).Name} not found in the dependency container.");
        return WithRecovery(handler);
    }

    public IPipeline<TContext, TRequest> WithRecovery(RecoveryHandler<TContext, TRequest> recoveryHandler)
    {
        var handler = Handlers.LastOrDefault();
        if (recoveryHandler != null && handler != null)
        {
            recoveryHandler.DefineContext(Context);
            handler.DefineRecovery(recoveryHandler);
        }

        return this;
    }

    private HandlerResult GetResult()
     => Context.Result ?? HandlerResult.NoResult();

    private bool IsFinished()
        => !Handlers.Any() || Context.Result != null;

    private Handler<TContext, TRequest> DequeueHandler()
    {
        var handler = Handlers.Dequeue();
        _currentHandlerIdentifier = handler.Identifier;
        return handler;
    }
}
