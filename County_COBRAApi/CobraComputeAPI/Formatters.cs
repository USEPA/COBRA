using CobraCompute;
using StackExchange.Redis;

namespace CobraComputeAPI
{
    public static class Formatters
    {
        public static void SummaryComposer(string filter, Cobra_Result result)
        {
            foreach (Cobra_ResultDetail detail in result.Impacts)
            {
                if ((filter == "0") || (filter == "00") || (filter == detail.FIPS.Substring(0, filter.Length)))
                { //check if FIPS state part matches in case of filter

                    result.Summary.Acute_Myocardial_Infarction_Nonfatal += detail.PM_Acute_Myocardial_Infarction_Nonfatal.GetValueOrDefault(0);
                    result.Summary.C__Acute_Myocardial_Infarction_Nonfatal += detail.C__PM_Acute_Myocardial_Infarction_Nonfatal.GetValueOrDefault(0);

                    result.Summary.ER_Visits_Asthma += detail.O3_ER_Visits_Asthma.GetValueOrDefault(0);
                    result.Summary.C__ER_Visits_Asthma += detail.C__O3_ER_Visits_Asthma.GetValueOrDefault(0);

                    result.Summary.HA_All_Respiratory += detail.O3_HA_All_Respiratory.GetValueOrDefault(0) + detail.PM_HA_All_Respiratory.GetValueOrDefault(0) + detail.PM_HA_Respiratory2.GetValueOrDefault(0);
                    result.Summary.C__HA_All_Respiratory += detail.C__O3_HA_All_Respiratory.GetValueOrDefault(0) + detail.C__PM_Resp_Hosp_Adm.GetValueOrDefault(0) + detail.C__PM_HA_Respiratory2.GetValueOrDefault(0);
                    /****** HA ALL RESP BREAKDOWN ********/
                    result.Summary.PM_HA_All_Respiratory += detail.PM_HA_All_Respiratory.GetValueOrDefault(0) + detail.PM_HA_Respiratory2.GetValueOrDefault(0); ;
                    result.Summary.C__PM_HA_All_Respiratory += detail.C__PM_Resp_Hosp_Adm.GetValueOrDefault(0) + detail.C__PM_HA_Respiratory2.GetValueOrDefault(0); ;
                    result.Summary.O3_HA_All_Respiratory += detail.O3_HA_All_Respiratory.GetValueOrDefault(0);
                    result.Summary.C__O3_HA_All_Respiratory += detail.C__O3_HA_All_Respiratory.GetValueOrDefault(0); 
                    /****** END HA ALL RESP BREAKDOWN ********/

                    result.Summary.Minor_Restricted_Activity_Days += detail.PM_Minor_Restricted_Activity_Days.GetValueOrDefault(0);
                    result.Summary.C__Minor_Restricted_Activity_Days += detail.C__PM_Minor_Restricted_Activity_Days.GetValueOrDefault(0);

                    result.Summary.Incidence_Lung_Cancer += detail.PM_Incidence_Lung_Cancer.GetValueOrDefault(0);
                    result.Summary.C__Incidence_Lung_Cancer += detail.C__PM_Incidence_Lung_Cancer.GetValueOrDefault(0);

                    result.Summary.HA_Cardio_Cerebro_and_Peripheral_Vascular_Disease += detail.PM_HA_Cardio_Cerebro_and_Peripheral_Vascular_Disease.GetValueOrDefault(0);
                    result.Summary.C__HA_Cardio_Cerebro_and_Peripheral_Vascular_Disease += detail.C__PM_HA_Cardio_Cerebro_and_Peripheral_Vascular_Disease.GetValueOrDefault(0);

                    result.Summary.HA_Alzheimers_Disease += detail.PM_HA_Alzheimers_Disease.GetValueOrDefault(0);
                    result.Summary.C__HA_Alzheimers_Disease += detail.C__PM_HA_Alzheimers_Disease.GetValueOrDefault(0);

                    result.Summary.HA_Parkinsons_Disease += detail.PM_HA_Parkinsons_Disease.GetValueOrDefault(0);
                    result.Summary.C__HA_Parkinsons_Disease += detail.C__PM_HA_Parkinsons_Disease.GetValueOrDefault(0);

                    result.Summary.Incidence_Stroke += detail.PM_Incidence_Stroke.GetValueOrDefault(0);
                    result.Summary.C__Incidence_Stroke += detail.C__PM_Incidence_Stroke.GetValueOrDefault(0);


                    result.Summary.Incidence_Out_of_Hospital_Cardiac_Arrest += detail.PM_Incidence_Out_of_Hospital_Cardiac_Arrest.GetValueOrDefault(0);
                    result.Summary.C__Incidence_Out_of_Hospital_Cardiac_Arrest += detail.C__PM_Incidence_Out_of_Hospital_Cardiac_Arrest.GetValueOrDefault(0);

                    result.Summary.Incidence_Asthma += detail.PM_Incidence_Asthma.GetValueOrDefault(0) + detail.O3_Incidence_Asthma.GetValueOrDefault(0);
                    result.Summary.C__Incidence_Asthma += detail.C__PM_Incidence_Asthma.GetValueOrDefault(0) + detail.C__O3_Incidence_Asthma.GetValueOrDefault(0);

                    /** ASTHMA BREAKDOWN ****/
                    result.Summary.PM_Incidence_Asthma += detail.PM_Incidence_Asthma.GetValueOrDefault(0);
                    result.Summary.C__PM_Incidence_Asthma += detail.C__PM_Incidence_Asthma.GetValueOrDefault(0);
                    result.Summary.O3_Incidence_Asthma += detail.O3_Incidence_Asthma.GetValueOrDefault(0);
                    result.Summary.C__O3_Incidence_Asthma += detail.C__O3_Incidence_Asthma.GetValueOrDefault(0);
                    /** END ASTHMA BREAKDOWN ***/

                    result.Summary.Asthma_Symptoms += detail.PM_Asthma_Symptoms_Albuterol_use.GetValueOrDefault(0) + detail.O3_Asthma_Symptoms_Chest_Tightness.GetValueOrDefault(0) + detail.O3_Asthma_Symptoms_Cough.GetValueOrDefault(0) + detail.O3_Asthma_Symptoms_Shortness_of_Breath.GetValueOrDefault(0) + detail.O3_Asthma_Symptoms_Wheeze.GetValueOrDefault(0);
                    result.Summary.C__Asthma_Symptoms += detail.C__PM_Asthma_Symptoms_Albuterol_use.GetValueOrDefault(0) + detail.C__O3_Asthma_Symptoms_Chest_Tightness.GetValueOrDefault(0) + detail.C__O3_Asthma_Symptoms_Cough.GetValueOrDefault(0) + detail.C__O3_Asthma_Symptoms_Shortness_of_Breath.GetValueOrDefault(0) + detail.C__O3_Asthma_Symptoms_Wheeze.GetValueOrDefault(0);

                    /***** Asthma Symptoms Breakdown **/
                    result.Summary.PM_Asthma_Symptoms_Albuterol_use += detail.PM_Asthma_Symptoms_Albuterol_use.GetValueOrDefault(0);
                    result.Summary.C__PM_Asthma_Symptoms_Albuterol_use += detail.C__PM_Asthma_Symptoms_Albuterol_use.GetValueOrDefault(0);
                    result.Summary.O3_Asthma_Symptoms_Chest_Tightness += detail.O3_Asthma_Symptoms_Chest_Tightness.GetValueOrDefault(0);
                    result.Summary.C__O3_Asthma_Symptoms_Chest_Tightness += detail.C__O3_Asthma_Symptoms_Chest_Tightness.GetValueOrDefault(0);
                    result.Summary.O3_Asthma_Symptoms_Cough += detail.O3_Asthma_Symptoms_Cough.GetValueOrDefault(0);
                    result.Summary.C__O3_Asthma_Symptoms_Cough += detail.C__O3_Asthma_Symptoms_Cough.GetValueOrDefault(0);
                    result.Summary.O3_Asthma_Symptoms_Shortness_of_Breath += detail.O3_Asthma_Symptoms_Shortness_of_Breath.GetValueOrDefault(0);
                    result.Summary.C__O3_Asthma_Symptoms_Shortness_of_Breath += detail.C__O3_Asthma_Symptoms_Shortness_of_Breath.GetValueOrDefault(0);
                    result.Summary.O3_Asthma_Symptoms_Wheeze += detail.O3_Asthma_Symptoms_Wheeze.GetValueOrDefault(0);
                    result.Summary.C__O3_Asthma_Symptoms_Wheeze += detail.C__O3_Asthma_Symptoms_Wheeze.GetValueOrDefault(0);
                    /***** End of Asthma Sypmotms Breakdown ***/


                    result.Summary.Incidence_Hay_Fever_Rhinitis += detail.PM_Incidence_Hay_Fever_Rhinitis.GetValueOrDefault(0) + detail.O3_Incidence_Hay_Fever_Rhinitis.GetValueOrDefault(0);
                    result.Summary.C__Incidence_Hay_Fever_Rhinitis += detail.C__PM_Incidence_Hay_Fever_Rhinitis.GetValueOrDefault(0) + detail.C__O3_Incidence_Hay_Fever_Rhinitis.GetValueOrDefault(0);
                    /****** HAY FEVER BREAKDOWN *****/
                    result.Summary.PM_Incidence_Hay_Fever_Rhinitis += detail.PM_Incidence_Hay_Fever_Rhinitis.GetValueOrDefault(0);
                    result.Summary.C__PM_Incidence_Hay_Fever_Rhinitis += detail.C__PM_Incidence_Hay_Fever_Rhinitis.GetValueOrDefault(0);
                    result.Summary.O3_Incidence_Hay_Fever_Rhinitis += detail.O3_Incidence_Hay_Fever_Rhinitis.GetValueOrDefault(0);
                    result.Summary.C__O3_Incidence_Hay_Fever_Rhinitis += detail.C__O3_Incidence_Hay_Fever_Rhinitis.GetValueOrDefault(0);
                    /****** END HAY FEVER BREAKDOWN ***********/

                    result.Summary.ER_visits_All_Cardiac_Outcomes += detail.PM_ER_visits_All_Cardiac_Outcomes.GetValueOrDefault(0);
                    result.Summary.C__ER_visits_All_Cardiac_Outcomes += detail.C__PM_ER_visits_All_Cardiac_Outcomes.GetValueOrDefault(0);

                    result.Summary.ER_visits_respiratory += detail.PM_ER_visits_respiratory.GetValueOrDefault(0) + detail.O3_ER_visits_respiratory.GetValueOrDefault(0);
                    result.Summary.C__ER_visits_respiratory += detail.C__PM_ER_visits_respiratory.GetValueOrDefault(0) + detail.C__O3_ER_visits_respiratory.GetValueOrDefault(0);
                    /****** ER RESP BREAKDOWN *****/
                    result.Summary.PM_ER_visits_respiratory += detail.PM_ER_visits_respiratory.GetValueOrDefault(0);
                    result.Summary.C__PM_ER_visits_respiratory += detail.C__PM_ER_visits_respiratory.GetValueOrDefault(0);
                    result.Summary.O3_ER_visits_respiratory += detail.O3_ER_visits_respiratory.GetValueOrDefault(0);
                    result.Summary.C__O3_ER_visits_respiratory += detail.C__O3_ER_visits_respiratory.GetValueOrDefault(0);
                    /****** END ER RESP BREAKDOWN ***********/

                    result.Summary.School_Loss_Days += detail.O3_School_Loss_Days.GetValueOrDefault(0);
                    result.Summary.C__School_Loss_Days += detail.C__O3_School_Loss_Days.GetValueOrDefault(0);

                    result.Summary.Work_Loss_Days += detail.PM_Work_Loss_Days.GetValueOrDefault(0);
                    result.Summary.C__Work_Loss_Days += detail.C__PM_Work_Loss_Days.GetValueOrDefault(0);



                    result.Summary.Infant_Mortality += detail.PM_Infant_Mortality.GetValueOrDefault(0);
                    result.Summary.C__Infant_Mortality += detail.C__PM_Infant_Mortality.GetValueOrDefault(0);

                    result.Summary.Mortality_All_Cause__low_ += detail.PM_Mortality_All_Cause__low_.GetValueOrDefault(0) + detail.O3_Mortality_Longterm_exposure.GetValueOrDefault(0) + +detail.O3_Mortality_Shortterm_exposure.GetValueOrDefault(0);
                    result.Summary.Mortality_All_Cause__high_ += detail.PM_Mortality_All_Cause__high_.GetValueOrDefault(0) + detail.O3_Mortality_Longterm_exposure.GetValueOrDefault(0) + detail.O3_Mortality_Shortterm_exposure.GetValueOrDefault(0);
                    result.Summary.C__Mortality_All_Cause__low_ += detail.C__PM_Mortality_All_Cause__low_.GetValueOrDefault(0) + detail.C__O3_Mortality_Longterm_exposure.GetValueOrDefault(0) + detail.C__O3_Mortality_Shortterm_exposure.GetValueOrDefault(0);
                    result.Summary.C__Mortality_All_Cause__high_ += detail.C__PM_Mortality_All_Cause__high_.GetValueOrDefault(0) + detail.C__O3_Mortality_Longterm_exposure.GetValueOrDefault(0) + detail.C__O3_Mortality_Shortterm_exposure.GetValueOrDefault(0);

                    /****** getting mortality breakdowns */
                    result.Summary.PM_Mortality_All_Cause__low_ += detail.PM_Mortality_All_Cause__low_.GetValueOrDefault(0);
                    result.Summary.PM_Mortality_All_Cause__high_ += detail.PM_Mortality_All_Cause__high_.GetValueOrDefault(0);

                    result.Summary.C__PM_Mortality_All_Cause__low_ += detail.C__PM_Mortality_All_Cause__low_.GetValueOrDefault(0);
                    result.Summary.C__PM_Mortality_All_Cause__high_ += detail.C__PM_Mortality_All_Cause__high_.GetValueOrDefault(0);

                    result.Summary.O3_Mortality_Longterm_exposure += detail.O3_Mortality_Longterm_exposure.GetValueOrDefault(0);
                    result.Summary.O3_Mortality_Shortterm_exposure += detail.O3_Mortality_Shortterm_exposure.GetValueOrDefault(0);
                    result.Summary.C__O3_Mortality_Longterm_exposure += detail.C__O3_Mortality_Longterm_exposure.GetValueOrDefault(0);
                    result.Summary.C__O3_Mortality_Shortterm_exposure += detail.C__O3_Mortality_Shortterm_exposure.GetValueOrDefault(0);
                    /**** end of mortality breakdowns ****/

                    double lowIncidence = (detail.PM_Acute_Myocardial_Infarction_Nonfatal.GetValueOrDefault(0)
                     + detail.PM_HA_All_Respiratory.GetValueOrDefault(0)
                     + detail.PM_Minor_Restricted_Activity_Days.GetValueOrDefault(0)
                     + detail.PM_Infant_Mortality.GetValueOrDefault(0)
                     + detail.PM_Work_Loss_Days.GetValueOrDefault(0)
                     + detail.PM_Incidence_Lung_Cancer.GetValueOrDefault(0)
                     + detail.PM_Incidence_Hay_Fever_Rhinitis.GetValueOrDefault(0)
                     + detail.PM_Incidence_Asthma.GetValueOrDefault(0)
                     + detail.PM_HA_Cardio_Cerebro_and_Peripheral_Vascular_Disease.GetValueOrDefault(0)
                     + detail.PM_HA_Alzheimers_Disease.GetValueOrDefault(0)
                     + detail.PM_HA_Parkinsons_Disease.GetValueOrDefault(0)
                     + detail.PM_Incidence_Stroke.GetValueOrDefault(0)
                     + detail.PM_Incidence_Out_of_Hospital_Cardiac_Arrest.GetValueOrDefault(0)
                     + detail.PM_Asthma_Symptoms_Albuterol_use.GetValueOrDefault(0)
                     + detail.PM_HA_Respiratory2.GetValueOrDefault(0)
                     + detail.PM_ER_visits_respiratory.GetValueOrDefault(0)
                     + detail.PM_ER_visits_All_Cardiac_Outcomes.GetValueOrDefault(0));

                    result.Summary.TotalPM_low += (lowIncidence + detail.PM_Mortality_All_Cause__low_.GetValueOrDefault(0));
                    result.Summary.TotalPM_high += (lowIncidence + detail.PM_Mortality_All_Cause__high_.GetValueOrDefault(0));



                    //now sort low high stuff, start with low bit that is equal to high bit
                    double lowvals = 0;
                    lowvals += detail.C__PM_Acute_Myocardial_Infarction_Nonfatal.GetValueOrDefault(0);
                    lowvals += detail.C__PM_Resp_Hosp_Adm.GetValueOrDefault(0);
                    lowvals += detail.C__PM_Minor_Restricted_Activity_Days.GetValueOrDefault(0);
                    lowvals += detail.C__PM_Infant_Mortality.GetValueOrDefault(0);
                    lowvals += detail.C__PM_Work_Loss_Days.GetValueOrDefault(0);
                    lowvals += detail.C__PM_Incidence_Lung_Cancer.GetValueOrDefault(0);
                    lowvals += detail.C__PM_Incidence_Hay_Fever_Rhinitis.GetValueOrDefault(0);
                    lowvals += detail.C__PM_Incidence_Asthma.GetValueOrDefault(0);
                    lowvals += detail.C__PM_HA_Cardio_Cerebro_and_Peripheral_Vascular_Disease.GetValueOrDefault(0);
                    lowvals += detail.C__PM_HA_Alzheimers_Disease.GetValueOrDefault(0);
                    lowvals += detail.C__PM_HA_Parkinsons_Disease.GetValueOrDefault(0);
                    lowvals += detail.C__PM_Incidence_Stroke.GetValueOrDefault(0);
                    lowvals += detail.C__PM_Incidence_Out_of_Hospital_Cardiac_Arrest.GetValueOrDefault(0);
                    lowvals += detail.C__PM_Asthma_Symptoms_Albuterol_use.GetValueOrDefault(0);
                    lowvals += detail.C__PM_HA_Respiratory2.GetValueOrDefault(0);
                    lowvals += detail.C__PM_ER_visits_respiratory.GetValueOrDefault(0);
                    lowvals += detail.C__PM_ER_visits_All_Cardiac_Outcomes.GetValueOrDefault(0);


                    result.Summary.TotalPMValue_low += lowvals;
                    result.Summary.TotalPMValue_high += lowvals;
                    //add mortality since they diverge here
                    result.Summary.TotalPMValue_low += detail.C__PM_Mortality_All_Cause__low_.GetValueOrDefault(0);
                    result.Summary.TotalPMValue_high += detail.C__PM_Mortality_All_Cause__high_.GetValueOrDefault(0);

                    lowvals += detail.C__O3_ER_visits_respiratory.GetValueOrDefault(0);
                    lowvals += detail.C__O3_HA_All_Respiratory.GetValueOrDefault(0);
                    lowvals += detail.C__O3_Incidence_Hay_Fever_Rhinitis.GetValueOrDefault(0);
                    lowvals += detail.C__O3_Incidence_Asthma.GetValueOrDefault(0);
                    lowvals += detail.C__O3_Asthma_Symptoms_Chest_Tightness.GetValueOrDefault(0);
                    lowvals += detail.C__O3_Asthma_Symptoms_Cough.GetValueOrDefault(0);
                    lowvals += detail.C__O3_Asthma_Symptoms_Shortness_of_Breath.GetValueOrDefault(0);
                    lowvals += detail.C__O3_Asthma_Symptoms_Wheeze.GetValueOrDefault(0);
                    lowvals += detail.C__O3_ER_Visits_Asthma.GetValueOrDefault(0);
                    lowvals += detail.C__O3_School_Loss_Days.GetValueOrDefault(0);
                    lowvals += detail.C__O3_Mortality_Longterm_exposure.GetValueOrDefault(0);
                    lowvals += detail.C__O3_Mortality_Shortterm_exposure.GetValueOrDefault(0);

                    //get total O3
                    result.Summary.TotalO3Value += (detail.C__O3_Asthma_Symptoms_Cough.GetValueOrDefault(0) + detail.C__O3_Asthma_Symptoms_Shortness_of_Breath.GetValueOrDefault(0) + detail.C__O3_Asthma_Symptoms_Wheeze.GetValueOrDefault(0) + detail.C__O3_ER_Visits_Asthma.GetValueOrDefault(0) + detail.C__O3_School_Loss_Days.GetValueOrDefault(0) + detail.C__O3_Mortality_Longterm_exposure.GetValueOrDefault(0) + detail.C__O3_Mortality_Shortterm_exposure.GetValueOrDefault(0)
                    + detail.C__O3_ER_visits_respiratory.GetValueOrDefault(0)
                    + detail.C__O3_HA_All_Respiratory.GetValueOrDefault(0)
                    + detail.C__O3_Incidence_Hay_Fever_Rhinitis.GetValueOrDefault(0)
                    + detail.C__O3_Incidence_Asthma.GetValueOrDefault(0)
                    + detail.C__O3_Asthma_Symptoms_Chest_Tightness.GetValueOrDefault(0));

                    result.Summary.TotalO3  += (detail.O3_Asthma_Symptoms_Cough.GetValueOrDefault(0) + detail.O3_Asthma_Symptoms_Shortness_of_Breath.GetValueOrDefault(0) + detail.O3_Asthma_Symptoms_Wheeze.GetValueOrDefault(0) + detail.O3_ER_Visits_Asthma.GetValueOrDefault(0) + detail.O3_School_Loss_Days.GetValueOrDefault(0) + detail.O3_Mortality_Longterm_exposure.GetValueOrDefault(0) + detail.O3_Mortality_Shortterm_exposure.GetValueOrDefault(0)
                    + detail.O3_ER_visits_respiratory.GetValueOrDefault(0)
                    + detail.O3_HA_All_Respiratory.GetValueOrDefault(0)
                    + detail.O3_Incidence_Hay_Fever_Rhinitis.GetValueOrDefault(0)
                    + detail.O3_Incidence_Asthma.GetValueOrDefault(0)
                    + detail.O3_Asthma_Symptoms_Chest_Tightness.GetValueOrDefault(0));


                    result.Summary.TotalHealthBenefitsValue_low += lowvals;

                    //add low to high this works
                    result.Summary.TotalHealthBenefitsValue_high += lowvals;

                    //and here they diverge
                    result.Summary.TotalHealthBenefitsValue_high += detail.C__PM_Mortality_All_Cause__high_.GetValueOrDefault(0);
                    result.Summary.TotalHealthBenefitsValue_low += detail.C__PM_Mortality_All_Cause__low_.GetValueOrDefault(0);
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
