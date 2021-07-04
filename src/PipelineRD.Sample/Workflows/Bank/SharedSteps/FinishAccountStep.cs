using System;

namespace PipelineRD.Sample.Workflows.Bank.SharedSteps
{
    public class FinishAccountStep : RequestStep<BankContext>, IFinishAccountStep
    {
        public override RequestStepResult HandleRequest()
        {
            Console.WriteLine("FinishAccountStep");

            return this.Finish(200);
        }
    }

    public interface IFinishAccountStep : IRequestStep<BankContext>
    {
    }
}
