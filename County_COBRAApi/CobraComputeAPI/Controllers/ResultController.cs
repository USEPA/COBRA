using CobraCompute;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;

namespace CobraComputeAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ResultController : ControllerBase
    {
        private readonly CobraComputeCore computeCore;

        public ResultController(CobraComputeCore _computeCore)
        {
            computeCore = _computeCore;
        }

        [HttpGet("{token}")]
        public JsonResult Get(Guid token,[FromQuery] double discountrate = 3)
        {
            lock (computeCore)
            {
                computeCore.retrieve_userscenario(token);
                Cobra_Result result = new Cobra_Result();
                result.Impacts = computeCore.GetResults(discountrate);
                result.Summary = new Cobra_ResultSummary();
                Formatters.SummaryComposer("0", result);
                computeCore.store_userscenario();
                return new JsonResult(result, new JsonSerializerSettings() { Formatting = Formatting.Indented });
            }

        }

        [HttpGet("{token}/{filter}")]
        public JsonResult Get(Guid token, string filter = "0", [FromQuery] double discountrate = 3)
        {
            lock (computeCore)
            {
                computeCore.retrieve_userscenario(token);
                Cobra_Result result = new Cobra_Result();
                result.Impacts = computeCore.GetResults(discountrate);
                result.Summary = new Cobra_ResultSummary();
                Formatters.SummaryComposer(filter, result);
                computeCore.store_userscenario();
                return new JsonResult(result, new JsonSerializerSettings() { Formatting = Formatting.Indented });
            }

        }


    }
}
