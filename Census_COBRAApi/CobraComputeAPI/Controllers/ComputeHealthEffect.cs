using CobraCompute;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CobraComputeAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ComputeHealthEffect : ControllerBase
    {
        private readonly CobraComputeCore computeCore;

        public ComputeHealthEffect(CobraComputeCore _computeCore)
        {
            computeCore = _computeCore;
        }

        [HttpPost]
        public async Task<JsonResult> Post([FromBody] CustomImpactComputeRequest req)
        {
            List<Custom_ResultDetail> result;
            var computeInstance = computeCore.CreateInstance();

            // Perform the computation asynchronously
            result = await Task.Run(() => computeInstance.CustomComputeGenericImpacts(
                req.delta_pm, req.base_pm, req.control_pm, req.population,
                req.incidence, req.valat3, req.CustomCRFunctions, req.CustomValuationFunctions));

            return new JsonResult(result, new JsonSerializerSettings() { Formatting = Formatting.Indented, Converters = new List<JsonConverter> { new Custom_ResultDetailConverter() } });
        }
    }
}