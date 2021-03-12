using CobraCompute;
using Microsoft.AspNetCore.Mvc;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using System;
using System.Globalization;
using System.IO;

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
        public FileContentResult Get(Guid token, string filter = "0", [FromQuery] double discountrate=3)
        {
            lock (computeCore)
            {
                computeCore.retrieve_userscenario(token);
                Cobra_Result result = new Cobra_Result();

                result.Impacts = computeCore.GetResults(discountrate);
                result.Summary = new Cobra_ResultSummary();
                Formatters.SummaryComposer(filter, result);

                HSSFWorkbook hssfwb;

                string[] paths = { @"data", "CobraTemplate.xls" };
                string templatePath = Path.Combine(paths);

                using (FileStream file = new FileStream(templatePath, FileMode.Open, FileAccess.Read))
                {
                    hssfwb = new HSSFWorkbook(file);
                }

                //now do the replacements
                ISheet sheet = hssfwb.GetSheetAt(hssfwb.ActiveSheetIndex);
                sheet.GetRow(3).GetCell(1).SetCellValue(result.Summary.TotalHealthBenefitsValue_low.ToString("C0", CultureInfo.CurrentCulture));
                sheet.GetRow(3).GetCell(3).SetCellValue(result.Summary.TotalHealthBenefitsValue_high.ToString("C0", CultureInfo.CurrentCulture));

                sheet.GetRow(8).GetCell(1).SetCellValue(result.Summary.Mortality_All_Cause__low_.ToString("F3", CultureInfo.CurrentCulture) + " / " + result.Summary.Mortality_All_Cause__high_.ToString("F3", CultureInfo.CurrentCulture));
                sheet.GetRow(8).GetCell(3).SetCellValue(result.Summary.C__Mortality_All_Cause__low_.ToString("C0", CultureInfo.CurrentCulture) + " / " + result.Summary.C__Mortality_All_Cause__high_.ToString("C0", CultureInfo.CurrentCulture));

                sheet.GetRow(9).GetCell(1).SetCellValue(result.Summary.Acute_Myocardial_Infarction_Nonfatal__low_.ToString("F3", CultureInfo.CurrentCulture) + " / " + result.Summary.Acute_Myocardial_Infarction_Nonfatal__high_.ToString("F3", CultureInfo.CurrentCulture));
                sheet.GetRow(9).GetCell(3).SetCellValue(result.Summary.C__Acute_Myocardial_Infarction_Nonfatal__low_.ToString("C0", CultureInfo.CurrentCulture) + " / " + result.Summary.C__Acute_Myocardial_Infarction_Nonfatal__high_.ToString("C0", CultureInfo.CurrentCulture));

                sheet.GetRow(10).GetCell(1).SetCellValue(result.Summary.Infant_Mortality.ToString("F3", CultureInfo.CurrentCulture));
                sheet.GetRow(10).GetCell(3).SetCellValue(result.Summary.C__Infant_Mortality.ToString("C0", CultureInfo.CurrentCulture));

                sheet.GetRow(11).GetCell(1).SetCellValue((result.Summary.HA_All_Respiratory + result.Summary.HA_Chronic_Lung_Disease).ToString("F3", CultureInfo.CurrentCulture));
                sheet.GetRow(11).GetCell(3).SetCellValue((result.Summary.C__CVD_Hosp_Adm).ToString("C0", CultureInfo.CurrentCulture));

                sheet.GetRow(12).GetCell(1).SetCellValue((result.Summary.HA_All_Cardiovascular__less_Myocardial_Infarctions_).ToString("F3", CultureInfo.CurrentCulture));
                sheet.GetRow(12).GetCell(3).SetCellValue((result.Summary.C__Resp_Hosp_Adm).ToString("C0", CultureInfo.CurrentCulture));

                sheet.GetRow(13).GetCell(1).SetCellValue((result.Summary.Acute_Bronchitis).ToString("F3", CultureInfo.CurrentCulture));
                sheet.GetRow(13).GetCell(3).SetCellValue((result.Summary.C__Acute_Bronchitis).ToString("C0", CultureInfo.CurrentCulture));

                sheet.GetRow(14).GetCell(1).SetCellValue((result.Summary.Upper_Respiratory_Symptoms).ToString("F3", CultureInfo.CurrentCulture));
                sheet.GetRow(14).GetCell(3).SetCellValue((result.Summary.C__Upper_Respiratory_Symptoms).ToString("C0", CultureInfo.CurrentCulture));

                sheet.GetRow(15).GetCell(1).SetCellValue((result.Summary.Lower_Respiratory_Symptoms).ToString("F3", CultureInfo.CurrentCulture));
                sheet.GetRow(15).GetCell(3).SetCellValue((result.Summary.C__Lower_Respiratory_Symptoms).ToString("C0", CultureInfo.CurrentCulture));

                sheet.GetRow(16).GetCell(1).SetCellValue((result.Summary.Emergency_Room_Visits_Asthma).ToString("F3", CultureInfo.CurrentCulture));
                sheet.GetRow(16).GetCell(3).SetCellValue((result.Summary.C__Emergency_Room_Visits_Asthma).ToString("C0", CultureInfo.CurrentCulture));

                sheet.GetRow(17).GetCell(1).SetCellValue((result.Summary.Asthma_Exacerbation_Cough + result.Summary.Asthma_Exacerbation_Shortness_of_Breath + result.Summary.Asthma_Exacerbation_Wheeze).ToString("F3", CultureInfo.CurrentCulture));
                sheet.GetRow(17).GetCell(3).SetCellValue((result.Summary.C__Asthma_Exacerbation).ToString("C0", CultureInfo.CurrentCulture));

                sheet.GetRow(18).GetCell(1).SetCellValue((result.Summary.Minor_Restricted_Activity_Days).ToString("F3", CultureInfo.CurrentCulture));
                sheet.GetRow(18).GetCell(3).SetCellValue((result.Summary.C__Minor_Restricted_Activity_Days).ToString("C0", CultureInfo.CurrentCulture));

                sheet.GetRow(19).GetCell(1).SetCellValue((result.Summary.Work_Loss_Days).ToString("F3", CultureInfo.CurrentCulture));
                sheet.GetRow(19).GetCell(3).SetCellValue((result.Summary.C__Work_Loss_Days).ToString("C0", CultureInfo.CurrentCulture));

                MemoryStream stream = new MemoryStream();
                hssfwb.Write(stream);
                stream.Seek(0, SeekOrigin.Begin);
                FileContentResult fcresult = new FileContentResult(stream.GetBuffer(), "application/vnd.ms-excel");
                string fipsindicator = "";
                if (filter!="0" && filter != "00")
                {
                    fipsindicator = "_FIPS" + filter;
                } else
                {
                    fipsindicator = "_Nation";
                }
                fcresult.FileDownloadName = "COBRA_Summary"+fipsindicator+".xls";
                return fcresult;

            }


        }


    }
}
