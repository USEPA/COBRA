using CobraCompute;

namespace CobraComputeAPI
{
    public static class Formatters
    {
        public static void SummaryComposer(string filter, Cobra_Result result)
        {
            foreach (Cobra_ResultDetail detail in result.Impacts)
            {
                if ((filter == "0") || (filter == detail.FIPS.Substring(0, filter.Length )) )
                { //check if FIPS state part matches in case of filter
                    result.Summary.Acute_Bronchitis += detail.Acute_Bronchitis.GetValueOrDefault(0);
                    result.Summary.Asthma_Exacerbation_Cough += detail.Asthma_Exacerbation_Cough.GetValueOrDefault(0);
                    result.Summary.Asthma_Exacerbation_Shortness_of_Breath += detail.Asthma_Exacerbation_Shortness_of_Breath.GetValueOrDefault(0);
                    result.Summary.Asthma_Exacerbation_Wheeze += detail.Asthma_Exacerbation_Wheeze.GetValueOrDefault(0);
                    result.Summary.Emergency_Room_Visits_Asthma += detail.Emergency_Room_Visits_Asthma.GetValueOrDefault(0);
                    result.Summary.HA_All_Cardiovascular__less_Myocardial_Infarctions_ += detail.HA_All_Cardiovascular__less_Myocardial_Infarctions_.GetValueOrDefault(0);
                    result.Summary.HA_All_Respiratory += detail.HA_All_Respiratory.GetValueOrDefault(0);
                    result.Summary.HA_Asthma += detail.HA_Asthma.GetValueOrDefault(0);
                    result.Summary.HA_Chronic_Lung_Disease += detail.HA_Chronic_Lung_Disease.GetValueOrDefault(0);
                    result.Summary.Lower_Respiratory_Symptoms += detail.Lower_Respiratory_Symptoms.GetValueOrDefault(0);
                    result.Summary.Minor_Restricted_Activity_Days += detail.Minor_Restricted_Activity_Days.GetValueOrDefault(0);
                    result.Summary.Infant_Mortality += detail.Infant_Mortality.GetValueOrDefault(0);
                    result.Summary.Upper_Respiratory_Symptoms += detail.Upper_Respiratory_Symptoms.GetValueOrDefault(0);
                    result.Summary.Work_Loss_Days += detail.Work_Loss_Days.GetValueOrDefault(0);
                    result.Summary.C__Acute_Bronchitis += detail.C__Acute_Bronchitis.GetValueOrDefault(0);
                    result.Summary.C__Acute_Myocardial_Infarction_Nonfatal__high_ += detail.C__Acute_Myocardial_Infarction_Nonfatal__high_.GetValueOrDefault(0);
                    result.Summary.C__Acute_Myocardial_Infarction_Nonfatal__low_ += detail.C__Acute_Myocardial_Infarction_Nonfatal__low_.GetValueOrDefault(0);
                    result.Summary.C__Asthma_Exacerbation += detail.C__Asthma_Exacerbation.GetValueOrDefault(0);
                    result.Summary.C__Emergency_Room_Visits_Asthma += detail.C__Emergency_Room_Visits_Asthma.GetValueOrDefault(0);
                    result.Summary.C__CVD_Hosp_Adm += detail.C__CVD_Hosp_Adm.GetValueOrDefault(0);
                    result.Summary.C__Resp_Hosp_Adm += detail.C__Resp_Hosp_Adm.GetValueOrDefault(0);
                    result.Summary.C__Lower_Respiratory_Symptoms += detail.C__Lower_Respiratory_Symptoms.GetValueOrDefault(0);
                    result.Summary.C__Minor_Restricted_Activity_Days += detail.C__Minor_Restricted_Activity_Days.GetValueOrDefault(0);
                    result.Summary.C__Mortality_All_Cause__low_ += detail.C__Mortality_All_Cause__low_.GetValueOrDefault(0);
                    result.Summary.C__Mortality_All_Cause__high_ += detail.C__Mortality_All_Cause__high_.GetValueOrDefault(0);
                    result.Summary.C__Infant_Mortality += detail.C__Infant_Mortality.GetValueOrDefault(0);
                    result.Summary.C__Upper_Respiratory_Symptoms += detail.C__Upper_Respiratory_Symptoms.GetValueOrDefault(0);
                    result.Summary.C__Work_Loss_Days += detail.C__Work_Loss_Days.GetValueOrDefault(0);

                    result.Summary.Acute_Myocardial_Infarction_Nonfatal__high_ += detail.Acute_Myocardial_Infarction_Nonfatal__high_.GetValueOrDefault(0);
                    result.Summary.Acute_Myocardial_Infarction_Nonfatal__low_ += detail.Acute_Myocardial_Infarction_Nonfatal__low_.GetValueOrDefault(0);

                    result.Summary.Mortality_All_Cause__low_ += detail.Mortality_All_Cause__low_.GetValueOrDefault(0);
                    result.Summary.Mortality_All_Cause__high_ += detail.Mortality_All_Cause__high_.GetValueOrDefault(0);

                    //now sort low high stuff, start with low bit that is equal to high bit
                    double lowvals = 0;

                    lowvals += detail.C__Acute_Bronchitis.GetValueOrDefault(0);

                    lowvals += detail.C__Asthma_Exacerbation.GetValueOrDefault(0);
                    lowvals += detail.C__Emergency_Room_Visits_Asthma.GetValueOrDefault(0);
                    lowvals += detail.C__CVD_Hosp_Adm.GetValueOrDefault(0);
                    lowvals += detail.C__Resp_Hosp_Adm.GetValueOrDefault(0);
                    lowvals += detail.C__Lower_Respiratory_Symptoms.GetValueOrDefault(0);
                    lowvals += detail.C__Minor_Restricted_Activity_Days.GetValueOrDefault(0);

                    lowvals += detail.C__Infant_Mortality.GetValueOrDefault(0);
                    lowvals += detail.C__Upper_Respiratory_Symptoms.GetValueOrDefault(0);
                    lowvals += detail.C__Work_Loss_Days.GetValueOrDefault(0);

                    result.Summary.TotalHealthBenefitsValue_low += lowvals;

                    //add low to high this works
                    result.Summary.TotalHealthBenefitsValue_high += lowvals;

                    //and here they diverge
                    result.Summary.TotalHealthBenefitsValue_high += detail.C__Acute_Myocardial_Infarction_Nonfatal__high_.GetValueOrDefault(0);
                    result.Summary.TotalHealthBenefitsValue_high += detail.C__Mortality_All_Cause__high_.GetValueOrDefault(0);

                    result.Summary.TotalHealthBenefitsValue_low += detail.C__Acute_Myocardial_Infarction_Nonfatal__low_.GetValueOrDefault(0);
                    result.Summary.TotalHealthBenefitsValue_low += detail.C__Mortality_All_Cause__low_.GetValueOrDefault(0);
                }


            }

        }


        public static EmissionsSums forSummarizedControlEmissions(ref EmissionsSums emissions)
        {
            emissions.baseline.PrimaryKey = null;
            emissions.baseline.Columns.Remove("ID");
            emissions.baseline.Columns.Remove("typeindx");
            emissions.baseline.Columns.Remove("sourceindx");
            emissions.baseline.Columns.Remove("stid");
            emissions.baseline.Columns.Remove("cyid");
            emissions.baseline.Columns.Remove("TIER1");
            emissions.baseline.Columns.Remove("TIER2");
            emissions.baseline.Columns.Remove("TIER3");
            emissions.control.PrimaryKey = null;
            emissions.control.Columns.Remove("ID");
            emissions.control.Columns.Remove("typeindx");
            emissions.control.Columns.Remove("sourceindx");
            emissions.control.Columns.Remove("stid");
            emissions.control.Columns.Remove("cyid");
            emissions.control.Columns.Remove("TIER1");
            emissions.control.Columns.Remove("TIER2");
            emissions.control.Columns.Remove("TIER3");
            return emissions;
        }

        public static EmissionsSums forSummarizedEmissions(ref EmissionsSums emissions)
        {
            return forSummarizedControlEmissions(ref emissions);
        }

    }
}
