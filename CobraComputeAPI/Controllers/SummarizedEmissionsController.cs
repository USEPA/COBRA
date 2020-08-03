using CobraCompute;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace CobraComputeAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SummarizedEmissionsController : ControllerBase
    {
        private readonly CobraComputeCore computeCore;

        public SummarizedEmissionsController(CobraComputeCore _computeCore)
        {
            computeCore = _computeCore;
        }

        [HttpGet]
        public JsonResult Get([FromQuery]  EmissionsDataRetrievalRequest requestparams)
        {
            lock (computeCore)
            {
                computeCore.retrieve_userscenario(requestparams.token);
                EmissionsSums result = computeCore.SummarizeBaseControlEmissionsWithCriteria_resulttable(computeCore.buildStringCriteria(requestparams));
                return new JsonResult(result, new JsonSerializerSettings() { Formatting = Formatting.Indented });
            }
        }

    }
}
