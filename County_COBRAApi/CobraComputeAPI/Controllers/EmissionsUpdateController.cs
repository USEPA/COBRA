using CobraCompute;
using Microsoft.AspNetCore.Mvc;


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
        public void Post([FromBody]  EmissionsDataUpdateRequest requestparams)
        {
            lock (computeCore)
            {
                computeCore.retrieve_userscenario(requestparams.spec.token);
                computeCore.UpdateEmissionsWithCriteria(requestparams);
                computeCore.store_userscenario();
            }
        }


    }
}
