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
    public class SummarizedBaseEmissionsController : ControllerBase
    {
        private readonly CobraComputeCore computeCore;

        public SummarizedBaseEmissionsController(CobraComputeCore _computeCore)
        {
            computeCore = _computeCore;
        }

        // GET api/values
        [HttpPost]
        public JsonResult Post([FromBody] EmissionsDataRetrievalRequest requestparams)
        {
            return new JsonResult(computeCore.SummarizedEmissionsInventory, new JsonSerializerSettings() { Formatting = Formatting.Indented });
        }

    }
}
