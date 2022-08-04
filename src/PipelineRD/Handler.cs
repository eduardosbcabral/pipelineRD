﻿using Polly;

using System.Linq.Expressions;
using System.Net;

namespace PipelineRD;

public abstract class Handler<TContext, TRequest> where TContext : BaseContext
{
    public TContext Context { get; private set; }
    public Expression<Func<TContext, bool>> Condition { get; private set; }
    public Policy<HandlerResult> Policy { get; private set; }
    public RecoveryHandler<TContext, TRequest> RecoveryHandler { get; private set; }
    public string Identifier => $"({typeof(TContext).Name}, {typeof(TRequest).Name}).{GetType().Name}";
    public HandlerResult Result => Context.Result;

    public abstract void Handle(TRequest request);

    protected void Abort(string errorMessage, int httpStatusCode)
        => Context.Result = HandlerResultBuilder.CreateDefault()
            .WithError(new(errorMessage))
            .WithStatusCode((HttpStatusCode)httpStatusCode)
            .WithLastHandler(Identifier);

    protected void Abort(string errorMessage, HttpStatusCode httpStatusCode)
        => Context.Result = HandlerResultBuilder.CreateDefault()
            .WithError(new(errorMessage))
            .WithStatusCode(httpStatusCode)
            .WithLastHandler(Identifier);

    protected void Abort(HandlerError error, HttpStatusCode httpStatusCode)
        => Context.Result = HandlerResultBuilder.CreateDefault()
            .WithError(error)
            .WithStatusCode(httpStatusCode)
            .WithLastHandler(Identifier);

    protected void Abort(HandlerError error, int httpStatusCode)
        => Context.Result = HandlerResultBuilder.CreateDefault()
            .WithError(error)
            .WithStatusCode((HttpStatusCode)httpStatusCode)
            .WithLastHandler(Identifier);

    protected void Abort(IEnumerable<HandlerError> errors, HttpStatusCode httpStatusCode)
        => Context.Result = HandlerResultBuilder.CreateDefault()
            .WithErrors(errors)
            .WithStatusCode(httpStatusCode)
            .WithLastHandler(Identifier);

    protected void Abort(IEnumerable<HandlerError> errors, int httpStatusCode)
        => Context.Result = HandlerResultBuilder.CreateDefault()
            .WithErrors(errors)
            .WithStatusCode((HttpStatusCode)httpStatusCode)
            .WithLastHandler(Identifier);

    protected void Finish(object result, HttpStatusCode httpStatusCode)
        => Context.Result = HandlerResultBuilder.CreateDefault()
            .WithResult(result)
            .WithStatusCode(httpStatusCode)
            .WithLastHandler(Identifier);

    protected void Finish(object result, int httpStatusCode)
        => Finish(result, (HttpStatusCode)httpStatusCode);

    protected void Finish(HttpStatusCode httpStatusCode)
        => Finish(null, httpStatusCode);

    protected void Finish(int httpStatusCode)
        => Finish(null, (HttpStatusCode)httpStatusCode);

    public void DefineContext(TContext context)
        => Context = context;

    public void DefineConditionToExecution(Expression<Func<TContext, bool>> condition)
        => Condition = condition;

    public void DefinePolicy(Policy<HandlerResult> policy)
        => Policy = policy;

    public void DefineRecovery(RecoveryHandler<TContext, TRequest> recoveryHandler)
        => RecoveryHandler = recoveryHandler;
}