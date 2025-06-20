using CobraCompute;
using Microsoft.AspNetCore.Mvc;
using NPOI.XSSF.UserModel;
using NPOI.SS.UserModel;
using System;
using System.Globalization;
using System.IO;
using DocumentFormat.OpenXml.Spreadsheet;

namespace CobraComputeAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SummaryExcelController : ControllerBase
    {
        private readonly CobraComputeCore computeCore;

        public SummaryExcelController(CobraComputeCore _computeCore)
        {
            computeCore = _computeCore;
        }

        [HttpGet("{token}/{filter}")]
        public FileContentResult Get(Guid token, string filter = "0", [FromQuery] double discountrate = 3)
        {
            lock (computeCore)
            {
                computeCore.retrieve_userscenario(token);
                Cobra_Result result = new Cobra_Result();

                result.Impacts = computeCore.GetResults(discountrate);
                result.Summary = new Cobra_ResultSummary();
                Formatters.SummaryComposer(filter, result);

                XSSFWorkbook workbook;

                string[] paths = { @"data", "CobraTemplate.xlsx" };
                string templatePath = Path.Combine(paths);

                using (FileStream file = new FileStream(templatePath, FileMode.Open, FileAccess.Read))
                {
                    workbook = new XSSFWorkbook(file);
                }

                //now do the replacements
                ISheet sheet = workbook.GetSheetAt(workbook.ActiveSheetIndex);
                sheet.GetRow(3).GetCell(1).SetCellValue(result.Summary.TotalHealthBenefitsValue_low.ToString("C0", CultureInfo.CurrentCulture));
                sheet.GetRow(3).GetCell(3).SetCellValue(result.Summary.TotalHealthBenefitsValue_high.ToString("C0", CultureInfo.CurrentCulture));

                sheet.GetRow(8).GetCell(1).SetCellValue(result.Summary.Mortality_All_Cause__low_.ToString("F3", CultureInfo.CurrentCulture) + " / " + result.Summary.Mortality_All_Cause__high_.ToString("F3", CultureInfo.CurrentCulture));
                sheet.GetRow(8).GetCell(3).SetCellValue(result.Summary.C__Mortality_All_Cause__low_.ToString("C0", CultureInfo.CurrentCulture) + " / " + result.Summary.C__Mortality_All_Cause__high_.ToString("C0", CultureInfo.CurrentCulture));

                sheet.GetRow(9).GetCell(1).SetCellValue(result.Summary.PM_Mortality_All_Cause__low_.ToString("F3", CultureInfo.CurrentCulture) + " / " + result.Summary.PM_Mortality_All_Cause__high_.ToString("F3", CultureInfo.CurrentCulture));
                sheet.GetRow(9).GetCell(3).SetCellValue(result.Summary.C__PM_Mortality_All_Cause__low_.ToString("C0", CultureInfo.CurrentCulture) + " / " + result.Summary.C__PM_Mortality_All_Cause__high_.ToString("C0", CultureInfo.CurrentCulture));

                sheet.GetRow(10).GetCell(1).SetCellValue(result.Summary.O3_Mortality_Shortterm_exposure.ToString("F3", CultureInfo.CurrentCulture));
                sheet.GetRow(10).GetCell(3).SetCellValue(result.Summary.C__O3_Mortality_Shortterm_exposure.ToString("C0", CultureInfo.CurrentCulture));

                sheet.GetRow(11).GetCell(1).SetCellValue(result.Summary.O3_Mortality_Longterm_exposure.ToString("F3", CultureInfo.CurrentCulture));
                sheet.GetRow(11).GetCell(3).SetCellValue(result.Summary.C__O3_Mortality_Longterm_exposure.ToString("C0", CultureInfo.CurrentCulture));

                sheet.GetRow(12).GetCell(1).SetCellValue(result.Summary.Acute_Myocardial_Infarction_Nonfatal.ToString("F3", CultureInfo.CurrentCulture));
                sheet.GetRow(12).GetCell(3).SetCellValue(result.Summary.C__Acute_Myocardial_Infarction_Nonfatal.ToString("C0", CultureInfo.CurrentCulture));

                sheet.GetRow(13).GetCell(1).SetCellValue(result.Summary.Infant_Mortality.ToString("F3", CultureInfo.CurrentCulture));
                sheet.GetRow(13).GetCell(3).SetCellValue(result.Summary.C__Infant_Mortality.ToString("C0", CultureInfo.CurrentCulture));

                sheet.GetRow(14).GetCell(1).SetCellValue((result.Summary.HA_All_Respiratory).ToString("F3", CultureInfo.CurrentCulture));
                sheet.GetRow(14).GetCell(3).SetCellValue((result.Summary.C__HA_All_Respiratory).ToString("C0", CultureInfo.CurrentCulture));
                sheet.GetRow(15).GetCell(1).SetCellValue((result.Summary.PM_HA_All_Respiratory).ToString("F3", CultureInfo.CurrentCulture));
                sheet.GetRow(15).GetCell(3).SetCellValue((result.Summary.C__PM_HA_All_Respiratory).ToString("C0", CultureInfo.CurrentCulture));
                sheet.GetRow(16).GetCell(1).SetCellValue((result.Summary.O3_HA_All_Respiratory).ToString("F3", CultureInfo.CurrentCulture));
                sheet.GetRow(16).GetCell(3).SetCellValue((result.Summary.C__O3_HA_All_Respiratory).ToString("C0", CultureInfo.CurrentCulture));

                sheet.GetRow(17).GetCell(1).SetCellValue((result.Summary.ER_visits_respiratory).ToString("F3", CultureInfo.CurrentCulture));
                sheet.GetRow(17).GetCell(3).SetCellValue((result.Summary.C__ER_visits_respiratory).ToString("C0", CultureInfo.CurrentCulture));
                sheet.GetRow(18).GetCell(1).SetCellValue((result.Summary.PM_ER_visits_respiratory).ToString("F3", CultureInfo.CurrentCulture));
                sheet.GetRow(18).GetCell(3).SetCellValue((result.Summary.C__PM_ER_visits_respiratory).ToString("C0", CultureInfo.CurrentCulture));
                sheet.GetRow(19).GetCell(1).SetCellValue((result.Summary.O3_ER_visits_respiratory).ToString("F3", CultureInfo.CurrentCulture));
                sheet.GetRow(19).GetCell(3).SetCellValue((result.Summary.C__O3_ER_visits_respiratory).ToString("C0", CultureInfo.CurrentCulture));

                sheet.GetRow(20).GetCell(1).SetCellValue((result.Summary.Incidence_Asthma).ToString("F3", CultureInfo.CurrentCulture));
                sheet.GetRow(20).GetCell(3).SetCellValue((result.Summary.C__Incidence_Asthma).ToString("C0", CultureInfo.CurrentCulture));
                sheet.GetRow(21).GetCell(1).SetCellValue((result.Summary.PM_Incidence_Asthma).ToString("F3", CultureInfo.CurrentCulture));
                sheet.GetRow(21).GetCell(3).SetCellValue((result.Summary.C__PM_Incidence_Asthma).ToString("C0", CultureInfo.CurrentCulture));
                sheet.GetRow(22).GetCell(1).SetCellValue((result.Summary.O3_Incidence_Asthma).ToString("F3", CultureInfo.CurrentCulture));
                sheet.GetRow(22).GetCell(3).SetCellValue((result.Summary.C__O3_Incidence_Asthma).ToString("C0", CultureInfo.CurrentCulture));

                sheet.GetRow(23).GetCell(1).SetCellValue((result.Summary.Asthma_Symptoms).ToString("F3", CultureInfo.CurrentCulture));
                sheet.GetRow(23).GetCell(3).SetCellValue((result.Summary.C__Asthma_Symptoms).ToString("C0", CultureInfo.CurrentCulture));
                sheet.GetRow(24).GetCell(1).SetCellValue((result.Summary.PM_Asthma_Symptoms_Albuterol_use).ToString("F3", CultureInfo.CurrentCulture));
                sheet.GetRow(24).GetCell(3).SetCellValue((result.Summary.C__PM_Asthma_Symptoms_Albuterol_use).ToString("C0", CultureInfo.CurrentCulture));
                sheet.GetRow(25).GetCell(1).SetCellValue((result.Summary.O3_Asthma_Symptoms_Chest_Tightness).ToString("F3", CultureInfo.CurrentCulture));
                sheet.GetRow(25).GetCell(3).SetCellValue((result.Summary.C__O3_Asthma_Symptoms_Chest_Tightness).ToString("C0", CultureInfo.CurrentCulture));
                sheet.GetRow(26).GetCell(1).SetCellValue((result.Summary.O3_Asthma_Symptoms_Cough).ToString("F3", CultureInfo.CurrentCulture));
                sheet.GetRow(26).GetCell(3).SetCellValue((result.Summary.C__O3_Asthma_Symptoms_Cough).ToString("C0", CultureInfo.CurrentCulture));
                sheet.GetRow(27).GetCell(1).SetCellValue((result.Summary.O3_Asthma_Symptoms_Shortness_of_Breath).ToString("F3", CultureInfo.CurrentCulture));
                sheet.GetRow(27).GetCell(3).SetCellValue((result.Summary.C__O3_Asthma_Symptoms_Shortness_of_Breath).ToString("C0", CultureInfo.CurrentCulture));
                sheet.GetRow(28).GetCell(1).SetCellValue((result.Summary.O3_Asthma_Symptoms_Wheeze).ToString("F3", CultureInfo.CurrentCulture));
                sheet.GetRow(28).GetCell(3).SetCellValue((result.Summary.C__O3_Asthma_Symptoms_Wheeze).ToString("C0", CultureInfo.CurrentCulture));

                sheet.GetRow(29).GetCell(1).SetCellValue((result.Summary.ER_Visits_Asthma).ToString("F3", CultureInfo.CurrentCulture));
                sheet.GetRow(29).GetCell(3).SetCellValue((result.Summary.C__ER_Visits_Asthma).ToString("C0", CultureInfo.CurrentCulture));

                sheet.GetRow(30).GetCell(1).SetCellValue((result.Summary.Incidence_Lung_Cancer).ToString("F3", CultureInfo.CurrentCulture));
                sheet.GetRow(30).GetCell(3).SetCellValue((result.Summary.C__Incidence_Lung_Cancer).ToString("C0", CultureInfo.CurrentCulture));

                sheet.GetRow(31).GetCell(1).SetCellValue((result.Summary.HA_Cardio_Cerebro_and_Peripheral_Vascular_Disease).ToString("F3", CultureInfo.CurrentCulture));
                sheet.GetRow(31).GetCell(3).SetCellValue((result.Summary.C__HA_Cardio_Cerebro_and_Peripheral_Vascular_Disease).ToString("C0", CultureInfo.CurrentCulture));

                sheet.GetRow(32).GetCell(1).SetCellValue((result.Summary.HA_Alzheimers_Disease).ToString("F3", CultureInfo.CurrentCulture));
                sheet.GetRow(32).GetCell(3).SetCellValue((result.Summary.C__HA_Alzheimers_Disease).ToString("C0", CultureInfo.CurrentCulture));

                sheet.GetRow(33).GetCell(1).SetCellValue((result.Summary.HA_Parkinsons_Disease).ToString("F3", CultureInfo.CurrentCulture));
                sheet.GetRow(33).GetCell(3).SetCellValue((result.Summary.C__HA_Parkinsons_Disease).ToString("C0", CultureInfo.CurrentCulture));

                sheet.GetRow(34).GetCell(1).SetCellValue((result.Summary.Incidence_Stroke).ToString("F3", CultureInfo.CurrentCulture));
                sheet.GetRow(34).GetCell(3).SetCellValue((result.Summary.C__Incidence_Stroke).ToString("C0", CultureInfo.CurrentCulture));

                sheet.GetRow(35).GetCell(1).SetCellValue((result.Summary.Incidence_Hay_Fever_Rhinitis).ToString("F3", CultureInfo.CurrentCulture));
                sheet.GetRow(35).GetCell(3).SetCellValue((result.Summary.C__Incidence_Hay_Fever_Rhinitis).ToString("C0", CultureInfo.CurrentCulture));
                sheet.GetRow(36).GetCell(1).SetCellValue((result.Summary.PM_Incidence_Hay_Fever_Rhinitis).ToString("F3", CultureInfo.CurrentCulture));
                sheet.GetRow(36).GetCell(3).SetCellValue((result.Summary.C__PM_Incidence_Hay_Fever_Rhinitis).ToString("C0", CultureInfo.CurrentCulture));
                sheet.GetRow(37).GetCell(1).SetCellValue((result.Summary.O3_Incidence_Hay_Fever_Rhinitis).ToString("F3", CultureInfo.CurrentCulture));
                sheet.GetRow(37).GetCell(3).SetCellValue((result.Summary.C__O3_Incidence_Hay_Fever_Rhinitis).ToString("C0", CultureInfo.CurrentCulture));

                sheet.GetRow(38).GetCell(1).SetCellValue((result.Summary.Incidence_Out_of_Hospital_Cardiac_Arrest).ToString("F3", CultureInfo.CurrentCulture));
                sheet.GetRow(38).GetCell(3).SetCellValue((result.Summary.C__Incidence_Out_of_Hospital_Cardiac_Arrest).ToString("C0", CultureInfo.CurrentCulture));

                sheet.GetRow(39).GetCell(1).SetCellValue((result.Summary.ER_visits_All_Cardiac_Outcomes).ToString("F3", CultureInfo.CurrentCulture));
                sheet.GetRow(39).GetCell(3).SetCellValue((result.Summary.C__ER_visits_All_Cardiac_Outcomes).ToString("C0", CultureInfo.CurrentCulture));

                sheet.GetRow(40).GetCell(1).SetCellValue((result.Summary.Minor_Restricted_Activity_Days).ToString("F3", CultureInfo.CurrentCulture));
                sheet.GetRow(40).GetCell(3).SetCellValue((result.Summary.C__Minor_Restricted_Activity_Days).ToString("C0", CultureInfo.CurrentCulture));

                sheet.GetRow(41).GetCell(1).SetCellValue((result.Summary.School_Loss_Days).ToString("F3", CultureInfo.CurrentCulture));
                sheet.GetRow(41).GetCell(3).SetCellValue((result.Summary.C__School_Loss_Days).ToString("C0", CultureInfo.CurrentCulture));

                sheet.GetRow(42).GetCell(1).SetCellValue((result.Summary.Work_Loss_Days).ToString("F3", CultureInfo.CurrentCulture));
                sheet.GetRow(42).GetCell(3).SetCellValue((result.Summary.C__Work_Loss_Days).ToString("C0", CultureInfo.CurrentCulture));

                sheet.GetRow(43).GetCell(1).SetCellValue(result.Summary.TotalPM_low.ToString("F3", CultureInfo.CurrentCulture) + " / " + result.Summary.TotalPM_high.ToString("F3", CultureInfo.CurrentCulture));
                sheet.GetRow(43).GetCell(3).SetCellValue(result.Summary.TotalPMValue_low.ToString("C0", CultureInfo.CurrentCulture) + " / " + result.Summary.TotalPMValue_high.ToString("C0", CultureInfo.CurrentCulture));

                sheet.GetRow(44).GetCell(1).SetCellValue(result.Summary.TotalO3.ToString("F3", CultureInfo.CurrentCulture));
                sheet.GetRow(44).GetCell(3).SetCellValue(result.Summary.TotalO3Value.ToString("C0", CultureInfo.CurrentCulture));

                MemoryStream stream = new MemoryStream();
                workbook.Write(stream);
                stream.Seek(0, SeekOrigin.Begin);
                FileContentResult fcresult = new FileContentResult(stream.ToArray(), "application/vnd.ms-excel");
                string fipsindicator = "";
                if (filter != "0" && filter != "00")
                {
                    fipsindicator = "_FIPS" + filter;
                }
                else
                {
                    fipsindicator = "_Nation";
                }
                fcresult.FileDownloadName = "COBRA_Summary" + fipsindicator + ".xlsx";
                return fcresult;

            }


        }


    }
}
