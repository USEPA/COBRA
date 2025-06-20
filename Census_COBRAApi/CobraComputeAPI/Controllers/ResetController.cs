using CobraCompute;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

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
        public async Task<IActionResult> Post([FromBody] ResetRequest request)
        {
            var computeInstance = computeCore.CreateInstance();
            Debug.WriteLine("CALLING RESET FROM CONTROLLER");
            await computeInstance.reset_userscenario(request.token);
            Debug.WriteLine("---------------------DONE AWAITING RESET");
            return Ok();
        }
    }
}