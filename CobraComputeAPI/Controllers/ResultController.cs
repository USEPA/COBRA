using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CobraCompute;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using Newtonsoft.Json;

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
        public JsonResult Get(Guid token)
        {
            lock (computeCore)
            {
                Cobra_Result result = new Cobra_Result();


                result.Impacts = computeCore.GetResults(token);
                result.Summary = new Cobra_ResultSummary();
                Formatters.SummaryComposer(0, result);

                return new JsonResult(result, new JsonSerializerSettings() { Formatting = Formatting.Indented });
            }

        }

        [HttpGet("{token}/{filter}")]
        public JsonResult Get(Guid token, int filter=0)
        {
            lock (computeCore)
            {
                Cobra_Result result = new Cobra_Result();


                result.Impacts = computeCore.GetResults(token);
                result.Summary = new Cobra_ResultSummary();
                Formatters.SummaryComposer(filter, result);

                return new JsonResult(result, new JsonSerializerSettings() { Formatting = Formatting.Indented });
            }

        }


    }
}
