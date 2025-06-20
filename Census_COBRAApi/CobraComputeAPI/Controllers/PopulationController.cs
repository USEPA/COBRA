using CobraCompute;
using Microsoft.AspNetCore.Mvc;
using Minio.Exceptions;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CobraComputeAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PopulationController : ControllerBase
    {
        private readonly CobraComputeCore computeCore;

        public PopulationController(CobraComputeCore _computeCore)
        {
            computeCore = _computeCore;
        }

        [HttpGet]
        public async Task<JsonResult> Get()
        {
            // Build a lookup for census info (keyed by dest_tract)
            var censusLookup = computeCore.census_dict.Values
                                     .ToDictionary(c => c.dest_tract, c => c);

            // Load slider data on demand from S3
            var sliderData = await computeCore.LoadSliderDataAsync();

            List<Cobra_POP> populations = computeCore.Populations;

            // Combine population data with census and slider data
            var popDict = populations.ToDictionary(
                pop => pop.dest_tract,
                pop =>
                {
                    var destTract = pop.dest_tract;
                    var summedPop = SumAges(pop);

                    // Create the basic result dictionary for each dest_tract
                    var result = new Dictionary<string, object>
                    {
                        { "population", summedPop }
                    };

                    // Add census data if available
                    if (censusLookup.TryGetValue(destTract, out var censusInfo))
                    {
                        result.Add("FIPS", censusInfo.FIPS);
                        result.Add("CJEST", censusInfo.CJEST);
                        result.Add("IRA_fraction", censusInfo.IRA_fraction);
                    }

                    // Add slider data if available
                    if (sliderData.TryGetValue(destTract, out var slider))
                    {

                        result.Add("LOWINCOME", slider.P_LOWINC);
                        result.Add("LIFEEXPPCT", slider.P_LIFEXPCT);
                        result.Add("P_PM25", slider.P_PM25);
                        result.Add("P_OZONE", slider.P_OZONE);
                        result.Add("ENERGYBURDEN<6PCT", slider.ENERGYBURDEN_LESS_6PCT);
                        result.Add("ENERGYBURDEN>=6PCT", slider.ENERGYBURDEN_GRTR_EQL_6PCT);
                        result.Add("ENERGYBURDEN>=10PCT", slider.ENERGYBURDEN_GRTR_EQL_10PCT);


                    } else
                    {
                        {
                            result.Add("LOWINCOME",-1);
                            result.Add("LOWINCPCT", -1);
                            result.Add("LIFEEXPPCT", -1);
                            result.Add("P_PM25", -1);
                            result.Add("P_OZONE",-1);
                            result.Add("ENERGYBURDEN_<6PCT", -1);
                            result.Add("ENERGYBURDEN_>=6PCT", -1);
                            result.Add("ENERGYBURDEN_>=10PCT", -1);
                        }
                    }

                    return result;
                }
            );

            return new JsonResult(popDict, new JsonSerializerSettings() { Formatting = Formatting.Indented });
        }


        // sum all age properties of a Cobra_POP instance
        private double SumAges(Cobra_POP pop)
        {
            double sum = 0;
            for (int i = 0; i <= 99; i++)
            {
                sum += pop.popat(i);
            }
            return sum;
        }
    }

  


}