using CobraCompute;
using Microsoft.AspNetCore.Mvc;
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

        [HttpGet]
        public JsonResult Get()
        {
            lock (computeCore)
            {
                return new JsonResult(computeCore.EmissionsInventory, new JsonSerializerSettings() { Formatting = Formatting.Indented });
            }
        }

    }
}
