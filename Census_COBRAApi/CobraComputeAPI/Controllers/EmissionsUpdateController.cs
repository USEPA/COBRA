using CobraCompute;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Threading.Tasks;

namespace CobraComputeAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmissionsUpdateController : ControllerBase
    {
        private readonly CobraComputeCore computeCore;

        public EmissionsUpdateController(CobraComputeCore _computeCore)
        {
            computeCore = _computeCore;
        }

        [HttpPost]
        public async Task Post([FromBody] EmissionsDataUpdateRequest requestparams)
        {
            var computeInstance = computeCore.CreateInstance();
            await computeInstance.retrieve_userscenario(requestparams.spec.token);
            Debug.WriteLine($"retrieved scenario now updating emissions....");
            computeInstance.UpdateEmissionsWithCriteria(requestparams);
            await computeInstance.store_userscenario();
        }
    }
}