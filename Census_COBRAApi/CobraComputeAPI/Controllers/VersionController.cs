using CobraCompute;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CobraComputeAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VersionController : ControllerBase
    {
        private readonly CobraComputeCore computeCore;
        private static readonly SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);

        public VersionController(CobraComputeCore _computeCore)
        {
            computeCore = _computeCore;
        }

        [HttpGet]
        public async Task<string> Get()
        {
            await semaphoreSlim.WaitAsync();
            try
            {
                if (!computeCore.initilized)
                {
                    await computeCore.initialize();
                }
                return computeCore.version();
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }
    }
}