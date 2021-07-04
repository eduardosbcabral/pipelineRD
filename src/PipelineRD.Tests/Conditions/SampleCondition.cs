using System;
using System.Linq.Expressions;

namespace PipelineRD.Tests.Conditions
{
    public class SampleCondition : ISampleCondition
    {
        public Expression<Func<ContextSample, bool>> When()
            => x => x.ValidSecond == true;
    }

    public interface ISampleCondition : ICondition<ContextSample>
    { }
}
