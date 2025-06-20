using CobraCompute;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;

namespace CobraComputeAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ComputePMController : ControllerBase
    {
        private readonly CobraComputeCore computeCore;

        public ComputePMController(CobraComputeCore _computeCore)
        {
            computeCore = _computeCore;
        }


        [HttpPost]
        public JsonResult Post([FromBody] Emissions[] reqs)
        {
            List<double> final_pm = new List<double>();
            lock (computeCore)
            {
                foreach (var req in reqs)
                {
                    //took out req.nh3
                    final_pm.Add(computeCore.computePM(req.PM25, req.NOx, req.SOA, req.SO2, 1));
                }
            }
            return new JsonResult(final_pm.ToArray(), new JsonSerializerSettings() { Formatting = Formatting.Indented });
        }

        private class ArrayList<T>
        {
        }
    }
}