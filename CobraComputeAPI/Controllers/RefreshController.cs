using CobraCompute;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;

namespace CobraComputeAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RefreshController : ControllerBase
    {
        private readonly CobraComputeCore computeCore;

        public RefreshController(CobraComputeCore _computeCore)
        {
            computeCore = _computeCore;
        }

        [HttpGet]
        public JsonResult Get(Guid token)
        {
            lock (computeCore)
            {
                Guid _token = computeCore.Scenarios.renewUserScenario(token);
                return new JsonResult(new { value = _token }, new JsonSerializerSettings() { Formatting = Formatting.Indented });
            }
        }

    }
}
