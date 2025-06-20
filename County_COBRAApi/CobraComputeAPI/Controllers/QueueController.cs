using CobraCompute;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace CobraComputeAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class QueueController : ControllerBase
    {
        private readonly CobraComputeCore computeCore;

        public QueueController(CobraComputeCore _computeCore)
        {
            computeCore = _computeCore;
        }

        [HttpGet("{token}")]
        public JsonResult Get(Guid token)
        {
            lock (computeCore)
            {
                computeCore.retrieve_userscenario(token);
                return new JsonResult(computeCore.GetChangeQueueSubmission(), new JsonSerializerSettings() { Formatting = Formatting.Indented });
            }
        }


        [HttpPost]
        public StatusCodeResult Post([FromBody] queueSubmission submission)
        {
            // pls note: in order to keep object definitions low and avoid copying back and forth between objects we are
            // using queueSubmission which can potentially include an updatepacket
            // the code however will overwrite the updatepacket intentionally based on the actual provided inputs
            lock (computeCore)
            {
                computeCore.retrieve_userscenario(submission.token);
                //fix up updatepacket
                foreach (QueueElement item in submission.queueElements)
                {
                    //Get current baseline emissions;
                    EmissionsDataRetrievalRequest request = new EmissionsDataRetrievalRequest();
                    request.token = submission.token;
                    request.fipscodes = item.statetree_items_selected;
                    request.tiers = item.tiertree_items_selected[0];
                    EmissionsSums inventoryEmissions = computeCore.SummarizeBaseControlEmissionsWithCriteria(computeCore.buildStringCriteria(request));
                    //build update packet from result.baseline
                    UpdatePacket finalcontrolpacket = new UpdatePacket();
                    finalcontrolpacket.tierselection = new String[1];
                    finalcontrolpacket.tierselection[0] = request.tiers;
                    finalcontrolpacket.fipscodes = request.fipscodes;
                    float baseinventoryPM25 = 0;
                    float baseinventorySO2 = 0;
                    float baseinventoryNOx = 0;
                    float baseinventoryNH3 = 0;
                    float baseinventoryVOC = 0;
                    if (inventoryEmissions.baseline.Rows.Count > 0)
                    {
                        baseinventoryPM25 = float.Parse(inventoryEmissions.baseline.Rows[0]["PM25"].ToString());
                        baseinventorySO2 = float.Parse(inventoryEmissions.baseline.Rows[0]["SO2"].ToString());
                        baseinventoryNOx = float.Parse(inventoryEmissions.baseline.Rows[0]["NOx"].ToString());
                        baseinventoryNH3 = float.Parse(inventoryEmissions.baseline.Rows[0]["NH3"].ToString());
                        baseinventoryVOC = float.Parse(inventoryEmissions.baseline.Rows[0]["VOC"].ToString());
                    }
                    //pm
                    float value = 0;
                    try
                    {
                        value = float.Parse(item.cPM25);
                        if (item.PM25pt == "percent")
                        {
                            //compute absolute tons value from percentage if required
                            value = baseinventoryPM25 * value / 100;
                        };
                        if (item.PM25ri == "reduce") { value = value * -1; }
                    }
                    catch { value = 0; }
                    finally
                    {
                        finalcontrolpacket.PM25 = baseinventoryPM25 + value;
                    }

                    //SO2
                    value = 0;
                    try
                    {
                        value = float.Parse(item.cSO2);
                        if (item.PM25pt == "percent")
                        {
                            //compute absolute tons value from percentage if required
                            value = baseinventorySO2 * value / 100;
                        };
                        if (item.SO2ri == "reduce") { value = value * -1; }
                    }
                    catch { value = 0; }
                    finally
                    {
                        finalcontrolpacket.SO2 = baseinventorySO2 + value;
                    }

                    //NOx
                    value = 0;
                    try
                    {
                        value = float.Parse(item.cNOX);
                        if (item.NOXpt == "percent")
                        {
                            //compute absolute tons value from percentage if required
                            value = baseinventoryNOx * value / 100;
                        };
                        if (item.NOXri == "reduce") { value = value * -1; }
                    }
                    catch { value = 0; }
                    finally
                    {
                        finalcontrolpacket.NOx = baseinventoryNOx + value;
                    }

                    //NH3
                    value = 0;
                    try
                    {
                        value = float.Parse(item.cNH3);
                        if (item.NH3pt == "percent")
                        {
                            //compute absolute tons value from percentage if required
                            value = baseinventoryNH3 * value / 100;
                        };
                        if (item.NH3ri == "reduce") { value = value * -1; }
                    }
                    catch { value = 0; }
                    finally
                    {
                        finalcontrolpacket.NH3 = baseinventoryNH3 + value;
                    }

                    //VOC
                    value = 0;
                    try
                    {
                        value = float.Parse(item.cVOC);
                        if (item.VOCpt == "percent")
                        {
                            //compute absolute tons value from percentage if required
                            value = baseinventoryVOC * value / 100;
                        };
                        if (item.VOCri == "reduce") { value = value * -1; }
                    }
                    catch { value = 0; }
                    finally
                    {
                        finalcontrolpacket.VOC = baseinventoryVOC + value;
                    }
                    item.updatePacket = finalcontrolpacket;
                }
                computeCore.SetChangeQueueSubmission(submission);
                computeCore.store_userscenario();
                return Ok();
            }
        }



    }
}
