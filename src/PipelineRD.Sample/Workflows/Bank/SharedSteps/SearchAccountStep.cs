using System;

namespace PipelineRD.Sample.Workflows.Bank.SharedSteps
{
    public class SearchAccountStep : RequestStep<BankContext>, ISearchAccountStep
    {
        public override RequestStepResult HandleRequest()
        {
            Console.WriteLine("SearchAccountStep");

            return this.Next();
        }
    }

    public interface ISearchAccountStep : IRequestStep<BankContext>
    {
    }
}