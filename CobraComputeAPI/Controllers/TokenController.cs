using CobraCompute;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;

namespace CobraComputeAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TokenController : ControllerBase
    {
        private readonly CobraComputeCore computeCore;

        public TokenController(CobraComputeCore _computeCore)
        {
            computeCore = _computeCore;
        }

        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public JsonResult Get()
        {
            lock (computeCore)
            {
                if (computeCore.initilized)
                {

                    Guid _token = computeCore.Scenarios.createUserScenario();
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
                    return new JsonResult(new { value = _token }, new JsonSerializerSettings() { Formatting = Formatting.Indented });
                }
                else
                {
                    computeCore.initialize();
                    return new JsonResult(new { value = "initializing" }, new JsonSerializerSettings() { Formatting = Formatting.Indented });
                }
            }
        }

        [HttpDelete("{token}")]
        public StatusCodeResult Delete(Guid token)
        {
            lock (computeCore)
            {
                computeCore.Scenarios.deleteUserScenario(token);
                return Ok();
            }
        }
    }
}
