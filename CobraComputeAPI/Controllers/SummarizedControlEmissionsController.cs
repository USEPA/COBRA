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
    public class SummarizedControlEmissionsController : ControllerBase
    {
        private readonly CobraComputeCore computeCore;

        public SummarizedControlEmissionsController(CobraComputeCore _computeCore)
        {
            computeCore = _computeCore;
        }

        // GET api/values
        [HttpGet]
        public JsonResult Get([FromQuery]  EmissionsDataRetrievalRequest requestparams)
        {
            lock (computeCore)
            {
                EmissionsSums result =  computeCore.SummarizeBaseControlEmissionsWithCriteria(requestparams.token, computeCore.buildStringCriteria(requestparams));
                result = Formatters.forSummarizedControlEmissions(ref result);
                return new JsonResult(result, new JsonSerializerSettings() { Formatting = Formatting.Indented });
            }
        }

    }
}
