using CobraCompute;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

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
        public async Task<JsonResult> Get(Guid token)
        {

                Guid _token = await computeCore.Scenarios.renewUserScenario(token);
                return new JsonResult(new { value = _token }, new JsonSerializerSettings() { Formatting = Formatting.Indented });
            
        }

    }
}
