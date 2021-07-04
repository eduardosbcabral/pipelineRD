using System;
using System.Linq.Expressions;

namespace PipelineRD
{
    public interface ICondition<TContext> where TContext : BaseContext
    {
        Expression<Func<TContext, bool>> When();
    }
}