using CobraCompute;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace CobraComputeAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SummarizedControlEmissionsController : ControllerBase
    {
        private readonly CobraComputeCore computeCore;

        public SummarizedControlEmissionsController(CobraComputeCore _computeCore)
        {
            computeCore = _computeCore;
        }

        [HttpGet]
        public async Task<JsonResult> Get([FromQuery] EmissionsDataRetrievalRequest requestparams)
        {
            var computeInstance = computeCore.CreateInstance();
            await computeInstance.retrieve_userscenario(requestparams.token);
            EmissionsSums result = computeInstance.SummarizeBaseControlEmissionsWithCriteria(computeCore.buildStringCriteria(requestparams));
            result = Formatters.forSummarizedControlEmissions(ref result);
            return new JsonResult(result, new JsonSerializerSettings() { Formatting = Formatting.Indented });
        }
    }
}