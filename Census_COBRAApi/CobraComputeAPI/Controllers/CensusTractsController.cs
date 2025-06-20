using CobraCompute;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CobraComputeAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CensusTractsController : Controller
    {
        private readonly CobraComputeCore computeCore;
        public CensusTractsController(CobraComputeCore _computeCore)
        {
            computeCore = _computeCore;
        }
        [HttpGet]
        public IActionResult Get(string fileType)
        {
            if (string.IsNullOrWhiteSpace(fileType))
            {
                return BadRequest(new { error = "fileType parameter is required." });
            }

            string geoJsonData = fileType.ToLower() switch
            {
                "simple" => computeCore.simpleGeojson,
                "full" => computeCore.fullGeojson,
                _ => null
            };

            if (string.IsNullOrEmpty(geoJsonData))
            {
                return BadRequest(new { error = "GeoJSON data not available for the provided fileType. Use 'simple' or 'full'." });
            }

            // Return the raw JSON content.
            return Content(geoJsonData, "application/json");
        }

    }
}

//////////////////////////////////////////


