using CobraCompute;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

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
        public async Task<JsonResult> Get(Guid token)
        {
            var computeInstance = computeCore.CreateInstance();
            await computeInstance.retrieve_userscenario(token);
            var result = computeInstance.GetControlEmissions("");
            return new JsonResult(result, new JsonSerializerSettings() { Formatting = Formatting.Indented });
        }

        [HttpGet("{token}/{criteria}")]
        public async Task<JsonResult> Get(Guid token, string criteria)
        {
            var computeInstance = computeCore.CreateInstance();
            await computeInstance.retrieve_userscenario(token);
            var result = computeInstance.GetControlEmissions(criteria);
            return new JsonResult(result, new JsonSerializerSettings() { Formatting = Formatting.Indented });
        }

        [HttpPost]
        public async Task<StatusCodeResult> Post([FromBody] CobraUpdateBundle bundle)
        {
            var computeInstance = computeCore.CreateInstance();
            await computeInstance.retrieve_userscenario(bundle.token);
            computeInstance.SetControlEmissions(bundle.emissions);
            await computeInstance.store_userscenario();
            return Ok();
        }
    }
}