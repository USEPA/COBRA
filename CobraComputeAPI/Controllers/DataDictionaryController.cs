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
    public class DataDictionaryController : ControllerBase
    {
        private readonly CobraComputeCore computeCore;

        public DataDictionaryController(CobraComputeCore _computeCore)
        {
            computeCore = _computeCore;
        }

        // GET api/values
        [HttpGet]
        public JsonResult Get(string which)
        {
            JsonResult result;
            switch (which.ToLower())
            {
                case "tiers":
                    result = new JsonResult(computeCore.dict_tier, new JsonSerializerSettings() { Formatting = Formatting.Indented });
                    break;
                default:
                    result = new JsonResult(computeCore.dict_state, new JsonSerializerSettings() { Formatting = Formatting.Indented });
                    break;
            }
            return result;
        }


    }
}
