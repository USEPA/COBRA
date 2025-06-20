using ClosedXML.Excel;
using CobraCompute;
using Export.XLS;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using CsvHelper;
using DocumentFormat.OpenXml.Bibliography;

namespace CobraComputeAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ExcelExportController : ControllerBase
    {
        private readonly CobraComputeCore computeCore;

        public ExcelExportController(CobraComputeCore _computeCore)
        {
            computeCore = _computeCore;
        }

        private static string Escape(string field)
        {
            if (field.Contains('"') || field.Contains(',') || field.Contains('\n'))
            {
                // Double up any existing quotes, then wrap in quotes
                var escaped = field.Replace("\"", "\"\"");
                return $"\"{escaped}\"";
            }
            return field;
        }

        private Dictionary<string, string[]> propertyDict = new Dictionary<string, string[]>
        //dictionary of strings corresponding to the column name and then the corresponding variable names from detailed results that we need to access for that column
        { /* To add new health effects to excel or change order simply change this dictionary and then make sure that the cell ranges A1: CO1 for example, line up properly*/
            ["ID"] = new string[] { "ID" },
            ["destindx"] = new string[] { "destindx" },
            ["FIPS"] = new string[] { "FIPS" },
            ["State"] = new string[] { "STATE" },
            ["County"] = new string[] { "COUNTY" },
            ["Census Tract ID"] = new string[] { "tract_id" },
            ["Tribal Fraction"] = new string[] { "TRIBES" },
            ["Population"] = new string[] {"population"},
            //["CEJST"] = new string[] { "CJEST" },
            //["IRA Fraction"] = new string[] {"IRA_fraction"},
            ["Base PM 2.5"] = new string[] { "BASE_FINAL_PM" },
            ["Control PM 2.5"] = new string[] { "CTRL_FINAL_PM" },
            ["Delta PM 2.5"] = new string[] { "DELTA_FINAL_PM" },
            ["Base O3"] = new string[] { "BASE_FINAL_O3" },
            ["Control O3"] = new string[] { "CTRL_FINAL_O3" },
            ["Delta O3"] = new string[] { "DELTA_FINAL_O3" },


            ["$ Total Health Benefits(low estimate)"] = new string[] { "C__Total_Health_Benefits_Low_Value" },
            ["$ Total Health Benefits(high estimate)"] = new string[] { "C__Total_Health_Benefits_High_Value" },
            //total PM 
            ["$ PM Total Health Benefits (low estimate)"] = new string[] { "C__Total_PM_Low_Value" },
            ["$ PM Total Health Benefits (high estimate)"] = new string[] { "C__Total_PM_High_Value" },
            //total O3
            ["$ O3 Total Health Benefits"] = new string[] { "C__Total_O3_Value" },

            //total mortality low/high (PM + O3)
            ["Total Mortality(low estimate)"] = new string[] { "PM_Mortality_All_Cause__low_", "O3_Mortality_Longterm_exposure", "O3_Mortality_Shortterm_exposure", "PM_Infant_Mortality" },
            ["$ Total Mortality(low estimate)"] = new string[] { "C__PM_Mortality_All_Cause__low_", "C__O3_Mortality_Longterm_exposure", "C__O3_Mortality_Shortterm_exposure", "C__PM_Infant_Mortality" },
            ["Total Mortality(high estimate)"] = new string[] { "PM_Mortality_All_Cause__high_", "O3_Mortality_Longterm_exposure", "O3_Mortality_Shortterm_exposure", "PM_Infant_Mortality" },
            ["$ Total Mortality(high estimate)"] = new string[] { "C__PM_Mortality_All_Cause__high_", "C__O3_Mortality_Longterm_exposure", "C__O3_Mortality_Shortterm_exposure", "C__PM_Infant_Mortality" },

            //TOTAL PM MORTALITY + Breakdown
            ["PM Mortality, All Cause (low)"] = new string[] { "PM_Mortality_All_Cause__low_", "PM_Infant_Mortality" },
            ["$ PM Mortality, All Cause (low)"] = new string[] { "C__PM_Mortality_All_Cause__low_", "C__PM_Infant_Mortality" },
            ["PM Mortality (high)"] = new string[] { "PM_Mortality_All_Cause__high_", "PM_Infant_Mortality" },
            ["$ PM Mortality (high)"] = new string[] { "C__PM_Mortality_All_Cause__high_", "C__PM_Infant_Mortality" },

            //["PM Mortality, All Cause (low)"] = new string[] { "PM_Mortality_All_Cause__low_" },
            //["$ Total PM Mortality(low estimate)"] = new string[] { "C__PM_Mortality_All_Cause__low_" },
            //["Total PM Mortality(high estimate)"] = new string[] { "PM_Mortality_All_Cause__high_" },
            //["$ Total PM Mortality(high estimate)"] = new string[] { "C__PM_Mortality_All_Cause__high_" },
            ["PM Infant Mortality"] = new string[] { "PM_Infant_Mortality" },
            ["$ PM Infant Mortality"] = new string[] { "C__PM_Infant_Mortality" },

            //TOTAL O3 Mortality + Breakdown
            ["Total O3 Mortality"] = new string[] { "O3_Mortality_Longterm_exposure", "O3_Mortality_Shortterm_exposure" },
            ["$ Total O3 Mortality"] = new string[] { "C__O3_Mortality_Longterm_exposure", "C__O3_Mortality_Shortterm_exposure" },
            //breakdown
            ["O3 Mortality (Short-term exposure)"] = new string[] { "O3_Mortality_Shortterm_exposure" },
            ["$ O3 Mortality (Short-term exposure)"] = new string[] { "C__O3_Mortality_Shortterm_exposure" },
            ["O3 Mortality (Long-term exposure)"] = new string[] { "O3_Mortality_Longterm_exposure" },
            ["$ O3 Mortality (Long-term exposure)"] = new string[] { "C__O3_Mortality_Longterm_exposure" },

            //Total Asthma Symptoms + Breakdown
            ["Total Asthma Symptoms"] = new string[] { "PM_Asthma_Symptoms_Albuterol_use", "O3_Asthma_Symptoms_Chest_Tightness", "O3_Asthma_Symptoms_Cough", "O3_Asthma_Symptoms_Shortness_of_Breath", "O3_Asthma_Symptoms_Wheeze" },
            ["$ Total Asthma Symptoms"] = new string[] { "C__PM_Asthma_Symptoms_Albuterol_use", "C__O3_Asthma_Symptoms_Chest_Tightness", "C__O3_Asthma_Symptoms_Cough", "C__O3_Asthma_Symptoms_Shortness_of_Breath", "C__O3_Asthma_Symptoms_Wheeze" },
            //breakdown
            ["PM Asthma Symptoms, Albuterol Use"] = new string[] { "PM_Asthma_Symptoms_Albuterol_use" },
            ["$ PM Asthma Symptoms, Albuterol Use"] = new string[] { "C__PM_Asthma_Symptoms_Albuterol_use" },
            ["O3 Asthma Symptoms, Chest Tightness"] = new string[] { "O3_Asthma_Symptoms_Chest_Tightness" },
            ["$ O3 Asthma Symptoms, Chest Tightness"] = new string[] { "C__O3_Asthma_Symptoms_Chest_Tightness" },
            ["O3 Asthma Symptoms, Cough"] = new string[] { "O3_Asthma_Symptoms_Cough" },
            ["$ O3 Asthma Symptoms, Cough"] = new string[] { "C__O3_Asthma_Symptoms_Cough" },
            ["O3 Asthma Symptoms, Shortness of Breath"] = new string[] { "O3_Asthma_Symptoms_Shortness_of_Breath" },
            ["$ O3 Asthma Symptoms, Shortness of Breath"] = new string[] { "C__O3_Asthma_Symptoms_Shortness_of_Breath" },
            ["O3 Asthma Symptoms, Wheeze"] = new string[] { "O3_Asthma_Symptoms_Wheeze" },
            ["$ O3 Asthma Symptoms, Wheze"] = new string[] { "C__O3_Asthma_Symptoms_Wheeze" },

            //total incidence Asthma + PM/O3 breakdown
            ["Total Asthma Onset"] = new string[] { "O3_Incidence_Asthma", "PM_Incidence_Asthma" },
            ["$ Total Asthma Onset"] = new string[] { "C__O3_Incidence_Asthma", "C__PM_Incidence_Asthma" },
            ["PM Asthma Onset"] = new string[] { "PM_Incidence_Asthma" },
            ["$ PM Asthma Onset"] = new string[] { "C__PM_Incidence_Asthma" },
            ["O3 Asthma Onset"] = new string[] { "O3_Incidence_Asthma" },
            ["$ O3 Asthma Onset"] = new string[] { "C__O3_Incidence_Asthma" },

            //total incidence hayfever + PM/O3 breakdown
            ["Total Incidence, Hay Fever/Rhinitis"] = new string[] { "PM_Incidence_Hay_Fever_Rhinitis", "O3_Incidence_Hay_Fever_Rhinitis" },
            ["$ Total Incidence, Hay Fever/Rhinitis"] = new string[] { "C__PM_Incidence_Hay_Fever_Rhinitis", "C__O3_Incidence_Hay_Fever_Rhinitis" },
            ["PM Incidence, Hay Fever/Rhinitis"] = new string[] { "PM_Incidence_Hay_Fever_Rhinitis" },
            ["$ PM Incidence, Hay Fever/Rhinitis"] = new string[] { "C__PM_Incidence_Hay_Fever_Rhinitis" },
            ["O3 Incidence, Hay Fever/Rhinitis"] = new string[] { "O3_Incidence_Hay_Fever_Rhinitis" },
            ["$ O3 Incidence, Hay Fever/Rhinitis"] = new string[] { "C__O3_Incidence_Hay_Fever_Rhinitis" },

            //Total ER Visits, Respiratory + PM/O3 Breakdown
            ["Total ER Visits, Respiratory"] = new string[] { "PM_ER_visits_respiratory", "O3_ER_visits_respiratory" },
            ["$ Total ER Visits, Respiratory"] = new string[] { "C__PM_ER_visits_respiratory", "C__O3_ER_visits_respiratory" },
            ["PM ER Visits, Respiratory"] = new string[] { "PM_ER_visits_respiratory" },
            ["$ PM ER Visits, Respiratory"] = new string[] { "C__PM_ER_visits_respiratory" },
            ["O3 ER Visits, Respiratory"] = new string[] { "O3_ER_visits_respiratory" },
            ["$ O3 ER Visits, Respiratory"] = new string[] { "C__O3_ER_visits_respiratory" },

            //Total Hospital Admits All Respiratory
            ["Total Hospital Admits, All Respiratory"] = new string[] { "PM_HA_All_Respiratory", "O3_HA_All_Respiratory", "PM_HA_Respiratory2" },
            ["$ Total Hospital Admits All Respiratory"] = new string[] { "C__PM_Resp_Hosp_Adm", "C__O3_HA_All_Respiratory", "C__PM_HA_Respiratory2" },
            ["PM Hospital Admits, All Respiratory"] = new string[] { "PM_HA_All_Respiratory", "PM_HA_Respiratory2" },
            ["$ PM Hospital Admits All Respiratory"] = new string[] { "C__PM_Resp_Hosp_Adm", "C__PM_HA_Respiratory2" },
            ["O3 Hospital Admits, All Respiratory"] = new string[] { "O3_HA_All_Respiratory" },
            ["$ O3 Hospital Admits All Respiratory"] = new string[] { "C__O3_HA_All_Respiratory" },

            //rest of health effects are either just PM or O3
            ["PM Nonfatal Heart Attacks"] = new string[] { "PM_Acute_Myocardial_Infarction_Nonfatal" },
            ["$ PM Nonfatal Heart Attacks"] = new string[] { "C__PM_Acute_Myocardial_Infarction_Nonfatal" },
            ["PM Minor Restricted Activity Days"] = new string[] { "PM_Minor_Restricted_Activity_Days" },
            ["$ PM Minor Restricted Activity Days"] = new string[] { "C__PM_Minor_Restricted_Activity_Days" },
            ["PM Work Loss Days"] = new string[] { "PM_Work_Loss_Days" },
            ["$ PM Work Loss Days"] = new string[] { "C__PM_Work_Loss_Days" },
            ["PM Incidence Lung Cancer"] = new string[] { "PM_Incidence_Lung_Cancer" },
            ["$ PM Incidence Lung Cancer"] = new string[] { "C__PM_Incidence_Lung_Cancer" },
            ["PM HA Cardio Cerebro and Peripheral Vascular Disease"] = new string[] { "PM_HA_Cardio_Cerebro_and_Peripheral_Vascular_Disease" },
            ["$ PM HA Cardio Cerebro and Peripheral Vascular Disease"] = new string[] { "C__PM_HA_Cardio_Cerebro_and_Peripheral_Vascular_Disease" },
            ["PM HA Alzheimers Disease"] = new string[] { "PM_HA_Alzheimers_Disease" },
            ["$ PM HA Alzheimers Disease"] = new string[] { "C__PM_HA_Alzheimers_Disease" },
            ["PM HA Parkinsons Disease"] = new string[] { "PM_HA_Parkinsons_Disease" },
            ["$ PM HA Parkinsons Disease"] = new string[] { "C__PM_HA_Parkinsons_Disease" },
            ["PM Incidence Stroke"] = new string[] { "PM_Incidence_Stroke" },
            ["$ PM Incidence Stroke"] = new string[] { "C__PM_Incidence_Stroke" },
            ["PM Incidence Out of Hospital Cardiac Arrest"] = new string[] { "PM_Incidence_Out_of_Hospital_Cardiac_Arrest" },
            ["$ PM Incidence Out of Hospital Cardiac Arrest"] = new string[] { "C__PM_Incidence_Out_of_Hospital_Cardiac_Arrest" },
            ["PM ER Visits All Cardiac Outcomes"] = new string[] { "PM_ER_visits_All_Cardiac_Outcomes" },
            ["$ PM ER Visits All Cardiac Outcomes"] = new string[] { "C__PM_ER_visits_All_Cardiac_Outcomes" },
            ["O3 ER Visits, Asthma"] = new string[] { "O3_ER_Visits_Asthma" },
            ["$ O3 ER Visits, Asthma"] = new string[] { "C__O3_ER_Visits_Asthma" },
            ["O3 School Loss Days, All Cause"] = new string[] { "O3_School_Loss_Days" },
            ["$ O3 School Loss Days, All Cause"] = new string[] { "C__O3_School_Loss_Days" },
        };

        [HttpGet("/getCellValue")]
        public double getCellValue(string key, Cobra_ResultDetail result)
        {
            string[] varsToSum = propertyDict[key];
            double cellValue = 0;
            foreach (string resultKey in varsToSum)
            {
                var propertyInfo = result.GetType().GetProperty(resultKey);
                if (propertyInfo != null)
                {
                    var resultVal = (double)propertyInfo.GetValue(result, null);
                    cellValue += resultVal;
                }
            }
            return cellValue;
        }

        [HttpGet("{token}/{which}")]
        public async Task<FileContentResult> Get(Guid token, string which, [FromQuery] double discountrate = 3)
        {

            if (which == "results")
            {
                var computeInstance = computeCore.CreateInstance();
                await computeInstance.retrieve_userscenario(token);
                var results = computeInstance.GetResults(discountrate);
                var populations = computeCore.Populations;
                   var popDict = populations.ToDictionary(p => p.dest_tract, p => p.totalPop());

                using var stream = new MemoryStream();
                using (var writer = new StreamWriter(stream))
                using (var csv = new CsvHelper.CsvWriter(writer, CultureInfo.InvariantCulture))
                {
                    // Write headers to CSV
                    foreach (var header in propertyDict.Keys)
                    {
                        csv.WriteField(header);
                    }
                    csv.NextRecord();

                    // Populate data rows
                    for (int i = results.Count - 1; i >= 0; i--)
                    {
                        var result = results[i];
                        foreach (var columnName in propertyDict.Keys)
                        {
                            var cellValue = columnName switch
                            {
                                "State" => (object)result.STATE,
                                "County" => (object)result.COUNTY,
                                "destindx" => (object)result.destindx,
                                "ID" => (object)result.ID,
                                "FIPS" => (object)result.FIPS,
                                "Census Tract ID" => (object)result.tract_id,
                                "Population" => popDict.TryGetValue(result.tract_id, out var p) ? p : 0.0,
                                "Tribal Fraction" =>
                                    string.Join("; ",
                                        result.TRIBES.Select(kv => $"{kv.Key}: {kv.Value}")),
                                _ => (object)getCellValue(columnName, result)
                            };
                            csv.WriteField(cellValue);
                        }
                        csv.NextRecord();
                    }

                    // Clear large objects from memory
                    results = null;
                    computeInstance.clearUserScenario();
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();

                    // Flush writer and get CSV content as byte array
                    writer.Flush();
                }

                return File(stream.ToArray(), "text/csv", "Detailed_COBRA_Report.csv");
            }
            else
            {
                DataTable result = null;
                if (which == "base")
                {
                    result = computeCore.SummarizeEmissionsForExport(computeCore.EmissionsInventory);
                }

                ExcelDocument document = new ExcelDocument
                {
                    UserName = "COBRA WEB API",
                    CodePage = CultureInfo.CurrentCulture.TextInfo.ANSICodePage
                };

                int iColCount = result.Columns.Count;
                for (int i = 0; i < iColCount; i++)
                {
                    document[0, i].Value = result.Columns[i].ColumnName;
                }
                int rowcount = 1;
                foreach (DataRow dr in result.Rows)
                {
                    for (int i = 0; i < iColCount; i++)
                    {
                        document[rowcount, i].Value = dr[i].ToString();
                    }
                    rowcount++;
                }

                using var stream = new MemoryStream();
                document.Save(stream);
                stream.Seek(0, SeekOrigin.Begin);
                FileContentResult fcresult = new FileContentResult(stream.ToArray(), "application/vnd.ms-excel")
                {
                    FileDownloadName = "COBRA_Baseline_Report.xls"
                };
                return fcresult;
            }
        }
    }
}