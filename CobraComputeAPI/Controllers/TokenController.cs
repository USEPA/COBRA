using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CobraCompute;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

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

        // GET api/values
        [HttpGet]
        public JsonResult Get()
        {
            if (computeCore.initilized)
            {

                Guid _token = computeCore.createUserScenario();
                computeCore.FlushUserScenarios();
                return new JsonResult(new { value = _token }, new JsonSerializerSettings() { Formatting = Formatting.Indented });
            } else
            {
                //case is important in container deployments 
                computeCore.initialize();
                return new JsonResult(new { value = "initializing" }, new JsonSerializerSettings() { Formatting = Formatting.Indented });
            }
                 
        }

        // DELETE api/values/5
        [HttpDelete("{token}")]
        public StatusCodeResult Delete(Guid token)
        {
            computeCore.deleteUserScenario(token);
            return Ok();
        }
    }
}
