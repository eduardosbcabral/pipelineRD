using Polly;

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace PipelineRD
{
    public interface IPipeline<TContext> where TContext : BaseContext
    {
        TContext Context { get; }
        string CurrentRequestStepIdentifier { get; }
        string Identifier { get; }
        IReadOnlyCollection<IStep<TContext>> Steps { get; }
        void SetRequestKey(string requestKey);
        IServiceProvider GetServiceProvider();

        #region RecoveryHash
        IPipeline<TContext> EnableRecoveryRequestByHash();
        IPipeline<TContext> DisableRecoveryRequestByHash();
        #endregion

        #region Execute
        Task<RequestStepResult> Execute<TRequest>(TRequest request) where TRequest : IPipelineRequest;
        Task<RequestStepResult> Execute<TRequest>(TRequest request, string idempotencyKey) where TRequest : IPipelineRequest;
        #endregion

        #region AddNext
        IPipeline<TContext> AddNext<TRequestStep>() where TRequestStep : IStep<TContext>;
        IPipeline<TContext> AddNext<TRequestStep>(IStep<TContext> step) where TRequestStep : IStep<TContext>;
        #endregion

        #region AddPolicy
        IPipeline<TContext> WithPolicy(Policy<RequestStepResult> policy);
        IPipeline<TContext> WithPolicy(AsyncPolicy<RequestStepResult> policy);
        #endregion

        #region AddCondition
        IPipeline<TContext> When(Expression<Func<TContext, bool>> condition);
        IPipeline<TContext> When<TCondition>() where TCondition : ICondition<TContext>;
            #endregion

            #region AddRollback
        IPipeline<TContext> AddRollback(IRollbackStep<TContext> rollbackStep);
        IPipeline<TContext> AddRollback<TRollbackRequestStep>() where TRollbackRequestStep : IRollbackStep<TContext>;
        #endregion

        #region AddFinally
        IPipeline<TContext> AddFinally<TRequestStep>() where TRequestStep : IStep<TContext>;
        #endregion
    }
}
