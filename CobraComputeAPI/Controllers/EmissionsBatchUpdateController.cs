using CobraCompute;
using Microsoft.AspNetCore.Mvc;


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
        public void Post([FromBody]  EmissionsDataUpdateRequest[] requestparamsarray)
        {
            lock (computeCore)
            {
                computeCore.retrieve_userscenario(requestparamsarray[0].spec.token);
                foreach (var requestparams in requestparamsarray)
                {
                    if (requestparams.spec.token == requestparamsarray[0].spec.token)
                    {
                        computeCore.UpdateEmissionsWithCriteria(requestparams);
                    }
                }
                computeCore.store_userscenario();
            }
        }


    }
}
