using FluentValidation;

using Polly;

using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace PipelineRD
{
    public interface IPipeline<TContext> where TContext : BaseContext
    {
        TContext Context { get; }
        string CurrentRequestStepIdentifier { get; }
        string Identifier { get; }
        IReadOnlyCollection<IRequestStep<TContext>> Steps { get; }

        #region RecoveryHash
        IPipeline<TContext> EnableRecoveryRequestByHash();
        IPipeline<TContext> DisableRecoveryRequestByHash();
        #endregion

        #region Execute
        RequestStepResult Execute<TRequest>(TRequest request) where TRequest : IPipelineRequest;
        RequestStepResult Execute<TRequest>(TRequest request, string idempotencyKey) where TRequest : IPipelineRequest;
        RequestStepResult ExecuteFromSpecificRequestStep(string requestStepIdentifier);
        RequestStepResult ExecuteNextRequestStep();
        #endregion

        #region AddNext
        IPipeline<TContext> AddNext<TRequestStep>() where TRequestStep : IRequestStep<TContext>;
        #endregion

        #region AddValidator
        IPipeline<TContext> AddValidator<TRequest>(IValidator<TRequest> validator) where TRequest : IPipelineRequest;
        IPipeline<TContext> AddValidator<TRequest>() where TRequest : IPipelineRequest;
        #endregion

        #region AddPolicy
        IPipeline<TContext> WithPolicy(Policy<RequestStepResult> policy);
        #endregion

        #region AddCondition
        IPipeline<TContext> When(Expression<Func<TContext, bool>> condition);
        IPipeline<TContext> When<TCondition>();
        #endregion

        #region AddRollback
        IPipeline<TContext> AddRollback(IRollbackRequestStep<TContext> rollbackStep);
        IPipeline<TContext> AddRollback<TRollbackRequestStep>() where TRollbackRequestStep : IRollbackRequestStep<TContext>;
        #endregion

        #region AddFinally
        IPipeline<TContext> AddFinally<TRequestStep>() where TRequestStep : IRequestStep<TContext>;
        #endregion

        void ExecuteRollback();
    }
}
