﻿using Polly;

using System.Linq.Expressions;
using System.Net;

namespace PipelineRD;

public abstract class Handler<TContext, TRequest> where TContext : BaseContext
{
    public TContext Context { get; private set; }
    public Expression<Func<TContext, TRequest, bool>> Condition { get; private set; }
    public AsyncPolicy<HandlerResult> Policy { get; private set; }
    public RecoveryHandler<TContext, TRequest> RecoveryHandler { get; private set; }
    public string Identifier => $"({typeof(TContext).Name}, {typeof(TRequest).Name}).{GetType().Name}";
    public HandlerResult Result => Context.Result;

    public abstract Task<HandlerResult> Handle(TRequest request);

    protected Task<HandlerResult> Proceed()
        => Task.FromResult<HandlerResult>(null);

    protected Task<HandlerResult> Abort(string errorMessage, int httpStatusCode)
        => Task.FromResult(HandlerResultBuilder.CreateDefault()
            .WithError(new(errorMessage))
            .WithStatusCode((HttpStatusCode)httpStatusCode)
            .WithLastHandler(Identifier));

    protected Task<HandlerResult> Abort(string errorMessage, HttpStatusCode httpStatusCode)
        => Task.FromResult(HandlerResultBuilder.CreateDefault()
            .WithError(new(errorMessage))
            .WithStatusCode(httpStatusCode)
            .WithLastHandler(Identifier));

    protected Task<HandlerResult> Abort(HandlerError error, HttpStatusCode httpStatusCode)
        => Task.FromResult(HandlerResultBuilder.CreateDefault()
            .WithError(error)
            .WithStatusCode(httpStatusCode)
            .WithLastHandler(Identifier));

    protected Task<HandlerResult> Abort(HandlerError error, int httpStatusCode)
        => Task.FromResult(HandlerResultBuilder.CreateDefault()
            .WithError(error)
            .WithStatusCode((HttpStatusCode)httpStatusCode)
            .WithLastHandler(Identifier));

    protected Task<HandlerResult> Abort(IEnumerable<HandlerError> errors, HttpStatusCode httpStatusCode)
        => Task.FromResult(HandlerResultBuilder.CreateDefault()
            .WithErrors(errors)
            .WithStatusCode(httpStatusCode)
            .WithLastHandler(Identifier));

    protected Task<HandlerResult> Abort(IEnumerable<HandlerError> errors, int httpStatusCode)
        => Task.FromResult(HandlerResultBuilder.CreateDefault()
            .WithErrors(errors)
            .WithStatusCode((HttpStatusCode)httpStatusCode)
            .WithLastHandler(Identifier));

    protected Task<HandlerResult> Finish(object result, HttpStatusCode httpStatusCode)
        => Task.FromResult(HandlerResultBuilder.CreateDefault()
            .WithResult(result)
            .WithStatusCode(httpStatusCode)
            .WithLastHandler(Identifier));

    protected Task<HandlerResult> Finish(object result, int httpStatusCode)
        => Finish(result, (HttpStatusCode)httpStatusCode);

    protected Task<HandlerResult> Finish(HttpStatusCode httpStatusCode)
        => Finish(null, httpStatusCode);

    protected Task<HandlerResult> Finish(int httpStatusCode)
        => Finish(null, (HttpStatusCode)httpStatusCode);

    public void DefineContext(TContext context)
        => Context = context;

    public void DefineConditionToExecution(Expression<Func<TContext, TRequest, bool>> condition)
        => Condition = condition;

    public void DefinePolicy(AsyncPolicy<HandlerResult> policy)
        => Policy = policy;

    public void DefineRecovery(RecoveryHandler<TContext, TRequest> recoveryHandler)
        => RecoveryHandler = recoveryHandler;
}
