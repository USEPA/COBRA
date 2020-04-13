using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CobraCompute;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;


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

        // GET: api/EmissionsUpdate
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET: api/EmissionsUpdate/5
        [HttpGet("{id}", Name = "Get")]
        public string Get(int id)
        {
            return "value";
        }

        // POST: api/EmissionsUpdate
        [HttpPost]
        public void Post([FromBody]  EmissionsDataUpdateRequest requestparams)
        {
            lock (computeCore)
            {
                computeCore.UpdateEmissionsWithCriteria(requestparams.spec.token, requestparams);// computeCore.buildStringCriteria(requestparams));
            }
        }

        // PUT: api/EmissionsUpdate/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE: api/ApiWithActions/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
