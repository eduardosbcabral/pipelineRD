using PipelineRD.Sample.Models;

using System;

namespace PipelineRD.Sample.Workflows.Bank.AccountSteps
{
    public class CreateAccountStep : RequestStep<BankContext>, ICreateAccountStep
    {
        public override RequestStepResult HandleRequest()
        {
            var request = this.Request<CreateAccountModel>();

            Console.WriteLine("CreateAccountStep");
            
            return this.Next();
        }
    }

    public interface ICreateAccountStep : IRequestStep<BankContext>
    {
    }
}