﻿using CobraCompute;
using Microsoft.AspNetCore.Mvc;
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


        [HttpGet]
        public JsonResult Get([FromQuery]  EmissionsDataRetrievalRequest requestparams)
        {
            lock (computeCore)
            {
                computeCore.retrieve_userscenario(requestparams.token);
                EmissionsSums result = computeCore.SummarizeBaseControlEmissionsWithCriteria(computeCore.buildStringCriteria(requestparams));
                result = Formatters.forSummarizedControlEmissions(ref result);
                return new JsonResult(result, new JsonSerializerSettings() { Formatting = Formatting.Indented });
            }
        }

    }
}
