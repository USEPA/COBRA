﻿using CobraCompute;
using Export.XLS;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;

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

        [HttpGet("{token}/{which}")]
        public FileContentResult Get(Guid token, string which, [FromQuery] double discountrate = 3)
        {

            lock (computeCore)
            {
                computeCore.retrieve_userscenario(token);
                if (which == "results")
                {
                    List<Cobra_ResultDetail> results = computeCore.GetResults(discountrate);

                    ExcelDocument document = new ExcelDocument();
                    document.UserName = "COBRA WEB API";
                    document.CodePage = CultureInfo.CurrentCulture.TextInfo.ANSICodePage;

                    document[0, 0].Value = "ID";
                    document[0, 1].Value = "destindx";
                    document[0, 2].Value = "FIPS";
                    document[0, 3].Value = "STATE";
                    document[0, 4].Value = "COUNTY";
                    document[0, 5].Value = "BASE_FINAL_PM";
                    document[0, 6].Value = "CTRL_FINAL_PM";
                    document[0, 7].Value = "DELTA_FINAL_PM";
                    document[0, 8].Value = "Acute_Bronchitis";
                    document[0, 9].Value = "Acute_Myocardial_Infarction_Nonfatal__high_";
                    document[0, 10].Value = "Acute_Myocardial_Infarction_Nonfatal__low_";
                    document[0, 11].Value = "Asthma_Exacerbation_Cough";
                    document[0, 12].Value = "Asthma_Exacerbation_Shortness_of_Breath";
                    document[0, 13].Value = "Asthma_Exacerbation_Wheeze";
                    document[0, 14].Value = "Emergency_Room_Visits_Asthma";
                    document[0, 15].Value = "HA_All_Cardiovascular__less_Myocardial_Infarctions_";
                    document[0, 16].Value = "HA_All_Respiratory";
                    document[0, 17].Value = "HA_Asthma";
                    document[0, 18].Value = "HA_Chronic_Lung_Disease";
                    document[0, 19].Value = "Lower_Respiratory_Symptoms";
                    document[0, 20].Value = "Minor_Restricted_Activity_Days";
                    document[0, 21].Value = "Mortality_All_Cause__low_";
                    document[0, 22].Value = "Mortality_All_Cause__high_";
                    document[0, 23].Value = "Infant_Mortality";
                    document[0, 24].Value = "Upper_Respiratory_Symptoms";
                    document[0, 25].Value = "Work_Loss_Days";
                    document[0, 26].Value = "$_Acute_Bronchitis";
                    document[0, 27].Value = "$_Acute_Myocardial_Infarction_Nonfatal__high_";
                    document[0, 28].Value = "$_Acute_Myocardial_Infarction_Nonfatal__low_";
                    document[0, 29].Value = "$_Asthma_Exacerbation";
                    document[0, 30].Value = "$_Emergency_Room_Visits_Asthma";
                    document[0, 31].Value = "$_CVD_Hosp_Adm";
                    document[0, 32].Value = "$_Resp_Hosp_Adm";
                    document[0, 33].Value = "$_Lower_Respiratory_Symptoms";
                    document[0, 34].Value = "$_Minor_Restricted_Activity_Days";
                    document[0, 35].Value = "$_Mortality_All_Cause__low_";
                    document[0, 36].Value = "$_Mortality_All_Cause__high_";
                    document[0, 37].Value = "$_Infant_Mortality";
                    document[0, 38].Value = "$_Upper_Respiratory_Symptoms";
                    document[0, 39].Value = "$_Work_Loss_Days";


                    int rowcount = 1;
                    foreach (Cobra_ResultDetail result in results)
                    {
                        document[rowcount, 0].Value = result.ID;
                        document[rowcount, 1].Value = result.destindx;
                        document[rowcount, 2].Value = result.FIPS;
                        document[rowcount, 3].Value = result.STATE;
                        document[rowcount, 4].Value = result.COUNTY;
                        document[rowcount, 5].Value = result.BASE_FINAL_PM;
                        document[rowcount, 6].Value = result.CTRL_FINAL_PM;
                        document[rowcount, 7].Value = result.DELTA_FINAL_PM;
                        document[rowcount, 8].Value = result.Acute_Bronchitis;
                        document[rowcount, 9].Value = result.Acute_Myocardial_Infarction_Nonfatal__high_;
                        document[rowcount, 10].Value = result.Acute_Myocardial_Infarction_Nonfatal__low_;
                        document[rowcount, 11].Value = result.Asthma_Exacerbation_Cough;
                        document[rowcount, 12].Value = result.Asthma_Exacerbation_Shortness_of_Breath;
                        document[rowcount, 13].Value = result.Asthma_Exacerbation_Wheeze;
                        document[rowcount, 14].Value = result.Emergency_Room_Visits_Asthma;
                        document[rowcount, 15].Value = result.HA_All_Cardiovascular__less_Myocardial_Infarctions_;
                        document[rowcount, 16].Value = result.HA_All_Respiratory;
                        document[rowcount, 17].Value = result.HA_Asthma;
                        document[rowcount, 18].Value = result.HA_Chronic_Lung_Disease;
                        document[rowcount, 19].Value = result.Lower_Respiratory_Symptoms;
                        document[rowcount, 20].Value = result.Minor_Restricted_Activity_Days;
                        document[rowcount, 21].Value = result.Mortality_All_Cause__low_;
                        document[rowcount, 22].Value = result.Mortality_All_Cause__high_;
                        document[rowcount, 23].Value = result.Infant_Mortality;
                        document[rowcount, 24].Value = result.Upper_Respiratory_Symptoms;
                        document[rowcount, 25].Value = result.Work_Loss_Days;
                        document[rowcount, 26].Value = result.C__Acute_Bronchitis;
                        document[rowcount, 27].Value = result.C__Acute_Myocardial_Infarction_Nonfatal__high_;
                        document[rowcount, 28].Value = result.C__Acute_Myocardial_Infarction_Nonfatal__low_;
                        document[rowcount, 29].Value = result.C__Asthma_Exacerbation;
                        document[rowcount, 30].Value = result.C__Emergency_Room_Visits_Asthma;
                        document[rowcount, 31].Value = result.C__CVD_Hosp_Adm;
                        document[rowcount, 32].Value = result.C__Resp_Hosp_Adm;
                        document[rowcount, 33].Value = result.C__Lower_Respiratory_Symptoms;
                        document[rowcount, 34].Value = result.C__Minor_Restricted_Activity_Days;
                        document[rowcount, 35].Value = result.C__Mortality_All_Cause__low_;
                        document[rowcount, 36].Value = result.C__Mortality_All_Cause__high_;
                        document[rowcount, 37].Value = result.C__Infant_Mortality;
                        document[rowcount, 38].Value = result.C__Upper_Respiratory_Symptoms;
                        document[rowcount, 39].Value = result.C__Work_Loss_Days;
                        rowcount++;
                    }


                    MemoryStream stream = new MemoryStream();
                    document.Save(stream);
                    stream.Seek(0, SeekOrigin.Begin);
                    FileContentResult fcresult = new FileContentResult(stream.GetBuffer(), "application/vnd.ms-excel");
                    fcresult.FileDownloadName = "Detailed_COBRA_Report.xls";
                    return fcresult;
                }
                else
                {
                    DataTable result = null;
                    if (which == "base")
                    {
                        result = computeCore.SummarizeEmissionsForExport(computeCore.EmissionsInventory);
                    }

                    ExcelDocument document = new ExcelDocument();
                    document.UserName = "COBRA WEB API";
                    document.CodePage = CultureInfo.CurrentCulture.TextInfo.ANSICodePage;

                    // First headers.
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


                    MemoryStream stream = new MemoryStream();
                    document.Save(stream);
                    stream.Seek(0, SeekOrigin.Begin);
                    FileContentResult fcresult = new FileContentResult(stream.GetBuffer(), "application/vnd.ms-excel");
                    fcresult.FileDownloadName = "COBRA_Baseline_Report.xls";
                    return fcresult;
                }


            }
        }


    }
}
