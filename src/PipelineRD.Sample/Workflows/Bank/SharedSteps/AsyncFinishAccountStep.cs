using PipelineRD.Async;

using System;
using System.Threading.Tasks;

namespace PipelineRD.Sample.Workflows.Bank.SharedSteps
{
    public class AsyncFinishAccountStep : AsyncRequestStep<BankContext>, IAsyncFinishAccountStep
    {
        public override async Task<RequestStepResult> HandleRequest()
        {
            Console.WriteLine("FinishAccountStep");
            return this.Finish(200);
        }
    }

    public interface IAsyncFinishAccountStep : IAsyncRequestStep<BankContext>
    {
    }
}
