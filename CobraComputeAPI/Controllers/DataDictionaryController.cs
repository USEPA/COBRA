using CobraCompute;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace CobraComputeAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DataDictionaryController : ControllerBase
    {
        private readonly CobraComputeCore computeCore;

        public DataDictionaryController(CobraComputeCore _computeCore)
        {
            computeCore = _computeCore;
        }

        [HttpGet]
        public JsonResult Get(string which)
        {
            JsonResult result;
            switch (which.ToLower())
            {
                case "tiers":
                    result = new JsonResult(computeCore.dict_tier, new JsonSerializerSettings() { Formatting = Formatting.Indented });
                    break;
                default:
                    result = new JsonResult(computeCore.dict_state, new JsonSerializerSettings() { Formatting = Formatting.Indented });
                    break;
            }
            return result;
        }


    }
}
