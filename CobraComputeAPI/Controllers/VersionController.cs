using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CobraCompute;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace CobraComputeAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VersionController : ControllerBase
    {
        private readonly CobraComputeCore computeCore;

        public VersionController(CobraComputeCore _computeCore)
        {
            computeCore = _computeCore;
        }

        // GET api/values
        [HttpGet]
        public String Get()
        {
            computeCore.FlushUserScenarios();
            if (!computeCore.initilized)
            {
                computeCore.initialize();
            }
            return "V0.3" + Environment.NewLine + computeCore.statuslog.ToString();
        }

    }
}
