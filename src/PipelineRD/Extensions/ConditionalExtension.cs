using System;

namespace PipelineRD.Extensions
{
    public static class ConditionalExtension
    {
        public static bool IsSatisfied<TContext>(this Func<TContext, bool> condition, TContext context)
            => condition.Invoke(context);
    }
}