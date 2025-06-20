using CobraCompute;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Threading.Tasks;

namespace CobraComputeAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmissionsBatchUpdateController : ControllerBase
    {
        private readonly CobraComputeCore computeCore;

        public EmissionsBatchUpdateController(CobraComputeCore _computeCore)
        {
            computeCore = _computeCore;
        }

        [HttpPost]
        public async Task Post([FromBody] EmissionsDataUpdateRequest[] requestparamsarray)
        {
            var computeInstance = computeCore.CreateInstance();
            await computeInstance.retrieve_userscenario(requestparamsarray[0].spec.token);
            foreach (var requestparams in requestparamsarray)
            {
                if (requestparams.spec.token == requestparamsarray[0].spec.token)
                {
                   computeInstance.UpdateEmissionsWithCriteria(requestparams);
                }
            }
            Debug.WriteLine("finished update emissions");
            await computeInstance.store_userscenario();
        }
    }
}