using CobraCompute;
using Microsoft.AspNetCore.Mvc;
using System;

namespace CobraComputeAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LogController : ControllerBase
    {
        private readonly CobraComputeCore computeCore;

        public LogController(CobraComputeCore _computeCore)
        {
            computeCore = _computeCore;
        }

        [HttpGet]
        public String Get()
        {
            return computeCore.statuslog.ToString();
        }

    }
}
