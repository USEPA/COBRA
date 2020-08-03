using CobraCompute;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;

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

        [HttpGet("{token}")]
        public JsonResult Get(Guid token)
        {
            lock (computeCore)
            {
                computeCore.retrieve_userscenario(token);
                return new JsonResult(computeCore.GetControlEmissions(""), new JsonSerializerSettings() { Formatting = Formatting.Indented });
            }
        }

        [HttpGet("{token}/{criteria}")]
        public JsonResult Get(Guid token, string criteria)
        {
            lock (computeCore)
            {
                computeCore.retrieve_userscenario(token);
                return new JsonResult(computeCore.GetControlEmissions(criteria), new JsonSerializerSettings() { Formatting = Formatting.Indented });
            }
        }

        [HttpPost]
        public StatusCodeResult Post([FromBody] CobraUpdateBundle bundle)
        {
            lock (computeCore)
            {
                computeCore.retrieve_userscenario(bundle.token);
                computeCore.SetControlEmissions(bundle.emissions);
                computeCore.store_userscenario();
                return Ok();
            }
        }



    }
}
