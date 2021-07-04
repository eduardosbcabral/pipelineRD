using Microsoft.AspNetCore.Mvc;

using PipelineRD.Sample.Models;
using PipelineRD.Sample.Workflows.Bank;

namespace PipelineRD.Sample.Controllers
{
    [Route("bank")]
    public class BankController : ControllerBase
    {
        private readonly IBankPipelineBuilder _bankPipelineBuilder;

        public BankController(IBankPipelineBuilder bankPipelineBuilder)
        {
            _bankPipelineBuilder = bankPipelineBuilder;
        }

        public IActionResult Get()
        {
            var request = new CreateAccountModel();
            var result = _bankPipelineBuilder.CreateAccount(request);
            return Ok(result);
        }
    }
}
