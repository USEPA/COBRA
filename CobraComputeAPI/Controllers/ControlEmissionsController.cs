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
    public class ControlEmissionsController : ControllerBase
    {
        private readonly CobraComputeCore computeCore;

        public ControlEmissionsController(CobraComputeCore _computeCore)
        {
            computeCore = _computeCore;
        }

        // GET api/values
        [HttpGet("{token}")]
        public JsonResult Get(Guid token)
        {
            return new JsonResult(computeCore.GetControlEmissions(token,""), new JsonSerializerSettings() { Formatting = Formatting.Indented });
        }

        [HttpGet("{token}/{criteria}")]
        public JsonResult Get(Guid token, string criteria)
        {
            return new JsonResult(computeCore.GetControlEmissions(token, criteria), new JsonSerializerSettings() { Formatting = Formatting.Indented });
        }

        // POST api/values
        [HttpPost]
        public StatusCodeResult Post([FromBody] CobraUpdateBundle bundle)
        {
            computeCore.SetControlEmissions(bundle.token, bundle.emissions);
            return Ok();
        }



    }
}
