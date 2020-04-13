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
    public class BaseEmissionsController : ControllerBase
    {
        private readonly CobraComputeCore computeCore;

        public BaseEmissionsController(CobraComputeCore _computeCore)
        {
            computeCore = _computeCore;
        }

        // GET api/values
        [HttpGet]
        public JsonResult Get()
        {
            return new JsonResult(computeCore.EmissionsInventory, new JsonSerializerSettings() { Formatting = Formatting.Indented });
        }

    }
}
