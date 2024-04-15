using CobraCompute;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;

namespace CobraComputeAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ComputeO3Controller : ControllerBase
    {
        private readonly CobraComputeCore computeCore;

        public ComputeO3Controller(CobraComputeCore _computeCore)
        {
            computeCore = _computeCore;
        }


        [HttpPost]
        public JsonResult Post([FromBody] Emissions[] reqs)
        {
            List<double> final_O3 = new List<double>();
            lock (computeCore)
            {
                foreach (var req in reqs)
                {
                    final_O3.Add(computeCore.computeO3(req.VOC, req.O3N));
                }
            }
            return new JsonResult(final_O3.ToArray(), new JsonSerializerSettings() { Formatting = Formatting.Indented });
        }

        private class ArrayList<T>
        {
        }
    }
}