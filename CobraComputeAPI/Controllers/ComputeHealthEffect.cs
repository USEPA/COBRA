using CobraCompute;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Collections.Generic;

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
        public JsonResult Post([FromBody]  ImpactComputeRequest req)
        {
            List<Cobra_ResultDetail> result;
            lock (computeCore)
            {
                result = computeCore.ComputeGenericImpacts(req.delta_pm, req.base_pm, req.control_pm, req.population, req.incidence, req.valat3);
            }
            return new JsonResult(result, new JsonSerializerSettings() { Formatting = Formatting.Indented });
        }

    }
}