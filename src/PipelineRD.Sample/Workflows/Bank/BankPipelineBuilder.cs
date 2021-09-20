using PipelineRD.Sample.Models;
using PipelineRD.Sample.Workflows.Bank.AccountSteps;
using PipelineRD.Sample.Workflows.Bank.SharedSteps;

using Polly;

using System;
using System.Threading.Tasks;

namespace PipelineRD.Sample.Workflows.Bank
{
    public class BankPipelineBuilder : IBankPipelineBuilder
    {
        public IPipelineInitializer<BankContext> Pipeline { get; }

        public BankPipelineBuilder(IPipelineInitializer<BankContext> pipeline)
        {
            Pipeline = pipeline;
        }

        public async Task<RequestStepResult> CreateAccount(CreateAccountModel model)
        {
            var requestKey = Guid.NewGuid().ToString();
            return await Pipeline
                .Initialize(requestKey)
                .EnableRecoveryRequestByHash()
                .AddNext<IFinishAccountStep>()
                .Execute(model);
        }

        public async Task<RequestStepResult> DepositAccount(DepositAccountModel model)
        {
            return await Pipeline
                .Initialize()
                .AddNext<ISearchAccountStep>()
                .AddNext<ISearchAccountStep>()
                .AddNext<IDepositAccountStep>()
                    .When(b => b.Id == "test")
                .AddNext<IFinishAccountStep>()
                .Execute(model);
        }
    }

    public interface IBankPipelineBuilder : IPipelineBuilder<BankContext>
    {
        Task<RequestStepResult> CreateAccount(CreateAccountModel model);
        Task<RequestStepResult> DepositAccount(DepositAccountModel model);
    }
}