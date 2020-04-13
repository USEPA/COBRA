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
            Guid _token = computeCore.renewUserScenario(token);
            return new JsonResult(new { value = _token }, new JsonSerializerSettings() { Formatting = Formatting.Indented });
        }

    }
}
