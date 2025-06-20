using CobraCompute;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Linq;

namespace CobraComputeAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TribeController : ControllerBase
    {
        private readonly CobraComputeCore computeCore;

        public TribeController(CobraComputeCore _computeCore)
        {
            computeCore = _computeCore;
        }

        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<JsonResult> Get()
        {
            //get list of all tribes
            string[] uniqueTribeNames = computeCore.tribal_dict
          .SelectMany(entry => entry.Value.Keys)
          .Distinct()
          .ToArray();

            return new JsonResult(new { tribes = uniqueTribeNames }, new JsonSerializerSettings { Formatting = Formatting.Indented });




        }
    }
}