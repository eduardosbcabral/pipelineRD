using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace PipelineRD.Internals
{
    internal static class ExpressionExtensions
    {
        private static readonly ConcurrentDictionary<ExpressionCacheKey, Delegate> compiledExpressionsCache = new ConcurrentDictionary<ExpressionCacheKey, Delegate>();

        public static bool IsSatisfied<TContext, TRequest>(this Expression<Func<TContext, TRequest, bool>> condition, TContext context, TRequest request)
        {
            var expressionKey = new ExpressionCacheKey(condition);
            var compiledExpression = (Func<TContext, TRequest, bool>)compiledExpressionsCache.GetOrAdd(expressionKey, _ => condition.Compile());

            return compiledExpression(context, request);
        }
    }
}
