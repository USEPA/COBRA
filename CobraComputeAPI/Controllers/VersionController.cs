using CobraCompute;
using Microsoft.AspNetCore.Mvc;
using System;

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

        [HttpGet]
        public String Get()
        {
            lock (computeCore)
            {
                if (!computeCore.initilized)
                {
                    computeCore.initialize();
                }
            }
            return computeCore.version();
        }

    }
}
