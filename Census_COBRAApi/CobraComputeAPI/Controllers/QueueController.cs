using CobraCompute;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Diagnostics;

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
        public async Task<JsonResult> Get(Guid token)
        {
            var computeInstance = computeCore.CreateInstance();
            await computeInstance.retrieve_userscenario(token);
            return new JsonResult(computeInstance.GetChangeQueueSubmission(), new JsonSerializerSettings() { Formatting = Formatting.Indented });
        }

        [HttpPost]
        public async Task<StatusCodeResult> Post([FromBody] queueSubmission submission)
        {

            var computeInstance = computeCore.CreateInstance();
            await computeInstance.retrieve_userscenario(submission.token);
            // Filter out queue elements where all pollutants are zero
            var filteredQueueElements = submission.queueElements.Where(item =>
                !(item.cPM25 == 0 && item.cSO2 == 0 && item.cNOX == 0 && item.cNH3 == 0 && item.cVOC == 0)).ToList();


            var tasks = filteredQueueElements.Select(async item =>
            {
                EmissionsDataRetrievalRequest request = new EmissionsDataRetrievalRequest
                {
                    token = submission.token,
                    fipscodes = item.statetree_items_selected,
                    tiers = item.tiertree_items_selected[0]
                };

                EmissionsSums inventoryEmissions;
                lock (computeInstance)
                {
                    inventoryEmissions = computeInstance.SummarizeBaseControlEmissionsWithCriteria(computeCore.buildStringCriteria(request));
                }

                UpdatePacket finalcontrolpacket = new UpdatePacket
                {
                    tierselection = new string[1] { request.tiers },
                    fipscodes = request.fipscodes
                };

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

                float value = 0;

                // Helper function to calculate emissions
                void ProcessEmission(ref float baseValue, float controlValue, string percentType, string reduceType, string itemType)
                {
                    value = 0;
                    try
                    {
                        value = controlValue;
                        if (percentType == "percent")
                        {
                            value = baseValue * value / 100;
                        }
                        if (reduceType == "reduce") { value = value * -1; }
                    }
                    catch { value = 0; }
                    finally
                    {
                        switch (itemType)
                        {
                            case "PM25": finalcontrolpacket.PM25 = baseValue + value; break;
                            case "SO2": finalcontrolpacket.SO2 = baseValue + value; break;
                            case "NOX": finalcontrolpacket.NOx = baseValue + value; break;
                            case "NH3": finalcontrolpacket.NH3 = baseValue + value; break;
                            case "VOC": finalcontrolpacket.VOC = baseValue + value; break;
                        }
                    }
                }

                ProcessEmission(ref baseinventoryPM25, item.cPM25, item.PM25pt, item.PM25ri, "PM25");
                ProcessEmission(ref baseinventorySO2, item.cSO2, item.SO2pt, item.SO2ri, "SO2");
                ProcessEmission(ref baseinventoryNOx, item.cNOX, item.NOXpt, item.NOXri, "NOX");
                ProcessEmission(ref baseinventoryNH3, item.cNH3, item.NH3pt, item.NH3ri, "NH3");
                ProcessEmission(ref baseinventoryVOC, item.cVOC, item.VOCpt, item.VOCri, "VOC");

                item.updatePacket = finalcontrolpacket;
                return item;
            }).ToList();

            QueueElement[] processedElements = await Task.WhenAll(tasks);
            submission.queueElements = processedElements;

            
            // Check and add avertRegions if null or missing
            if (submission.avertRegions == null || !submission.avertRegions.Any())
            {
                submission.avertRegions = null;
            }

            // Check and add avertInputs if null or missing
            if (submission.avertInputs == null)
            {
                submission.avertInputs = null;
            }



            computeInstance.SetChangeQueueSubmission(submission);
            await computeInstance.store_userscenario();

            return Ok();
        }
    }
}