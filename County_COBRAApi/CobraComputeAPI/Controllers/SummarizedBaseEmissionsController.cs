using CobraCompute;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace CobraComputeAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SummarizedBaseEmissionsController : ControllerBase
    {
        private readonly CobraComputeCore computeCore;

        public SummarizedBaseEmissionsController(CobraComputeCore _computeCore)
        {
            computeCore = _computeCore;
        }


        [HttpPost]
        public JsonResult Post([FromBody] EmissionsDataRetrievalRequest requestparams)
        {
            lock (computeCore)
            {
                return new JsonResult(computeCore.SummarizedEmissionsInventory, new JsonSerializerSettings() { Formatting = Formatting.Indented });
            }
        }

    }
}
