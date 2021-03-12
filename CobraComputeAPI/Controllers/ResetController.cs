using CobraCompute;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;

namespace CobraComputeAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ResetController : ControllerBase
    {
        private readonly CobraComputeCore computeCore;

        public ResetController(CobraComputeCore _computeCore)
        {
            computeCore = _computeCore;
        }

        [HttpPost]
        public void Post([FromBody] ResetRequest request)
        {
            lock (computeCore)
            {
                computeCore.reset_userscenario(request.token);
            }
        }


    }
}
