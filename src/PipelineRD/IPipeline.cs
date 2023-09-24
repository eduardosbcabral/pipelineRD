using PipelineRD.Cache;

using Polly;

using System.Linq.Expressions;

namespace PipelineRD;

public interface IPipeline<TContext, TRequest> where TContext : BaseContext
{
    Queue<Handler<TContext, TRequest>> Handlers { get; }
    TContext Context { get; }

    IPipeline<TContext, TRequest> EnableCache(ICacheProvider cacheProvider = null);

    IPipeline<TContext, TRequest> WithHandler<THandler>() where THandler : Handler<TContext, TRequest>;
    IPipeline<TContext, TRequest> WithHandler(Handler<TContext, TRequest> handler);
    IPipeline<TContext, TRequest> WithRecovery<THandler>() where THandler : RecoveryHandler<TContext, TRequest>;
    IPipeline<TContext, TRequest> WithRecovery(RecoveryHandler<TContext, TRequest> handler);
    IPipeline<TContext, TRequest> When(Expression<Func<TContext, TRequest, bool>> condition);
    IPipeline<TContext, TRequest> WithPolicy(AsyncPolicy<HandlerResult> policy);
    Task<HandlerResult> Execute(TRequest request);
    Task<HandlerResult> Execute(TRequest request, string idempotencyKey);

    string GetRequestHash(TRequest request, string idempotencyKey);
}
