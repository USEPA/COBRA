using CobraCompute;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace CobraComputeAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TokenController : ControllerBase
    {
        private readonly CobraComputeCore computeCore;
        private static readonly SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);

        public TokenController(CobraComputeCore _computeCore)
        {
            computeCore = _computeCore;
        }

        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<JsonResult> Get()
        {
            Console.WriteLine("IN TOKEN CONTROLLER");
            await semaphoreSlim.WaitAsync();
            try
            {
                if (computeCore.initilized)
                {
                    Guid _token = await computeCore.Scenarios.createUserScenario();
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
                    return new JsonResult(new { value = _token }, new JsonSerializerSettings() { Formatting = Formatting.Indented });
                }
                else
                {
                    await computeCore.initialize();
                    Guid _token = await computeCore.Scenarios.createUserScenario();
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
                    return new JsonResult(new { value = _token }, new JsonSerializerSettings() { Formatting = Formatting.Indented });
                }
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }

        [HttpDelete("{token}")]
        public async Task<StatusCodeResult> Delete(Guid token)
        {
            await semaphoreSlim.WaitAsync();
            try
            {
                await computeCore.Scenarios.deleteUserScenario(token);
                return Ok();
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }
    }
}