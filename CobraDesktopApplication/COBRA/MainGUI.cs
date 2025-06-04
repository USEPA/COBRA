using ClosedXML.Excel;
using cobra_console.units;
using CsvHelper;
using DotSpatial.Controls;
using DotSpatial.Data;
using DotSpatial.Symbology;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Media;
using System.Windows.Forms;
using Telerik.WinControls;
using Telerik.WinControls.UI;
using Telerik.WinControls.UI.Export;
using static COBRA.cobraDataSet;

namespace COBRA
{
    public partial class MainGUI : Form
    {
        private bool lockout = false;
        private bool rblockout = false;

        private int datayear = 0;

        private Emissionssummary _base = new Emissionssummary();
        private Emissionssummary _control = new Emissionssummary();
        private Emissionssummary _delta = new Emissionssummary();

        private DataRow[] selectedemissions = null;

        private string popfile = "";
        private string incidencefile = "";
        private string valuationfile = "";
        private string crfile = "";

        private bool runexecuted = false;
        private bool _dirty = false;
        //nested dictionary containing each row
        private Dictionary<string, Dictionary<string, string>> SummaryTableVals = new Dictionary<string, Dictionary<string, string>>();
        private Dictionary<string, string> colPropertyDict = new Dictionary<string, string>
        {
            {"ALLMORTLOW", "Mortality*" },
            {"ALLMORTHIGH", "Mortality*"},
            {"ALLMORTLOWVAL", "Mortality*" },
            {"ALLMORTHIGHVAL", "Mortality*" },
{"_PM_Mortality_All_Cause__low_", "Mortality, All Cause*"},
{"_PM_Mortality_All_Cause__high_", "Mortality, All Cause*"},
{"___PM_Mortality_All_Cause__low_", "Mortality, All Cause*"},
{"___PM_Mortality_All_Cause__high_", "Mortality, All Cause*"},
            { "O3_Mortality_Shortterm_exposure", "Mortality, O3 Short-term Exposure"},
            { "___O3_Mortality_Shortterm_exposure",  "Mortality, O3 Short-term Exposure"},
            { "O3_Mortality_Longterm_exposure", "Mortality, O3 Long-term Exposure"},
            { "___O3_Mortality_Longterm_exposure", "Mortality, O3 Long-term Exposure" },
            {"PMHAALLRESP","Hospital Admits from PM2.5"},
            {"PMHAALLRESPVAL","Hospital Admits from PM2.5"},

                        {"O3_HA_All_Respiratory","Hospital Admits from O3"},
            {"___O3_HA_All_Respiratory","Hospital Admits from O3"},



            {"PM_ER_visits_respiratory", "Respiratory Visits from PM2.5"},
             {"___PM_ER_visits_respiratory", "Respiratory Visits from PM2.5"},

                         {"O3_ER_visits_respiratory", "Respiratory Visits from O3"},
             {"___O3_ER_visits_respiratory", "Respiratory Visits from O3"},

            { "PM_Incidence_Asthma","Asthma Onset from PM2.5"},
             {"___PM_Incidence_Asthma","Asthma Onset from PM2.5"},

            {"O3_Incidence_Asthma", "Asthma Onset from O3"},
            {"___O3_Incidence_Asthma", "Asthma Onset from O3"},

            {"PM_Asthma_Symptoms_Albuterol_use", "Albuterol Use"},
              {"___PM_Asthma_Symptoms_Albuterol_use", "Albuterol Use"},

            {"O3_Asthma_Symptoms_Chest_Tightness" , "Chest Tightness"},
        { "___O3_Asthma_Symptoms_Chest_Tightness" , "Chest Tightness"},
            {"O3_Asthma_Symptoms_Cough", "Cough"},
            {"___O3_Asthma_Symptoms_Cough", "Cough"},
            {  "O3_Asthma_Symptoms_Shortness_of_Breath","Shortness of Breath"},
            {  "___O3_Asthma_Symptoms_Shortness_of_Breath","Shortness of Breath"},

            { "O3_Asthma_Symptoms_Wheeze","Wheeze" },
              { "___O3_Asthma_Symptoms_Wheeze","Wheeze" },

            { "PM_Incidence_Hay_Fever_Rhinitis", "Hay Fever/Rhinitis from PM2.5"},
             {"___PM_Incidence_Hay_Fever_Rhinitis", "Hay Fever/Rhinitis from PM2.5"},

            { "O3_Incidence_Hay_Fever_Rhinitis", "Hay Fever/Rhinitis from O3"},
             { "___O3_Incidence_Hay_Fever_Rhinitis", "Hay Fever/Rhinitis from O3"},
{ "_PM_Acute_Myocardial_Infarction_Nonfatal","Nonfatal Heart Attacks" },
            {"___PM_Acute_Myocardial_Infarction_Nonfatal", "Nonfatal Heart Attacks"},
            {"PM_Infant_Mortality", "Infant Mortality" },
            {"___PM_Infant_Mortality", "Infant Mortality"},
            {"HAALLRESP", "Hospital Admits, All Respiratory"},
            {"HAALLRESPVAL", "Hospital Admits, All Respiratory"},
            {"TOTALERRESP", "ER Visits, Respiratory"},
            {"TOTALERRESPVAL", "ER Visits, Respiratory"},
            {"TOTALINCIDENCEASTHMA", "Asthma Onset"},
            {"TOTALINCIDENCEASTHMAVAL", "Asthma Onset"},
            {"ALLASTHMA","Asthma Symptoms"},
            {"ALLASTHMAVAL", "Asthma Symptoms"},
            {"O3_ER_Visits_Asthma","ER Vists, Asthma"},
            {"___O3_ER_Visits_Asthma", "ER Vists, Asthma"},
            {"PM_Incidence_Lung_Cancer","Lung Cancer Incidence"},
            {"___PM_Incidence_Lung_Cancer", "Lung Cancer Incidence"},
            {"PM_HA_Cardio_Cerebro_and_Peripheral_Vascular_Disease","Hospital Admits, Cardiovascular"},
            {"___PM_HA_Cardio_Cerebro_and_Peripheral_Vascular_Disease", "Hospital Admits, Cardiovascular"},
            {"PM_HA_Alzheimers_Disease","Hospital Admits, Alzheimers"},
            {"___PM_HA_Alzheimers_Disease", "Hospital Admits, Alzheimers"},
            {"PM_HA_Parkinsons_Disease","Hospital Admits, Parkinsons"},
            {"___PM_HA_Parkinsons_Disease", "Hospital Admits, Parkinsons"},
            {"PM_Incidence_Stroke","Stroke Incidence"},
            {"___PM_Incidence_Stroke", "Stroke Incidence"},
            {"TOTALHAYFEVER","Hay Fever/Rhinitis Incidence"},
            {"TOTALHAYFEVERVAL", "Hay Fever/Rhinitis Incidence"},
            {"PM_Incidence_Out_of_Hospital_Cardiac_Arrest","Cardiac Arrest, Out of Hospital"},
            {"___PM_Incidence_Out_of_Hospital_Cardiac_Arrest", "Cardiac Arrest, Out of Hospital"},
            {"PM_ER_visits_All_Cardiac_Outcomes","ER Visits, All Cardiac"},
            {"___PM_ER_visits_All_Cardiac_Outcomes", "ER Visits, All Cardiac"},
            {"PM_Minor_Restricted_Activity_Days","Minor Restricted Activity Days"},
            {"___PM_Minor_Restricted_Activity_Days", "Minor Restricted Activity Days"},
            {"O3_School_Loss_Days","School Loss Days"},
            {"___O3_School_Loss_Days","School Loss Days"},
            {"PM_Work_Loss_Days","Work Loss Days"},
            {"___PM_Work_Loss_Days","Work Loss Days"},
            {"TOTALHELOW","Total Health Effects"},
            {"TOTALHEHIGH","Total Health Effects"},
        };


        string[] outputCols =
                      {
    "ID",
    "destindx",
    "FIPS",
    "STATE",
    "COUNTY",
    "BASE_FINAL_PM",
    "CTRL_FINAL_PM",
    "DELTA_FINAL_PM",
    "BASE_FINAL_O3",
    "CTRL_FINAL_O3",
    "DELTA_FINAL_O3",
    "TOTALHELOW",
    "TOTALHEHIGH",
    "ALLMORTLOW",
    "ALLMORTLOWVAL",
    "ALLMORTHIGH",
    "ALLMORTHIGHVAL",
    "_PM_Mortality_All_Cause__low_",
    "___PM_Mortality_All_Cause__low_",
    "_PM_Mortality_All_Cause__high_",
    "___PM_Mortality_All_Cause__high_",
    "PM_Infant_Mortality",
    "___PM_Infant_Mortality",
    "TOTALO3MORT",
    "TOTALO3MORTVAL",
    "O3_Mortality_Shortterm_exposure",
    "___O3_Mortality_Shortterm_exposure",
    "O3_Mortality_Longterm_exposure",
    "___O3_Mortality_Longterm_exposure",
    "ALLASTHMA",
    "ALLASTHMAVAL",
    "PM_Asthma_Symptoms_Albuterol_use",
    "___PM_Asthma_Symptoms_Albuterol_use",
     "O3_Asthma_Symptoms_Chest_Tightness",
    "___O3_Asthma_Symptoms_Chest_Tightness",
     "O3_Asthma_Symptoms_Cough",
    "___O3_Asthma_Symptoms_Cough",
     "O3_Asthma_Symptoms_Shortness_of_Breath",
    "___O3_Asthma_Symptoms_Shortness_of_Breath",
         "O3_Asthma_Symptoms_Wheeze",
    "___O3_Asthma_Symptoms_Wheeze",
    "TOTALINCIDENCEASTHMA",
    "TOTALINCIDENCEASTHMAVAL",
        "PM_Incidence_Asthma",
    "___PM_Incidence_Asthma",
        "O3_Incidence_Asthma",
    "___O3_Incidence_Asthma",
    "TOTALHAYFEVER",
    "TOTALHAYFEVERVAL",
        "PM_Incidence_Hay_Fever_Rhinitis",
    "___PM_Incidence_Hay_Fever_Rhinitis",
        "O3_Incidence_Hay_Fever_Rhinitis",
    "___O3_Incidence_Hay_Fever_Rhinitis",
    "TOTALERRESP",
    "TOTALERRESPVAL",
        "PM_ER_visits_respiratory",
    "___PM_ER_visits_respiratory",
        "O3_ER_visits_respiratory",
    "___O3_ER_visits_respiratory",
    "HAALLRESP",
       "HAALLRESPVAL",
        "PMHAALLRESP",
    "PMHAALLRESPVAL",
    "O3_HA_All_Respiratory",
    "___O3_HA_All_Respiratory",
    "_PM_Acute_Myocardial_Infarction_Nonfatal",
    "___PM_Acute_Myocardial_Infarction_Nonfatal",
    "PM_Minor_Restricted_Activity_Days",
    "___PM_Minor_Restricted_Activity_Days",
    "PM_Work_Loss_Days",
    "___PM_Work_Loss_Days",
    "PM_Incidence_Lung_Cancer",
    "___PM_Incidence_Lung_Cancer",
    "PM_HA_Cardio_Cerebro_and_Peripheral_Vascular_Disease",
    "___PM_HA_Cardio_Cerebro_and_Peripheral_Vascular_Disease",
    "PM_HA_Alzheimers_Disease",
    "___PM_HA_Alzheimers_Disease",
    "PM_HA_Parkinsons_Disease",
    "___PM_HA_Parkinsons_Disease",
    "PM_Incidence_Stroke",
    "___PM_Incidence_Stroke",
    "PM_Incidence_Out_of_Hospital_Cardiac_Arrest",
    "___PM_Incidence_Out_of_Hospital_Cardiac_Arrest",
    "PM_ER_visits_All_Cardiac_Outcomes",
    "___PM_ER_visits_All_Cardiac_Outcomes",
        "O3_ER_Visits_Asthma",
    "___O3_ER_Visits_Asthma",
     "O3_School_Loss_Days",
    "___O3_School_Loss_Days"};


        [Export("Shell", typeof(ContainerControl))]
        private static ContainerControl Shell;
        public MainGUI()
        {
            InitializeComponent();
            comboBox_States.SelectedIndexChanged += comboBox_States_SelectedIndexChanged;
            comboBox_Counties.SelectedIndexChanged += comboBox_Counties_SelectedIndexChanged;
            if (DesignMode) return;
            Shell = this;
            //appManager1.LoadExtensions();

            map1.AddLayer("shp/cb_2015_us_county_20m_rev.shp");
            map2.AddLayer("shp/cb_2015_us_county_20m_rev.shp");

            spatialToolStrip1.Items[0].Visible = false;
            spatialToolStrip1.Items[1].Visible = false;
            spatialToolStrip1.Items[2].Visible = false;
            spatialToolStrip1.Items[5].Visible = false;
            spatialToolStrip1.Items[17].Visible = false;
            spatialToolStrip1.Items[18].Visible = false;

            spatialToolStrip2.Items[0].Visible = false;
            spatialToolStrip2.Items[1].Visible = false;
            spatialToolStrip2.Items[2].Visible = false;
            spatialToolStrip2.Items[5].Visible = false;
            spatialToolStrip2.Items[17].Visible = false;
            spatialToolStrip2.Items[18].Visible = false;

            // creating the base template for filling out the final results summary table, eventually their values will retrieved and added in getSumaryTotals(state, county);
            SummaryTableVals["Mortality*"] = new Dictionary<string, string> {
            {"lowproperty", "ALLMORTLOW"},
             {"highproperty", "ALLMORTHIGH"},
            {"lowvalproperty", "ALLMORTLOWVAL"},
            {"highvalproperty", "ALLMORTHIGHVAL"},
            {"low", "0"},
            {"high", "0"},
            {"lowval", "0"},
            {"highval", "0"},
            {"pollutant", "PM2.5 + O3" }
        };
            SummaryTableVals["Mortality, All Cause*"] = new Dictionary<string, string> {
            {"lowproperty", "_PM_Mortality_All_Cause__low_"},
             {"highproperty","_PM_Mortality_All_Cause__high_"},
            {"lowvalproperty", "___PM_Mortality_All_Cause__low_"},
            {"highvalproperty", "___PM_Mortality_All_Cause__high_"},
            {"low", "0"},
            {"high", "0"},
            {"lowval", "0"},
            {"highval", "0"},
            {"pollutant", "PM2.5" }
        };

            SummaryTableVals["Mortality, O3 Short-term Exposure"] = new Dictionary<string, string> {
            {"lowproperty", "O3_Mortality_Shortterm_exposure"},
             {"highproperty","O3_Mortality_Shortterm_exposure"},
            {"lowvalproperty", "___O3_Mortality_Shortterm_exposure"},
            {"highvalproperty", "___O3_Mortality_Shortterm_exposure"},
            {"low", "0"},
            {"high", "0"},
            {"lowval", "0"},
            {"highval", "0"},
            {"pollutant", "O3" }
        };
            SummaryTableVals["Mortality, O3 Long-term Exposure"] = new Dictionary<string, string> {
            {"lowproperty", "O3_Mortality_Longterm_exposure"},
             {"highproperty","O3_Mortality_Longterm_exposure"},
            {"lowvalproperty",  "___O3_Mortality_Longterm_exposure"},
            {"highvalproperty",  "___O3_Mortality_Longterm_exposure"},
            {"low", "0"},
            {"high", "0"},
            {"lowval", "0"},
            {"highval", "0"},
            {"pollutant", "O3" }
        };

            SummaryTableVals["Nonfatal Heart Attacks"] = new Dictionary<string, string> {
            {"lowproperty", "_PM_Acute_Myocardial_Infarction_Nonfatal"},
             {"highproperty", "_PM_Acute_Myocardial_Infarction_Nonfatal"},
            {"lowvalproperty", "___PM_Acute_Myocardial_Infarction_Nonfatal"},
            {"highvalproperty", "___PM_Acute_Myocardial_Infarction_Nonfatal"},
            {"low", "0"},
            {"high", "0"},
            {"lowval", "0"},
            {"highval", "0"},
            {"pollutant", "PM2.5" }
        };
            SummaryTableVals["Infant Mortality"] = new Dictionary<string, string> {
            {"lowproperty", "PM_Infant_Mortality"},
             {"highproperty", "PM_Infant_Mortality"},
            {"lowvalproperty", "___PM_Infant_Mortality"},
            {"highvalproperty", "___PM_Infant_Mortality"},
            {"low", "0"},
            {"high", "0"},
            {"lowval", "0"},
            {"highval", "0"},
            {"pollutant", "PM2.5" }
        };

            SummaryTableVals["Hospital Admits, All Respiratory"] = new Dictionary<string, string> {
            {"lowproperty", "HAALLRESP"},
             {"highproperty", "HAALLRESP"},
            {"lowvalproperty", "HAALLRESPVAL"},
            {"highvalproperty", "HAALLRESPVAL"},
            {"low", "0"},
            {"high", "0"},
            {"lowval", "0"},
            {"highval", "0"},
            {"pollutant", "PM2.5 + O3" }
        };
            SummaryTableVals["Hospital Admits from PM2.5"] = new Dictionary<string, string> {
            {"lowproperty", "PMHAALLRESP"},
             {"highproperty", "PMHAALLRESP"},
            {"lowvalproperty", "PMHAALLRESPVAL"},
            {"highvalproperty", "PMHAALLRESPVAL"},
            {"low", "0"},
            {"high", "0"},
            {"lowval", "0"},
            {"highval", "0"},
            {"pollutant", "PM2.5" }
        };

            SummaryTableVals["Hospital Admits from O3"] = new Dictionary<string, string> {
            {"lowproperty", "O3_HA_All_Respiratory"},
             {"highproperty", "O3_HA_All_Respiratory"},
            {"lowvalproperty", "___O3_HA_All_Respiratory"},
            {"highvalproperty","___O3_HA_All_Respiratory"},
            {"low", "0"},
            {"high", "0"},
            {"lowval", "0"},
            {"highval", "0"},
            {"pollutant", "O3" }
        };



            SummaryTableVals["ER Visits, Respiratory"] = new Dictionary<string, string> {
            {"lowproperty", "TOTALERRESP"},
             {"highproperty", "TOTALERRESP"},
            {"lowvalproperty", "TOTALERRESPVAL"},
            {"highvalproperty", "TOTALERRESPVAL"},
            {"low", "0"},
            {"high", "0"},
            {"lowval", "0"},
            {"highval", "0"},
            {"pollutant", "PM2.5 + O3" }
        };
            SummaryTableVals["Respiratory Visits from PM2.5"] = new Dictionary<string, string> {
            {"lowproperty", "PM_ER_visits_respiratory"},
             {"highproperty",  "PM_ER_visits_respiratory"},
            {"lowvalproperty",   "___PM_ER_visits_respiratory"},
            {"highvalproperty",  "___PM_ER_visits_respiratory"},
            {"low", "0"},
            {"high", "0"},
            {"lowval", "0"},
            {"highval", "0"},
            {"pollutant", "PM2.5" }
        };
            SummaryTableVals["Respiratory Visits from O3"] = new Dictionary<string, string> {
            {"lowproperty",  "O3_ER_visits_respiratory"},
             {"highproperty",  "O3_ER_visits_respiratory"},
            {"lowvalproperty", "___O3_ER_visits_respiratory"},
            {"highvalproperty","___O3_ER_visits_respiratory"},
            {"low", "0"},
            {"high", "0"},
            {"lowval", "0"},
            {"highval", "0"},
            {"pollutant", "O3" }
        };
            SummaryTableVals["Asthma Onset"] = new Dictionary<string, string> {
            {"lowproperty", "TOTALINCIDENCEASTHMA"},
             {"highproperty", "TOTALINCIDENCEASTHMA"},
            {"lowvalproperty", "TOTALINCIDENCEASTHMAVAL"},
            {"highvalproperty", "TOTALINCIDENCEASTHMAVAL"},
            {"low", "0"},
            {"high", "0"},
            {"lowval", "0"},
            {"highval", "0"},
            {"pollutant", "PM2.5 + O3" }
        };
            SummaryTableVals["Asthma Onset from PM2.5"] = new Dictionary<string, string> {
            {"lowproperty","PM_Incidence_Asthma"},
             {"highproperty",  "PM_Incidence_Asthma"},
            {"lowvalproperty",  "___PM_Incidence_Asthma"},
            {"highvalproperty", "___PM_Incidence_Asthma"},
            {"low", "0"},
            {"high", "0"},
            {"lowval", "0"},
            {"highval", "0"},
            {"pollutant", "PM2.5" }
        };
            SummaryTableVals["Asthma Onset from O3"] = new Dictionary<string, string> {
            {"lowproperty", "O3_Incidence_Asthma"},
             {"highproperty", "O3_Incidence_Asthma"},
            {"lowvalproperty", "___O3_Incidence_Asthma"},
            {"highvalproperty","___O3_Incidence_Asthma"},
            {"low", "0"},
            {"high", "0"},
            {"lowval", "0"},
            {"highval", "0"},
            {"pollutant", "O3" }
        };

            SummaryTableVals["Asthma Symptoms"] = new Dictionary<string, string> {
            {"lowproperty", "ALLASTHMA"},
             {"highproperty", "ALLASTHMA"},
            {"lowvalproperty", "ALLASTHMAVAL"},
            {"highvalproperty", "ALLASTHMAVAL"},
            {"low", "0"},
            {"high", "0"},
            {"lowval", "0"},
            {"highval", "0"},
            {"pollutant", "PM2.5 + O3" }
        };

            SummaryTableVals["Albuterol Use"] = new Dictionary<string, string> {
            {"lowproperty", "PM_Asthma_Symptoms_Albuterol_use"},
             {"highproperty", "PM_Asthma_Symptoms_Albuterol_use"},
            {"lowvalproperty", "___PM_Asthma_Symptoms_Albuterol_use"},
            {"highvalproperty", "___PM_Asthma_Symptoms_Albuterol_use"},
            {"low", "0"},
            {"high", "0"},
            {"lowval", "0"},
            {"highval", "0"},
            {"pollutant", "PM2.5" }
        };
            SummaryTableVals["Chest Tightness"] = new Dictionary<string, string> {
            {"lowproperty", "O3_Asthma_Symptoms_Chest_Tightness"},
             {"highproperty","O3_Asthma_Symptoms_Chest_Tightness"},
            {"lowvalproperty", "___O3_Asthma_Symptoms_Chest_Tightness"},
            {"highvalproperty", "___O3_Asthma_Symptoms_Chest_Tightness"},
            {"low", "0"},
            {"high", "0"},
            {"lowval", "0"},
            {"highval", "0"},
            {"pollutant", "O3" }
        };
            SummaryTableVals["Cough"] = new Dictionary<string, string> {
            {"lowproperty", "O3_Asthma_Symptoms_Cough"},
             {"highproperty","O3_Asthma_Symptoms_Cough"},
            {"lowvalproperty", "___O3_Asthma_Symptoms_Cough"},
            {"highvalproperty", "___O3_Asthma_Symptoms_Cough"},
            {"low", "0"},
            {"high", "0"},
            {"lowval", "0"},
            {"highval", "0"},
            {"pollutant", "O3" }
        };
            SummaryTableVals["Shortness of Breath"] = new Dictionary<string, string> {
            {"lowproperty", "O3_Asthma_Symptoms_Shortness_of_Breath"},
             {"highproperty","O3_Asthma_Symptoms_Shortness_of_Breath"},
            {"lowvalproperty", "___O3_Asthma_Symptoms_Shortness_of_Breath"},
            {"highvalproperty", "___O3_Asthma_Symptoms_Shortness_of_Breath"},
            {"low", "0"},
            {"high", "0"},
            {"lowval", "0"},
            {"highval", "0"},
            {"pollutant", "O3" }
        };

            SummaryTableVals["Wheeze"] = new Dictionary<string, string> {
            {"lowproperty", "O3_Asthma_Symptoms_Wheeze"},
             {"highproperty","O3_Asthma_Symptoms_Wheeze"},
            {"lowvalproperty", "___O3_Asthma_Symptoms_Wheeze"},
            {"highvalproperty", "___O3_Asthma_Symptoms_Wheeze"},
            {"low", "0"},
            {"high", "0"},
            {"lowval", "0"},
            {"highval", "0"},
            {"pollutant", "O3" }
        };




            SummaryTableVals["ER Vists, Asthma"] = new Dictionary<string, string> {
            {"lowproperty", "O3_ER_Visits_Asthma"},
             {"highproperty", "O3_ER_Visits_Asthma"},
            {"lowvalproperty", "___O3_ER_Visits_Asthma"},
            {"highvalproperty", "___O3_ER_Visits_Asthma"},
            {"low", "0"},
            {"high", "0"},
            {"lowval", "0"},
            {"highval", "0"},
            {"pollutant", "O3" }
        };

            SummaryTableVals["Lung Cancer Incidence"] = new Dictionary<string, string> {
            {"lowproperty", "PM_Incidence_Lung_Cancer"},
             {"highproperty", "PM_Incidence_Lung_Cancer"},
            {"lowvalproperty", "___PM_Incidence_Lung_Cancer"},
            {"highvalproperty", "___PM_Incidence_Lung_Cancer"},
            {"low", "0"},
            {"high", "0"},
            {"lowval", "0"},
            {"highval", "0"},
            {"pollutant", "PM2.5" }
        };
            SummaryTableVals["Hospital Admits, Cardiovascular"] = new Dictionary<string, string> {
            {"lowproperty", "PM_HA_Cardio_Cerebro_and_Peripheral_Vascular_Disease"},
             {"highproperty", "PM_HA_Cardio_Cerebro_and_Peripheral_Vascular_Disease"},
            {"lowvalproperty", "___PM_HA_Cardio_Cerebro_and_Peripheral_Vascular_Disease"},
            {"highvalproperty", "___PM_HA_Cardio_Cerebro_and_Peripheral_Vascular_Disease"},
            {"low", "0"},
            {"high", "0"},
            {"lowval", "0"},
            {"highval", "0"},
            {"pollutant", "PM2.5" }
        };

            SummaryTableVals["Hospital Admits, Alzheimers"] = new Dictionary<string, string> {
            {"lowproperty", "PM_HA_Alzheimers_Disease"},
             {"highproperty", "PM_HA_Alzheimers_Disease"},
            {"lowvalproperty", "___PM_HA_Alzheimers_Disease"},
            {"highvalproperty", "___PM_HA_Alzheimers_Disease"},
            {"low", "0"},
            {"high", "0"},
            {"lowval", "0"},
            {"highval", "0"},
            {"pollutant", "PM2.5" }
        };
            SummaryTableVals["Hospital Admits, Parkinsons"] = new Dictionary<string, string> {
            {"lowproperty", "PM_HA_Parkinsons_Disease"},
             {"highproperty", "PM_HA_Parkinsons_Disease"},
            {"lowvalproperty", "___PM_HA_Parkinsons_Disease"},
            {"highvalproperty", "___PM_HA_Parkinsons_Disease"},
            {"low", "0"},
            {"high", "0"},
            {"lowval", "0"},
            {"highval", "0"},
            {"pollutant", "PM2.5" }
        };

            SummaryTableVals["Stroke Incidence"] = new Dictionary<string, string> {
            {"lowproperty", "PM_Incidence_Stroke"},
             {"highproperty", "PM_Incidence_Stroke"},
            {"lowvalproperty", "___PM_Incidence_Stroke"},
            {"highvalproperty", "___PM_Incidence_Stroke"},
            {"low", "0"},
            {"high", "0"},
            {"lowval", "0"},
            {"highval", "0"},
            {"pollutant", "PM2.5" }
        };
            SummaryTableVals["Hay Fever/Rhinitis Incidence"] = new Dictionary<string, string> {
            {"lowproperty", "TOTALHAYFEVER"},
             {"highproperty", "TOTALHAYFEVER"},
            {"lowvalproperty", "TOTALHAYFEVERVAL"},
            {"highvalproperty", "TOTALHAYFEVERVAL"},
            {"low", "0"},
            {"high", "0"},
            {"lowval", "0"},
            {"highval", "0"},
            {"pollutant", "PM2.5 + O3" }
        };
            SummaryTableVals["Hay Fever/Rhinitis from PM2.5"] = new Dictionary<string, string> {
            {"lowproperty",  "PM_Incidence_Hay_Fever_Rhinitis"},
             {"highproperty",  "PM_Incidence_Hay_Fever_Rhinitis"},
            {"lowvalproperty", "___PM_Incidence_Hay_Fever_Rhinitis"},
            {"highvalproperty","___PM_Incidence_Hay_Fever_Rhinitis"},
            {"low", "0"},
            {"high", "0"},
            {"lowval", "0"},
            {"highval", "0"},
            {"pollutant", "PM2.5" }
        };
            SummaryTableVals["Hay Fever/Rhinitis from O3"] = new Dictionary<string, string> {
            {"lowproperty",  "O3_Incidence_Hay_Fever_Rhinitis"},
             {"highproperty", "O3_Incidence_Hay_Fever_Rhinitis"},
            {"lowvalproperty", "___O3_Incidence_Hay_Fever_Rhinitis"},
            {"highvalproperty","___O3_Incidence_Hay_Fever_Rhinitis"},
            {"low", "0"},
            {"high", "0"},
            {"lowval", "0"},
            {"highval", "0"},
            {"pollutant", "O3" }
        };



            SummaryTableVals["Cardiac Arrest, Out of Hospital"] = new Dictionary<string, string> {
            {"lowproperty", "PM_Incidence_Out_of_Hospital_Cardiac_Arrest"},
             {"highproperty", "PM_Incidence_Out_of_Hospital_Cardiac_Arrest"},
            {"lowvalproperty", "___PM_Incidence_Out_of_Hospital_Cardiac_Arrest"},
            {"highvalproperty", "___PM_Incidence_Out_of_Hospital_Cardiac_Arrest"},
            {"low", "0"},
            {"high", "0"},
            {"lowval", "0"},
            {"highval", "0"},
            {"pollutant", "PM2.5" }
        };
            SummaryTableVals["ER Visits, All Cardiac"] = new Dictionary<string, string> {
            {"lowproperty", "PM_ER_visits_All_Cardiac_Outcomes"},
             {"highproperty", "PM_ER_visits_All_Cardiac_Outcomes"},
            {"lowvalproperty", "___PM_ER_visits_All_Cardiac_Outcomes"},
            {"highvalproperty", "___PM_ER_visits_All_Cardiac_Outcomes"},
            {"low", "0"},
            {"high", "0"},
            {"lowval", "0"},
            {"highval", "0"},
            {"pollutant", "PM2.5" }
        };
            SummaryTableVals["Minor Restricted Activity Days"] = new Dictionary<string, string> {
            {"lowproperty", "PM_Minor_Restricted_Activity_Days"},
             {"highproperty", "PM_Minor_Restricted_Activity_Days"},
            {"lowvalproperty", "___PM_Minor_Restricted_Activity_Days"},
            {"highvalproperty", "___PM_Minor_Restricted_Activity_Days"},
            {"low", "0"},
            {"high", "0"},
            {"lowval", "0"},
            {"highval", "0"},
            {"pollutant", "PM2.5" }
        };
            SummaryTableVals["School Loss Days"] = new Dictionary<string, string> {
            {"lowproperty", "O3_School_Loss_Days"},
             {"highproperty", "O3_School_Loss_Days"},
            {"lowvalproperty", "___O3_School_Loss_Days"},
            {"highvalproperty", "___O3_School_Loss_Days"},
            {"low", "0"},
            {"high", "0"},
            {"lowval", "0"},
            {"highval", "0"},
            {"pollutant", "O3" }
        };
            SummaryTableVals["Work Loss Days"] = new Dictionary<string, string> {
            {"lowproperty", "PM_Work_Loss_Days"},
             {"highproperty", "PM_Work_Loss_Days"},
            {"lowvalproperty", "___PM_Work_Loss_Days"},
            {"highvalproperty", "___PM_Work_Loss_Days"},
            {"low", "0"},
            {"high", "0"},
            {"lowval", "0"},
            {"highval", "0"},
            {"pollutant", "PM2.5" }
        };
            SummaryTableVals["Total PM Health Effects"] = new Dictionary<string, string> {
                /* for this specific result whe actually only care about lowval and highval (not low and high)*/
            {"lowproperty", ""},
             {"highproperty", ""},
            {"lowvalproperty", ""},
            {"highvalproperty", ""},
            {"low", "0"},
            {"high", "0"},
            {"lowval", "0"},
            {"highval", "0"},
            {"pollutant", "PM2.5" }
        };
            SummaryTableVals["Total O3 Health Effects"] = new Dictionary<string, string> {
                /* for this specific result whe actually only care about lowval and highval (not low and high)*/
            {"lowproperty", ""},
             {"highproperty", ""},
            {"lowvalproperty", ""},
            {"highvalproperty", ""},
            {"low", "0"},
            {"high", "0"},
            {"lowval", "0"},
            {"highval", "0"},
            {"pollutant", "PM2.5" }
        };
            SummaryTableVals["Total Health Effects"] = new Dictionary<string, string> {
                /* for this specific result whe actually only care about lowval and highval (not low and high)*/
            {"lowproperty", "TOTALHELOW"},
             {"highproperty", "TOTALHEHIGH"},
            {"lowvalproperty", "TOTALHELOW"},
            {"highvalproperty", "TOTALHEHIGH"},
            {"low", "0"},
            {"high", "0"},
            {"lowval", "0"},
            {"highval", "0"},
            {"pollutant", "PM2.5 + O3" }
        };


        }





        private string GetExcelColumnLetter(int columnNumber)
        {

            int dividend = columnNumber;
            string columnLetter = string.Empty;

            while (dividend > 0)
            {
                int modulo = (dividend - 1) % 26;
                columnLetter = Convert.ToChar('A' + modulo) + columnLetter;
                dividend = (dividend - modulo) / 26;
            }

            return columnLetter;


        }


        public void setDirty(bool isdirty)
        {
            _dirty = isdirty;
            warninglabel.Visible = _dirty && runexecuted;
        }

        private void btnNext_Click(object sender, EventArgs e)
        {
            tabControl_Main.SelectedIndex++;
        }

        private void btnPrevious_Click(object sender, EventArgs e)
        {
            tabControl_Main.SelectedIndex--;
        }

        private void completedataload(int year)
        {
            cobraDataSet.SYS_Emissions.BeginLoadData();
            sYS_EmissionsTableAdapter.Fill(cobraDataSet.SYS_Emissions);
            cobraDataSet.SYS_Emissions.EndLoadData();
            PopulateTrees();
            setDirty(true);
            sYSEmissionsBindingSource.ResetBindings(false);
            //advancedDataGridView1.LoadFilterAndSort("(Modified=True)", "");
            sYSEmissionsBindingSource.Filter = "(Modified=True)";
            datayear = year;
            SystemSounds.Exclamation.Play();
        }

        private void btnEmissions_Click(object sender, EventArgs e)
        {
            if (openFileDialogBaseline.ShowDialog() == DialogResult.OK)
            {
                Cursor.Current = Cursors.WaitCursor;
                SQLiteConnection dbConn = new SQLiteConnection(Globals.connectionstring);
                dbConn.Open();
                Dataloader loader = new Dataloader(dbConn);
                loader.loadfileintocombinedemissions(openFileDialogBaseline.FileName);
                dbConn.Close();
                tabControl_Main.SelectedIndex = 2;
                tabControlCombinedEmissions.SelectedIndex = 0;
                completedataload(-1);
                Cursor.Current = Cursors.Default;
            }
        }
        private void LoadStatesDropdown()
        {
            comboBox_States.Items.Clear();
            comboBox_States.Items.Add("All contiguous U.S. States");
            comboBox_States.SelectedIndex = 0; // This will set the default item as the first one

            using (var qry = new QueryHelper())
            {
                DataTable datal2 = qry.getDataTable("select distinct STFIPS, STNAME from SYS_Dict order by STNAME");
                foreach (DataRow rowl2 in datal2.Rows)
                {
                    string stateName = rowl2.Field<string>("STNAME");
                    comboBox_States.Items.Add(stateName);
                }
            }
        }


        private void comboBox_States_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox_States.SelectedItem.ToString() == "All contiguous U.S. States")
            {
                comboBox_Counties.Enabled = false;
                comboBox_Counties.Items.Clear();
                comboBox_Counties.Items.Add("All counties");
                comboBox_States.SelectedIndex = 0;
            }
            else
            {
                LoadCountiesDropdown(comboBox_States.SelectedItem.ToString());
                comboBox_Counties.Enabled = true;
            }
            calculateSummaryVals();
            SetResultsContent();

        }

        private void comboBox_Counties_SelectedIndexChanged(object sender, EventArgs e)
        {
            // This code will be executed whenever the selected index (selected item) of comboBox_Counties changes.
            calculateSummaryVals();
            SetResultsContent();



        }

        private void LoadCountiesDropdown(string selectedState)
        {
            comboBox_Counties.Items.Clear();

            // Add the "All Counties" item
            comboBox_Counties.Items.Add("All Counties");
            comboBox_Counties.SelectedIndex = 0; // Set "All Counties" as the default selection

            using (var qry = new QueryHelper())
            {
                DataTable datal3 = qry.getDataTable($"select distinct CNTYFIPS , CYNAME from SYS_Dict where STNAME = '{selectedState}' order by CYNAME");
                foreach (DataRow rowl3 in datal3.Rows)
                {
                    string countyName = rowl3.Field<string>("CYNAME");
                    comboBox_Counties.Items.Add(countyName);
                }
            }
        }

        private string round2sigfig(string res)
        {
            double val = double.Parse(res);

            if (val == 0.0)
                return "0";

            int logValue = (int)Math.Floor(Math.Log10(Math.Abs(val))) + 1;
            double scale = Math.Pow(10, logValue);
            val = Math.Round(val / scale, 2) * scale;

            // For numbers >= 1, count the number of digits before the decimal point
            int digitsBeforeDecimal = val >= 1 ? (int)Math.Floor(Math.Log10(Math.Abs(val))) + 1 : 0;

            // Calculate the number of decimal places to display
            int decimalPlaces = 2 - digitsBeforeDecimal;
            decimalPlaces = Math.Max(0, decimalPlaces); // Ensure non-negative

            string stringVal = val.ToString("F" + decimalPlaces);


            try
            {
                // Attempt to convert the string to a long to support larger numbers
                long number = Convert.ToInt64(stringVal);
                // Format the number with commas and no decimal places
                return number.ToString("N0");
            }
            catch (FormatException)
            {
                Console.WriteLine("FORMAT EXCEPTION FOR stringVAL:");
                Console.WriteLine(stringVal);
                return stringVal;
            }
            catch (OverflowException)
            {
                Console.WriteLine("OVERFLOW EXCEPTION FOR stringVAL:");
                Console.WriteLine(stringVal);
                return stringVal;
            }


        }
        /*{
            double val = double.Parse(res);

            if (val == 0.0)
                return "0";

            int logValue = (int)Math.Floor(Math.Log10(Math.Abs(val)));
            double scale = Math.Pow(10, logValue - 1);

            val = Math.Round(val / scale, 2) * scale;

            if (Math.Abs(val) >= 1 && val == Math.Round(val))
                return val.ToString("N0"); // For rounded whole numbers

            // Determine the number of decimal places required to display 2 significant figures
            int decimalPlaces = logValue >= 1 ? 2 - logValue : 2;

            return val.ToString("N" + Math.Max(0, decimalPlaces));
        }*/



        private void calculateSummaryVals()
        {
            //make sure to reset dict vals to get new sums whenever this func is called with a different filter option:
            foreach (string key in SummaryTableVals.Keys)
            {
                SummaryTableVals[key]["high"] = "0";
                SummaryTableVals[key]["low"] = "0";
                SummaryTableVals[key]["highval"] = "0";
                SummaryTableVals[key]["lowval"] = "0";

            }

            foreach (SYS_ResultsRow result in this.cobraDataSet.SYS_Results.Rows)
            {
                string rowState = result.STATE;
                string rowCounty = result.COUNTY;

                string selectedState = comboBox_States.SelectedItem.ToString();

                //skip rows if filter is applied for specific state or county
                if (!(String.Equals(selectedState, "All contiguous U.S. States")))
                {
                    if (!(String.Equals(result.STATE.ToUpper(), selectedState.ToUpper()))) continue;
                }

                if (comboBox_Counties.Enabled && !(String.Equals(comboBox_Counties.SelectedItem.ToString(), "All Counties")))
                {
                    string selectedCounty = comboBox_Counties.SelectedItem.ToString();
                    if (!(String.Equals(result.COUNTY.ToUpper(), selectedCounty.ToUpper()))) continue;
                }

                //add mortality since it has different low/high vals
                double curLow = double.Parse(SummaryTableVals["Total PM Health Effects"]["low"]);
                curLow += result._PM_Mortality_All_Cause__low_;
                SummaryTableVals["Total PM Health Effects"]["low"] = curLow.ToString();
                double curHigh = double.Parse(SummaryTableVals["Total PM Health Effects"]["high"]);
                curHigh += result._PM_Mortality_All_Cause__high_;
                SummaryTableVals["Total PM Health Effects"]["high"] = curHigh.ToString();
                // $
                double curLowVal = double.Parse(SummaryTableVals["Total PM Health Effects"]["lowval"]);
                curLowVal += result.___PM_Mortality_All_Cause__low_;
                SummaryTableVals["Total PM Health Effects"]["lowval"] = curLowVal.ToString();
                double curHighVal = double.Parse(SummaryTableVals["Total PM Health Effects"]["highval"]);
                curHighVal += result.___PM_Mortality_All_Cause__high_;
                SummaryTableVals["Total PM Health Effects"]["highval"] = curHighVal.ToString();

                foreach (string colval in outputCols)
                {
                    //get val for the current row/column
                    var propertyInfo = result.GetType().GetProperty(colval);
                    object propertyValue = propertyInfo.GetValue(result);
                    string summaryKey;

                    //calculate total PM and total O3

                    if (colval.Contains("__PM_"))
                    {
                        if (!(colval.Contains("low") || colval.Contains("high")))
                        {
                            double curValLow = double.Parse(SummaryTableVals["Total PM Health Effects"]["lowval"]);
                            curValLow += (double)propertyValue;
                            double curValHigh = double.Parse(SummaryTableVals["Total PM Health Effects"]["highval"]);
                            curValHigh += (double)propertyValue;
                            SummaryTableVals["Total PM Health Effects"]["lowval"] = curValLow.ToString();
                            SummaryTableVals["Total PM Health Effects"]["highval"] = curValHigh.ToString();
                        }

                    }
                    else if (colval.Contains("_O3_"))
                    {
                        double curVal = double.Parse(SummaryTableVals["Total O3 Health Effects"]["lowval"]);
                        curVal += (double)propertyValue;
                        SummaryTableVals["Total O3 Health Effects"]["lowval"] = curVal.ToString();
                        SummaryTableVals["Total O3 Health Effects"]["highval"] = curVal.ToString();
                    }

                    if (colPropertyDict.TryGetValue(colval, out summaryKey))
                    {

                        if (SummaryTableVals[summaryKey]["lowproperty"] == colval)
                        {
                            double curVal = double.Parse(SummaryTableVals[summaryKey]["low"]);
                            curVal += (double)propertyValue;
                            SummaryTableVals[summaryKey]["low"] = curVal.ToString();
                        }
                        if (SummaryTableVals[summaryKey]["highproperty"] == colval)
                        {
                            double curVal = double.Parse(SummaryTableVals[summaryKey]["high"]);
                            curVal += (double)propertyValue;
                            SummaryTableVals[summaryKey]["high"] = curVal.ToString();
                        }
                        if (SummaryTableVals[summaryKey]["highvalproperty"] == colval)
                        {
                            double curVal = double.Parse(SummaryTableVals[summaryKey]["highval"]);
                            curVal += (double)propertyValue;
                            SummaryTableVals[summaryKey]["highval"] = curVal.ToString();
                        }
                        if (SummaryTableVals[summaryKey]["lowvalproperty"] == colval)
                        {
                            double curVal = double.Parse(SummaryTableVals[summaryKey]["lowval"]);
                            curVal += (double)propertyValue;
                            SummaryTableVals[summaryKey]["lowval"] = curVal.ToString();
                        }

                    }
                    else
                    {
                        //Console.WriteLine($"Key {colval} not found for summary table results.");
                    }



                }
            }
        }
        private void SetResultsContent()
        {
            string selection = comboBox_States.SelectedItem.ToString();

            if (comboBox_Counties.Enabled && comboBox_Counties.SelectedItem.ToString() != "All Counties")
            {
                string county = comboBox_Counties.SelectedItem.ToString();
                selection = $"{county}, {selection}";
            }

            string content = @"{\rtf1\ansi
{\colortbl;\red4\green131\blue134;\red0\green0\blue0;\red128\green0\blue128;} 
{\fonttbl{\f0\fnil\fcharset0 Arial;}}" +
$@"\fs44 \b \cf1 \ql Result Summary for: {selection}\b0 \fs28 \cf0 \par
 \fs26 \b \cf0 \tab\tab\tab\tab\tab\tab\tab\tab\tab Change in Incidence\tab\tab\tab\tab  Monetary Value\tab \cf1 \b0 \fs26 \cf0 \line
 \fs26 \b \cf0 \ul Health Endpoint\tab\tab\tab\tab Pollutant \tab\tab (cases, annual)\tab\tab\tab\tab\tab  (dollars, annual)\tab\tab\tab\tab\tab \cf1 \b0 \fs26 \cf0 \ulnone \line";
            content += @"\trowd \cellx4200 \cellx6400 \cellx8400 \cellx11400 \cellx13650 \cellx16600" +
                 @"\intbl \cell \brdrt\brdrnone \brdrl\brdrnone \brdrr\brdrnone \brdrb\brdrs" +
                    @"\intbl \cell \brdrt\brdrnone \brdrl\brdrnone \brdrr\brdrnone \brdrb\brdrs" +
 @"\intbl \b Low\b0 \cell \brdrt\brdrnone \brdrl\brdrnone \brdrr\brdrnone \brdrb\brdrs" +
 @"\intbl \b High\b0 \cell \brdrt\brdrnone \brdrl\brdrnone \brdrr\brdrnone \brdrb\brdrs" +
 @"\intbl \b Low\b0 \cell \brdrt\brdrnone \brdrl\brdrnone \brdrr\brdrnone \brdrb\brdrs" +
 @"\intbl \b High\b0 \cell \brdrt\brdrnone \brdrl\brdrnone \brdrr\brdrnone \brdrb\brdrs" +
 @"\row";
            // Loop through the keys of the dictionary
            foreach (string key in SummaryTableVals.Keys)
            {
                string pollutant = SummaryTableVals[key]["pollutant"];
                string low = round2sigfig(SummaryTableVals[key]["low"]);
                string high = round2sigfig(SummaryTableVals[key]["high"]);
                string lowval = round2sigfig(SummaryTableVals[key]["lowval"]);
                string highval = round2sigfig(SummaryTableVals[key]["highval"]);
                if (String.Equals(key, "Total Health Effects") || String.Equals(key, "Total PM Health Effects") || String.Equals(key, "Total O3 Health Effects"))
                {
                    low = "";
                    high = "";
                    pollutant = "";
                    content += @"\fs28 \b \cf3";
                    content += $@"\trowd \cellx4200 \cellx6400 \cellx8400 \cellx11400 \cellx13650 \cellx16600" +
                    $@"\intbl  {key}\cell \brdrt\brdrnone \brdrl\brdrnone \brdrr\brdrnone \brdrb\brdrs" +
                     $@"\intbl  {pollutant}\cell \brdrt\brdrnone \brdrl\brdrnone \brdrr\brdrnone \brdrb\brdrs" +
 $@"\intbl  {low}\cell \brdrt\brdrnone \brdrl\brdrnone \brdrr\brdrnone \brdrb\brdrs" +
 $@"\intbl  {high} \cell \brdrt\brdrnone \brdrl\brdrnone \brdrr\brdrnone \brdrb\brdrs" +
 $@"\intbl  ${lowval} \cell \brdrt\brdrnone \brdrl\brdrnone \brdrr\brdrnone \brdrb\brdrs" +
 $@"\intbl  ${highval} \cell \brdrt\brdrnone \brdrl\brdrnone \brdrr\brdrnone \brdrb\brdrs" +
 $@"\row";
                }
                else
                {
                    string spacer = "";
                    if (key.Contains("Mortality,") || key.Contains("from") || key == "Albuterol Use" || key == "Chest Tightness" || key == "Cough" || key == "Shortness of Breath" || key == "Wheeze")
                    {
                        content += @"\b0 \cf0";
                        spacer = "   ";
                    }
                    else
                    {
                        content += @"\b \cf3";
                    }

                    content += $@"\trowd \cellx4200 \cellx6400 \cellx8400 \cellx11400 \cellx13650 \cellx16600" +
                      $@"\intbl  {spacer + key}\cell \brdrt\brdrnone \brdrl\brdrnone \brdrr\brdrnone \brdrb\brdrs" +
                       $@"\intbl  {pollutant}\cell \brdrt\brdrnone \brdrl\brdrnone \brdrr\brdrnone \brdrb\brdrs" +
   $@"\intbl  {low}\cell \brdrt\brdrnone \brdrl\brdrnone \brdrr\brdrnone \brdrb\brdrs" +
   $@"\intbl  {high} \cell \brdrt\brdrnone \brdrl\brdrnone \brdrr\brdrnone \brdrb\brdrs" +
   $@"\intbl  ${lowval} \cell \brdrt\brdrnone \brdrl\brdrnone \brdrr\brdrnone \brdrb\brdrs" +
   $@"\intbl  ${highval} \cell \brdrt\brdrnone \brdrl\brdrnone \brdrr\brdrnone \brdrb\brdrs" +
   $@"\row";

                }



            } //end for loop
            content += @"}";


            richTextBox2.Rtf = content;
        }



        private void MainGUI_Load(object sender, EventArgs e)
        {
            LoadStatesDropdown();
            SetResultsContent();

            comboBox_AnalysisYearSelect.Items.Clear();
            comboBox_AnalysisYearSelect.DisplayMember = "Text";
            comboBox_AnalysisYearSelect.ValueMember = "Value";
            var items = new[] {
                new { Text = "2016", Value = 1 },
                new { Text = "2023", Value = 2 },
                new { Text = "2028", Value = 3 },
            };
            comboBox_AnalysisYearSelect.DataSource = items;
            comboBox_AnalysisYearSelect.SelectedIndex = 0;

            comboBox_EmissionsSelect.Items.Clear();
            comboBox_EmissionsSelect.DisplayMember = "Text";
            comboBox_EmissionsSelect.ValueMember = "Value";
            items = new[] {
                new { Text = "2016 COBRA emissions", Value = 1 },
                new { Text = "2023 COBRA emissions", Value = 2 },
                new { Text = "2028 COBRA emissions", Value = 3 },
            };
            comboBox_EmissionsSelect.DataSource = items;
            comboBox_EmissionsSelect.SelectedIndex = 0;

            comboBox_Incidence.Items.Clear();
            comboBox_Incidence.DisplayMember = "Text";
            comboBox_Incidence.ValueMember = "Value";
            items = new[] {
                new { Text = "COBRA 2016 incidence", Value = 1 },
                new { Text = "COBRA 2023 incidence", Value = 2 },
                new { Text = "COBRA 2028 incidence", Value = 3 },
            };
            comboBox_Incidence.DataSource = items;
            comboBox_Incidence.SelectedIndex = 0;

            comboBox_PopData.Items.Clear();
            comboBox_PopData.DisplayMember = "Text";
            comboBox_PopData.ValueMember = "Value";
            items = new[] {
                new { Text = "COBRA 2016 population", Value = 1 },
                new { Text = "COBRA 2023 population", Value = 2 },
                new { Text = "COBRA 2028 population", Value = 3 },
            };
            comboBox_PopData.DataSource = items;
            comboBox_PopData.SelectedIndex = 0;

            comboBox_ValueFunctions.Items.Clear();
            comboBox_ValueFunctions.DisplayMember = "Text";
            comboBox_ValueFunctions.ValueMember = "Value";
            items = new[] {
                new { Text = "COBRA 2016 valuation", Value = 1 },
                new { Text = "COBRA 2023 valuation", Value = 2 },
                new { Text = "COBRA 2028 valuation", Value = 3 },
            };
            comboBox_ValueFunctions.DataSource = items;
            comboBox_ValueFunctions.SelectedIndex = 0;

            comboBox_CRFunctions.Items.Clear();
            comboBox_CRFunctions.DisplayMember = "Text";
            comboBox_CRFunctions.ValueMember = "Value";
            items = new[] {
                new { Text = "COBRA Default Health Effect Functions", Value = 1 },
            };
            comboBox_CRFunctions.DataSource = items;
            comboBox_CRFunctions.SelectedIndex = 0;

            // TODO: This line of code loads data into the 'cobraDataSet.SYS_AnalysisYear' table. You can move, or remove it, as needed.
            //this.sYS_AnalysisYearTableAdapter.Fill(this.cobraDataSet.SYS_AnalysisYear);
            // TODO: This line of code loads data into the 'cobraDataSet.SYS_Valuation_INVENTORY_DESCRIPTION' table. You can move, or remove it, as needed.
            //this.sYS_Valuation_INVENTORY_DESCRIPTIONTableAdapter.Fill(this.cobraDataSet.SYS_Valuation_INVENTORY_DESCRIPTION);
            // TODO: This line of code loads data into the 'cobraDataSet.SYS_CR_INVENTORY_DESCRIPTION' table. You can move, or remove it, as needed.
            //this.sYS_CR_INVENTORY_DESCRIPTIONTableAdapter.Fill(this.cobraDataSet.SYS_CR_INVENTORY_DESCRIPTION);
            // TODO: This line of code loads data into the 'cobraDataSet.SYS_POP_INVENTORY_DESCRIPTION' table. You can move, or remove it, as needed.
            //this.sYS_POP_INVENTORY_DESCRIPTIONTableAdapter.Fill(this.cobraDataSet.SYS_POP_INVENTORY_DESCRIPTION);
            // TODO: This line of code loads data into the 'cobraDataSet.SYS_Incidence_Inventory_Description' table. You can move, or remove it, as needed.
            //this.SYS_Incidence_Inventory_DescriptionTableAdapter.Fill(this.cobraDataSet.SYS_Incidence_Inventory_Description);
            // TODO: This line of code loads data into the 'cobraDataSet.SYS_Emissions_Inventory_Description' table. You can move, or remove it, as needed.
            //this.sYS_Emissions_Inventory_DescriptionTableAdapter.Fill(this.cobraDataSet.SYS_Emissions_Inventory_Description);
            // TODO: This line of code loads data into the 'cobraDataSet.SYS_Results' table. You can move, or remove it, as needed.
            //this.sYS_ResultsTableAdapter.Fill(this.cobraDataSet.SYS_Results);
            // TODO: This line of code loads data into the 'cobraDataSet.SYS_Emissions' table. You can move, or remove it, as needed.
            //this.sYS_EmissionsTableAdapter.Fill(this.cobraDataSet.SYS_Emissions);

            this.syS_TiersTableAdapter1.Fill(cobraDataSet.SYS_Tiers);


            //fix up radview aggregates
            this.radGridView1.MasterTemplate.SummaryRowsTop[0].Clear();
            this.radGridView1.MasterTemplate.SummaryRowsBottom[0].Clear();

            var doAggregate = false;
            foreach (var column in radGridView1.MasterTemplate.Columns)
            {
                //System.Diagnostics.Debug.WriteLine("found: "+column.FieldName);
                if (doAggregate)
                {
                    Telerik.WinControls.UI.GridViewSummaryItem topsumfld = new Telerik.WinControls.UI.GridViewSummaryItem();
                    topsumfld.Aggregate = Telerik.WinControls.UI.GridAggregateFunction.Sum;
                    topsumfld.AggregateExpression = null;
                    topsumfld.FormatString = "Total: " + column.FormatString;
                    topsumfld.Name = column.FieldName;
                    this.radGridView1.MasterTemplate.SummaryRowsTop[0].Add(topsumfld);
                    Telerik.WinControls.UI.GridViewSummaryItem bottomsumfld = new Telerik.WinControls.UI.GridViewSummaryItem();
                    bottomsumfld.Aggregate = Telerik.WinControls.UI.GridAggregateFunction.Sum;
                    bottomsumfld.AggregateExpression = null;
                    bottomsumfld.FormatString = "Total: " + column.FormatString;
                    bottomsumfld.Name = column.FieldName;
                    this.radGridView1.MasterTemplate.SummaryRowsBottom[0].Add(bottomsumfld);
                }
                doAggregate = doAggregate || (column.FieldName == "COUNTY"); ;
            }
        }

        private void tabControlCombinedEmissions_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabControlCombinedEmissions.SelectedIndex == 1)
            {
                comboBox_emissionsfieldselect.Items.Clear();
                foreach (var column in radGridView2.Columns)
                {
                    if (column.Index > 15)
                    {
                        comboBox_emissionsfieldselect.Items.Add(column.HeaderText);
                    }
                }


                Cursor.Current = Cursors.WaitCursor;
                SQLiteConnection dbConn = new SQLiteConnection(Globals.connectionstring);
                dbConn.Open();
                AQProcessor processor = new AQProcessor(dbConn, "");
                processor.summarize2map();
                dbConn.Close();
                //adjust extent
                map1.ViewExtents = new Extent(-142.97545329993662, 15.460321970688369, -58.562403149064146, 62.009339263360459);
                comboBox_emissionsfieldselect.SelectedIndex = -1;

                Cursor.Current = Cursors.Default;
            }

            //better reset the values displayed    
            treeView_Tiers.SelectedNode = null;

            val_pm.Value = 0;
            val_so2.Value = 0;
            val_nh3.Value = 0;
            val_nox.Value = 0;
            val_voc.Value = 0;
            lbl_pol_pm_state.Text = "";
            lbl_pol_so2_state.Text = "";
            lbl_pol_nh3_state.Text = "";
            lbl_pol_nox_state.Text = "";
            lbl_pol_voc_state.Text = "";


        }

        private void comboBox_emissionsfieldselect_SelectedIndexChanged(object sender, EventArgs e)
        {
            //populate from sys_emissionsummarized
            DataTable dt = null;
            if (map1.Layers.Count > 0)
            {
                MapPolygonLayer countyLayer = default(MapPolygonLayer);

                countyLayer = (MapPolygonLayer)map1.Layers[0];

                if (countyLayer == null)
                {
                    MessageBox.Show("The layer is not a polygon layer.");
                }
                else
                {

                    SYS_SummarizedEmissionsDataTable lookup = new SYS_SummarizedEmissionsDataTable();
                    var da = new cobraDataSetTableAdapters.SYS_SummarizedEmissionsTableAdapter();
                    da.Fill(lookup);


                    //Get the shapefile's attribute table to our datatable dt
                    dt = countyLayer.DataSet.DataTable;
                    for (int row = 0; row < dt.Rows.Count; row++)
                    {
                        //object val = dt.Rows[row]["NAME"];
                        string fips = dt.Rows[row]["GEOID"].ToString();

                        DataRow[] foundRows;
                        foundRows = lookup.Select("FIPS='" + fips + "'");

                        if (foundRows.Length > 0)
                        {
                            if (comboBox_emissionsfieldselect.Text == "")
                            {
                                dt.Rows[row]["Value"] = 0;
                            }
                            else
                            {
                                dt.Rows[row]["Value"] = foundRows[0][emissionsfieldnamefromfriendlyname(comboBox_emissionsfieldselect.Text)];
                            }
                        }

                    }
                    //Create a new PolygonScheme
                    PolygonScheme scheme = new PolygonScheme();

                    //Set the ClassificationType for the PolygonScheme via EditorSettings
                    System.Drawing.Color startcol = System.Drawing.ColorTranslator.FromHtml("#E6C657");
                    System.Drawing.Color endcolor = System.Drawing.ColorTranslator.FromHtml("#DC6D01");

                    scheme.EditorSettings.StartColor = startcol;
                    scheme.EditorSettings.EndColor = endcolor;
                    scheme.EditorSettings.ClassificationType = ClassificationType.Quantities;
                    scheme.EditorSettings.NumBreaks = 5;
                    scheme.EditorSettings.IntervalMethod = IntervalMethod.EqualInterval;
                    scheme.EditorSettings.UseGradient = false;

                    //Here STATE_NAME would be the Unique value field
                    scheme.EditorSettings.FieldName = "Value";

                    //create categories on the scheme based on the attributes table and field name
                    scheme.CreateCategories(countyLayer.DataSet.DataTable);

                    //Set the scheme to countyLayer's symbology
                    countyLayer.Symbology = scheme;

                    countyLayer.LegendText = "US Counties - " + comboBox_emissionsfieldselect.Text;

                }
            }
        }


        private void btnExecute_Click(object sender, EventArgs e)
        {
            Cursor.Current = Cursors.WaitCursor;


            label_Computing.Visible = true;
            Application.DoEvents();
            SQLiteConnection dbConn = new SQLiteConnection(Globals.connectionstring);
            dbConn.Open();
            AQProcessor processor = new AQProcessor(dbConn, Globals.connectionstring_EF);

            processor.setup_from_GUI();
            processor.summarize("SYS_Emissions_Base", "SYS_Emissions_Base_Summarized", "SYS_Destination_Base");
            processor.summarize("SYS_Emissions_Control", "SYS_Emissions_Control_Summarized", "SYS_Destination_Control");
            processor.finalize("SYS_Destination_Base", "SYS_Destination_Control");

            Console.WriteLine("Computing impacts at: " + System.DateTime.Now.ToString());
            ImpactProcessor impactprocessor = new ImpactProcessor(dbConn, Globals.connectionstring_EF);

            FileOrTable popdata = new FileOrTable
            {
                usetable = radioButton_PreloadPopulance.Checked,
                filename = popfile,
                dataidx = (int)comboBox_PopData.SelectedValue
            };
            FileOrTable incidencedata = new FileOrTable
            {
                usetable = radioButton_PreloadIncidence.Checked,
                filename = incidencefile,
                dataidx = (int)comboBox_Incidence.SelectedValue
            };
            FileOrTable crdata = new FileOrTable
            {
                usetable = radioButton_PreloadCR.Checked,
                filename = crfile,
                dataidx = (int)comboBox_CRFunctions.SelectedValue
            };
            FileOrTable valdata = new FileOrTable
            {
                usetable = radioButton_PreloadValue.Checked,
                filename = valuationfile,
                dataidx = (int)comboBox_ValueFunctions.SelectedValue
            };
            if (radioButtonDR3.Checked)
            {

                Console.WriteLine("COMPUTING IMPACTS WITH A DISCOUNT RATE OF 2%");
                impactprocessor.ComputeImpacts(true, Decimal.ToDouble(2), popdata, incidencedata, crdata, valdata);


            }
            else
            {
                Console.WriteLine("COMPUTING IMPACTS IWTH A DISCOUNT RATE OF:");
                Console.WriteLine(Decimal.ToDouble(discountrateUpDown.Value));
                //grab the custom value from the discountupdown value
                impactprocessor.ComputeImpacts(true, Decimal.ToDouble(discountrateUpDown.Value), popdata, incidencedata, crdata, valdata);

            }





            dbConn.Close();

            tabControl_Main.SelectedIndex = 4;
            tabControlResults.SelectedIndex = 0;
            sYS_ResultsTableAdapter.Fill(cobraDataSet.SYS_Results);

            Console.WriteLine("CREATED TABLE ADAPTER");

            //impacts have been computed so now populate data:
            Console.WriteLine("SHOULD BE CALCULATING SUMMARY VALS!");
            calculateSummaryVals();
            //make sure you trigger reload of summary output table based off of current SummaryTableVals dict
            SetResultsContent();

            SystemSounds.Exclamation.Play();

            runexecuted = true;
            setDirty(false);

            label_Computing.Visible = false;

            try
            {
                this.radGridView1.Rows[0].IsSelected = true;
                this.radGridView1.CurrentRow = this.radGridView1.Rows[0];
                this.radGridView1.TableElement.VScrollBar.Value = 0;
            }
            catch { };

            Cursor.Current = Cursors.Default;
        }

        private void tabControlResults_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabControlResults.SelectedIndex == 1)
            {
                comboBox3.Items.Clear();
                foreach (var column in radGridView1.MasterTemplate.Columns)
                {
                    if (column.Index > 4)
                    {
                        comboBox3.Items.Add(column.HeaderText);
                    }
                }

                Cursor.Current = Cursors.WaitCursor;
                //adjust extent
                map2.ViewExtents = new Extent(-142.97545329993662, 15.460321970688369, -58.562403149064146, 62.009339263360459);
                comboBox3.SelectedIndex = 0;
                Cursor.Current = Cursors.Default;
            }

        }

        private void comboBox3_SelectedIndexChanged(object sender, EventArgs e)
        {
            DataTable dt = null;
            if (map2.Layers.Count > 0)
            {
                MapPolygonLayer countyLayer = default(MapPolygonLayer);

                countyLayer = (MapPolygonLayer)map2.Layers[0];

                if (countyLayer == null)
                {
                    MessageBox.Show("The layer is not a polygon layer.");
                }
                else
                {

                    SYS_ResultsDataTable lookup = new SYS_ResultsDataTable();
                    var da = new cobraDataSetTableAdapters.SYS_ResultsTableAdapter();
                    da.Fill(lookup);

                    lookup.Columns["TOTALHELOW"].Expression = "[$ PM Acute Myocardial Infarction Nonfatal] + [$ PM Resp Hosp Adm] + [$ PM Minor Restricted Activity Days] + [$ PM Mortality All Cause (low)] + [$ PM Infant Mortality] +  [$ PM Work Loss Days] + [$ PM Asthma Symptoms Albuterol use] + [$ PM ER visits All Cardiac Outcomes] + [$ PM ER visits respiratory] + [$ PM HA Cardio Cerebro and Peripheral Vascular Disease] + [$ PM HA Alzheimers Disease] + [$ PM HA Parkinsons Disease] + [$ PM HA Respiratory2] + [$ PM Incidence Stroke] + [$ PM Incidence Out of Hospital Cardiac Arrest] + [$ PM Incidence Lung Cancer] + [$ PM Incidence Hay Fever Rhinitis] + [$ PM Incidence Asthma] + [$ O3 ER visits respiratory] + [$ O3 HA All Respiratory] + [$ O3 Incidence Hay Fever Rhinitis] + [$ O3 Incidence Asthma] + [$ O3 Asthma Symptoms Chest Tightness] + [$ O3 Asthma Symptoms Cough] + [$ O3 Asthma Symptoms Shortness of Breath] + [$ O3 Asthma Symptoms Wheeze] + [$ O3 ER Visits Asthma] + [$ O3 School Loss Days] + [$ O3 Mortality Longterm exposure] + [$ O3 Mortality Shortterm exposure]";
                    lookup.Columns["TOTALHEHIGH"].Expression = "[$ PM Acute Myocardial Infarction Nonfatal] + [$ PM Resp Hosp Adm] + [$ PM Minor Restricted Activity Days] + [$ PM Mortality All Cause (high)] + [$ PM Infant Mortality] + [$ PM Work Loss Days] + [$ PM Asthma Symptoms Albuterol use] + [$ PM ER visits All Cardiac Outcomes] + [$ PM ER visits respiratory] + [$ PM HA Cardio Cerebro and Peripheral Vascular Disease] + [$ PM HA Alzheimers Disease] + [$ PM HA Parkinsons Disease] + [$ PM HA Respiratory2] + [$ PM Incidence Stroke] + [$ PM Incidence Out of Hospital Cardiac Arrest] + [$ PM Incidence Lung Cancer] + [$ PM Incidence Hay Fever Rhinitis] + [$ PM Incidence Asthma] + [$ O3 ER visits respiratory] + [$ O3 HA All Respiratory] + [$ O3 Incidence Hay Fever Rhinitis] + [$ O3 Incidence Asthma] + [$ O3 Asthma Symptoms Chest Tightness] + [$ O3 Asthma Symptoms Cough] + [$ O3 Asthma Symptoms Shortness of Breath] + [$ O3 Asthma Symptoms Wheeze] + [$ O3 ER Visits Asthma] + [$ O3 School Loss Days] + [$ O3 Mortality Longterm exposure] + [$ O3 Mortality Shortterm exposure]";
                    lookup.Columns["ALLMORTLOW"].Expression = "[$ PM Mortality All Cause (low)] + [$ PM Infant Mortality] + [$ O3 Mortality Longterm exposure] + [$ O3 Mortality Shortterm exposure]";
                    lookup.Columns["ALLMORTHIGH"].Expression = "[$ PM Mortality All Cause (high)] + [$ PM Infant Mortality] + [$ O3 Mortality Longterm exposure] + [$ O3 Mortality Shortterm exposure]";
                    lookup.Columns["ALLMORTLOWVAL"].Expression = "[PM Mortality All Cause (low)] + [PM Infant Mortality] + [O3 Mortality Longterm exposure] + [O3 Mortality Shortterm exposure]";
                    lookup.Columns["ALLMORTHIGHVAL"].Expression = "[PM Mortality All Cause (high)] + [PM Infant Mortality] + [O3 Mortality Longterm exposure] + [O3 Mortality Shortterm exposure]";
                    /*lookup.Columns["ALLPMORTLOW"].Expression = "[PM Mortality All Cause (low)] + [PM Infant Mortality] + [O3 Mortality Longterm exposure] + [O3 Mortality Shortterm exposure]";
                    lookup.Columns["ALLPMORTLOWVAL"].Expression = "[PM Mortality All Cause (low)] + [PM Infant Mortality] + [O3 Mortality Longterm exposure] + [O3 Mortality Shortterm exposure]";

                    lookup.Columns["ALLPMORTHIGH"].Expression = "[PM Mortality All Cause (high)] + [PM Infant Mortality]";
                    lookup.Columns["ALLPMORTHIGHVAL"].Expression = "[$ PM Mortality All Cause (high)] + [$ PM Infant Mortality]";
                    lookup.Columns["ALLPMORTLOW"].Expression = "[PM Mortality All Cause (low)] + [PM Infant Mortality]";
                    lookup.Columns["ALLPMORTLOWVAL"].Expression = "[$ PM Mortality All Cause(low)] + [$ PM Infant Mortality]";*/
                    lookup.Columns["TOTALO3MORT"].Expression = "[O3 Mortality Longterm exposure] + [O3 Mortality Shortterm exposure]";
                    lookup.Columns["TOTALO3MORTVAL"].Expression = "[$ O3 Mortality Longterm exposure] + [$ O3 Mortality Shortterm exposure]";
                    lookup.Columns["ALLASTHMA"].Expression = "[PM Asthma Symptoms Albuterol use] + [O3 Asthma Symptoms Chest Tightness] + [O3 Asthma Symptoms Cough] + [O3 Asthma Symptoms Wheeze] + [O3 Asthma Symptoms Shortness of Breath]";
                    lookup.Columns["ALLASTHMAVAL"].Expression = "[$ PM Asthma Symptoms Albuterol use] + [$ O3 Asthma Symptoms Chest Tightness] + [$ O3 Asthma Symptoms Cough] + [$ O3 Asthma Symptoms Wheeze] + [$ O3 Asthma Symptoms Shortness of Breath]";
                    lookup.Columns["TOTALHAYFEVER"].Expression = "[PM Incidence Hay Fever Rhinitis] + [O3 Incidence Hay Fever Rhinitis]";
                    lookup.Columns["TOTALHAYFEVERVAL"].Expression = "[$ PM Incidence Hay Fever Rhinitis] + [$ O3 Incidence Hay Fever Rhinitis]";
                    lookup.Columns["TOTALINCIDENCEASTHMA"].Expression = "[PM Incidence Asthma] + [O3 Incidence Asthma]";
                    lookup.Columns["TOTALINCIDENCEASTHMAVAL"].Expression = "[$ PM Incidence Asthma] + [$ O3 Incidence Asthma]";
                    lookup.Columns["TOTALERRESP"].Expression = "[PM ER visits respiratory] + [O3 ER visits respiratory]";
                    lookup.Columns["TOTALERRESPVAL"].Expression = "[$ PM ER visits respiratory] + [$ O3 ER visits respiratory]";

                    lookup.Columns["HAALLRESP"].Expression = "[PM HA All Respiratory] + [PM HA Respiratory2] + [O3 HA All Respiratory]";
                    lookup.Columns["HAALLRESPVAL"].Expression = "[$ PM Resp Hosp Adm] + [$ PM HA Respiratory2] + [$ O3 HA All Respiratory]";
                    lookup.Columns["PMHAALLRESP"].Expression = "[PM HA All Respiratory] + [PM HA Respiratory2]";
                    lookup.Columns["PMHAALLRESPVAL"].Expression = "[$ PM Resp Hosp Adm] + [$ PM HA Respiratory2]";






                    //Get the shapefile's attribute table to our datatable dt
                    dt = countyLayer.DataSet.DataTable;
                    for (int row = 0; row < dt.Rows.Count; row++)
                    {
                        string fips = dt.Rows[row]["GEOID"].ToString();

                        DataRow[] foundRows;
                        foundRows = lookup.Select("FIPS='" + fips + "'");
                        dt.Rows[row]["Value"] = 0;

                        if (foundRows.Length > 0)
                        {
                            if (comboBox3.Text == "")
                            {
                                dt.Rows[row]["Value"] = 0;
                            }
                            else
                            {
                                string field2name = fieldnamefromfriendlyname(comboBox3.Text).Replace(".", "_");
                                double value2set = 0;
                                switch (field2name)
                                {
                                    case "PMHAALLRESP":
                                        value2set = (double)foundRows[0]["PM HA All Respiratory"];
                                        value2set = value2set + (double)foundRows[0]["PM HA Respiratory2"];
                                        dt.Rows[row]["Value"] = value2set;
                                        break;
                                    case "ALLASTHMA":
                                        value2set = (double)foundRows[0]["Asthma Exacerbation Cough"];
                                        value2set = value2set + (double)foundRows[0]["Asthma Exacerbation Shortness of Breath"];
                                        value2set = value2set + (double)foundRows[0]["Asthma Exacerbation Wheeze"];
                                        value2set = value2set + (double)foundRows[0]["Asthma Exacerbation Chest Tightness"];
                                        dt.Rows[row]["Value"] = value2set;
                                        break;
                                    default:
                                        dt.Rows[row]["Value"] = foundRows[0][field2name];
                                        break;
                                }
                            }
                        }

                    }
                    //Create a new PolygonScheme
                    PolygonScheme scheme = new PolygonScheme();

                    //Set the ClassificationType for the PolygonScheme via EditorSettings
                    scheme.EditorSettings.StartColor = System.Drawing.Color.LightBlue;
                    scheme.EditorSettings.EndColor = System.Drawing.Color.DarkBlue;
                    scheme.EditorSettings.ClassificationType = ClassificationType.Quantities;
                    scheme.EditorSettings.NumBreaks = 5;
                    scheme.EditorSettings.IntervalMethod = IntervalMethod.EqualFrequency;
                    scheme.EditorSettings.UseGradient = false;

                    //Here STATE_NAME would be the Unique value field
                    scheme.EditorSettings.FieldName = "Value";

                    //create categories on the scheme based on the attributes table and field name
                    scheme.CreateCategories(countyLayer.DataSet.DataTable);

                    //Set the scheme to countyLayer's symbology
                    countyLayer.Symbology = scheme;

                    countyLayer.LegendText = "US Counties - " + comboBox3.Text;
                }
            }
        }

        private void advancedDataGridView1_FilterStringChanged(object sender, EventArgs e)
        {
            //sYSEmissionsBindingSource.Filter = advancedDataGridView1.FilterString;
        }

        private void advancedDataGridView1_SortStringChanged(object sender, EventArgs e)
        {
            //sYSEmissionsBindingSource.Sort = advancedDataGridView1.SortString;
        }

        private void MainGUI_Shown(object sender, EventArgs e)
        {
            //advancedDataGridView1.DisableFilterAndSort(bASENOxDataGridViewTextBoxColumn);
            //advancedDataGridView1.DisableFilterAndSort(bASENH3DataGridViewTextBoxColumn);
            //advancedDataGridView1.DisableFilterAndSort(bASEPM25DataGridViewTextBoxColumn);
            //advancedDataGridView1.DisableFilterAndSort(bASESO2DataGridViewTextBoxColumn);
            //advancedDataGridView1.DisableFilterAndSort(bASEVOCDataGridViewTextBoxColumn);

            //advancedDataGridView1.LoadFilterAndSort("(Modified=True)", "");
        }

        private void advancedDataGridView2_FilterStringChanged(object sender, EventArgs e)
        {
            //sYSResultsBindingSource.Filter = advancedDataGridView2.FilterString;
        }

        private void advancedDataGridView2_SortStringChanged(object sender, EventArgs e)
        {
            //sYSResultsBindingSource.Sort = advancedDataGridView2.SortString;
        }

        private void buttonEmissionsUpdate_Click(object sender, EventArgs e)
        {
            sYS_EmissionsTableAdapter.Update(cobraDataSet.SYS_Emissions);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (saveFileDialogScenario.ShowDialog() == DialogResult.OK)
            {
                Cursor.Current = Cursors.WaitCursor;

                StreamWriter sw = new StreamWriter(saveFileDialogScenario.FileName);  // You should include System.IO;

                List<int> selcols = new List<int>();

                List<string> line = new List<string>();
                foreach (DataColumn column in cobraDataSet.SYS_Emissions.Columns)
                {
                    if ((column.Ordinal > 0) && (column.Ordinal < 25))
                    {
                        line.Add("\"" + column.ColumnName.Replace("\"", "\"\"") + "\"");
                        selcols.Add(column.Ordinal);
                    }
                }
                sw.WriteLine(string.Join(",", line));

                foreach (DataRow row in cobraDataSet.SYS_Emissions.Rows)
                {
                    line.Clear();
                    foreach (var item in selcols)
                    {
                        line.Add("\"" + row[item].ToString().Replace("\"", "\"\"") + "\"");
                    }
                    sw.WriteLine(string.Join(",", line));
                }
                sw.Close();


                Cursor.Current = Cursors.Default;
            }

        }

        private void PopulateTrees()
        {
            treeView_Tiers.Nodes.Clear();

            TreeNode nodetier1 = null;
            TreeNode nodetier2 = null;
            TreeNode nodetier3 = null;

            using (var qry = new QueryHelper())
            {
                DataTable data = qry.getDataTable("select distinct TIER1, TIER1NAME from SYS_Tiers order by TIER1");
                foreach (DataRow row in data.Rows)
                {
                    Int64 val = row.Field<Int64>("TIER1");
                    nodetier1 = new TreeNode(row.Field<string>("TIER1NAME"));
                    nodetier1.Tag = new Nodedesc() { level = 1, tier1 = val };
                    treeView_Tiers.Nodes.Add(nodetier1);
                    //children
                    DataTable datal2 = qry.getDataTable("select distinct TIER2, TIER2NAME from SYS_Tiers where TIER1 =" + val.ToString() + " order by TIER1");
                    foreach (DataRow rowl2 in datal2.Rows)
                    {
                        Int64 val2 = rowl2.Field<Int64>("TIER2");
                        nodetier2 = new TreeNode(rowl2.Field<string>("TIER2NAME"));
                        nodetier2.Tag = new Nodedesc() { level = 2, tier1 = val, tier2 = val2 };
                        nodetier1.Nodes.Add(nodetier2);
                        //leaves
                        DataTable datal3 = qry.getDataTable("select distinct TIER3, TIER3NAME from SYS_Tiers where TIER1 =" + val.ToString() + " and TIER2=" + val2.ToString() + " order by TIER1, TIER2");
                        foreach (DataRow rowl3 in datal3.Rows)
                        {
                            Int64 val3 = rowl3.Field<Int64>("TIER3");
                            nodetier3 = new TreeNode(rowl3.Field<string>("TIER3NAME"));
                            nodetier3.Tag = new Nodedesc() { level = 3, tier1 = val, tier2 = val2, tier3 = val3 };
                            nodetier2.Nodes.Add(nodetier3);
                        }
                    }
                }
            }
            //now for the states
            treeView_Locations.Nodes.Clear();

            nodetier1 = null;
            nodetier2 = null;
            nodetier3 = null;

            string curstate;
            string curcounty;
            long sourceindx;
            long stateid;
            long countyid;
            string curcountyname;
            string curstatename;

            using (var qry = new QueryHelper())
            {
                nodetier1 = new TreeNode("US");
                nodetier1.Tag = new LocationDesc();
                treeView_Locations.Nodes.Add(nodetier1);
                //children
                DataTable datal2 = qry.getDataTable("select distinct STFIPS, STNAME from SYS_Dict order by STNAME");
                foreach (DataRow rowl2 in datal2.Rows)
                {
                    curstate = rowl2.Field<string>("STFIPS");
                    nodetier2 = new TreeNode(rowl2.Field<string>("STNAME"));
                    nodetier2.Tag = new LocationDesc() { statefips = curstate };
                    nodetier1.Nodes.Add(nodetier2);
                    //leaves
                    DataTable datal3 = qry.getDataTable("select distinct STFIPS, CNTYFIPS , CYNAME, SOURCEINDX  from SYS_Dict where STFIPS = '" + curstate + "' order by CYNAME");
                    foreach (DataRow rowl3 in datal3.Rows)
                    {
                        curcounty = rowl3.Field<string>("CNTYFIPS");
                        sourceindx = rowl3.Field<long>("SOURCEINDX");
                        stateid = long.Parse(curstate);
                        countyid = long.Parse(curcounty);
                        curcountyname = rowl3.Field<string>("CYNAME");
                        curstatename = rowl2.Field<string>("STNAME");
                        nodetier3 = new TreeNode(rowl3.Field<string>("CYNAME"));
                        nodetier3.Tag = new LocationDesc() { statefips = curstate, countyfips = curcounty, sourceidx = sourceindx, stid = stateid, cyid = countyid, statename = curstatename, countyname = curcountyname };
                        nodetier2.Nodes.Add(nodetier3);
                    }
                }
            }
            nodetier1.Expand();


        }



        private void btnBaseControl_Click(object sender, EventArgs e)
        {
            if (openFileDialogBaseline.ShowDialog() == DialogResult.OK)
            {
                Cursor.Current = Cursors.WaitCursor;
                SQLiteConnection dbConn = new SQLiteConnection(Globals.connectionstring);
                dbConn.Open();
                Dataloader loader = new Dataloader(dbConn);
                loader.loadscenariofileintocombinedemissions(openFileDialogBaseline.FileName);
                dbConn.Close();
                //tabControl_Main.SelectedIndex = 2;
                //tabControlCombinedEmissions.SelectedIndex = 0;
                completedataload(-1);
                Cursor.Current = Cursors.Default;
            }

        }

        private void CheckTreeViewNode(TreeNode node, Boolean isChecked)
        {
            foreach (TreeNode item in node.Nodes)
            {
                item.Checked = isChecked;

                if (item.Nodes.Count > 0)
                {
                    this.CheckTreeViewNode(item, isChecked);
                }
            }
        }

        private void CheckTreeViewNodeExp(TreeNode node, Boolean isChecked, Boolean poshpopexpanded)
        {
            foreach (TreeNode item in node.Nodes)
            {
                item.Checked = isChecked;
                Boolean isexpanded = item.IsExpanded;

                if (item.Nodes.Count > 0)
                {
                    this.CheckTreeViewNodeExp(item, isChecked, poshpopexpanded);
                }

                if (poshpopexpanded && isexpanded)
                {
                    item.Expand();
                }
                else { item.Collapse(true); };
            }
        }


        private void UncheckTreeViewNode(TreeNode node)
        {
            if (node.Parent != null)
            {
                node.Parent.Checked = false;
                UncheckTreeViewNode(node.Parent);
            }
        }

        private void treeView_Locations_AfterCheck(object sender, TreeViewEventArgs e)
        {
            if (!lockout)
            {
                lockout = true;
                Cursor.Current = Cursors.WaitCursor;
                if (e.Node.Checked || System.Windows.Forms.Control.ModifierKeys == Keys.Control)
                {
                    CheckTreeViewNode(e.Node, e.Node.Checked);
                }
                if (!e.Node.Checked)
                {
                    UncheckTreeViewNode(e.Node);
                    if (e.Node.Nodes.Count > 0)
                    {
                        e.Node.ExpandAll();
                    }
                }
                executetierfilter();
                lockout = false;
                Cursor.Current = Cursors.Default;
            }
        }

        private void treeView_Tiers_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (!lockout)
            {
                lockout = true;
                Cursor.Current = Cursors.WaitCursor;
                executetierfilter();
                lockout = false;
                Cursor.Current = Cursors.Default;
            }
        }


        private string labelformat(double base_, double control_)
        {
            double b = Math.Truncate(base_ * 100) / 100;
            double c = Math.Truncate(control_ * 100) / 100;
            double d = Math.Truncate((base_ - control_) * 100) / 100;

            string bs = string.Format("{0:N2}", b);
            string cs = string.Format("{0:N2}", c);
            string ds = string.Format("{0:N2}", d);

            return "" + cs + " [ tons ] / " + bs + " [ tons ]" + "";
        }


        private void executetierfilter()
        {
            _base.NOx = 0;
            _base.SO2 = 0;
            _base.NH3 = 0;
            _base.PM25 = 0;
            _base.VOC = 0;

            _control.NOx = 0;
            _control.SO2 = 0;
            _control.NH3 = 0;
            _control.PM25 = 0;
            _control.VOC = 0;

            List<string> fips = new List<string>();

            foreach (TreeNode node in treeView_Locations.GetAllNodes())
            {
                if (node.Level == 2 && node.Checked)
                {
                    LocationDesc info = (LocationDesc)node.Tag;
                    fips.Add("'" + info.statefips + info.countyfips + "'");
                }
            }

            if ((treeView_Tiers.SelectedNode != null) && (fips.Count > 0))
            {
                Nodedesc filterinfo = (Nodedesc)treeView_Tiers.SelectedNode.Tag;
                var strExpr = "";
                switch (filterinfo.level)
                {
                    case 1:
                        strExpr = "TIER1 = " + filterinfo.tier1.ToString();
                        break;
                    case 2:
                        strExpr = "TIER1 = " + filterinfo.tier1.ToString() + " and TIER2=" + filterinfo.tier2.ToString();
                        break;
                    default:
                        strExpr = "TIER1 = " + filterinfo.tier1.ToString() + " and TIER2=" + filterinfo.tier2.ToString() + " and TIER3=" + filterinfo.tier3.ToString();
                        break;
                }


                strExpr = strExpr + " and FIPS in (" + String.Join(",", fips.ToArray()) + ")";
                selectedemissions = cobraDataSet.SYS_Emissions.Select(strExpr);



                foreach (var row in selectedemissions)
                {
                    _base.NOx = _base.NOx + row.Field<double>("BASE_NOx");
                    _base.SO2 = _base.SO2 + row.Field<double>("BASE_SO2");
                    _base.NH3 = _base.NH3 + row.Field<double>("BASE_NH3");
                    _base.PM25 = _base.PM25 + row.Field<double>("BASE_PM25");
                    _base.VOC = _base.VOC + row.Field<double>("BASE_VOC");

                    _control.NOx = _control.NOx + row.Field<double>("CTRL_NOx");
                    _control.SO2 = _control.SO2 + row.Field<double>("CTRL_SO2");
                    _control.NH3 = _control.NH3 + row.Field<double>("CTRL_NH3");
                    _control.PM25 = _control.PM25 + row.Field<double>("CTRL_PM25");
                    _control.VOC = _control.VOC + row.Field<double>("CTRL_VOC");
                }

                //set up the labels
                lblEmissionsCounts.Visible = true;

                lbl_pol_pm_state.Visible = true;
                lbl_pol_so2_state.Visible = true;
                lbl_pol_nox_state.Visible = true;
                lbl_pol_nh3_state.Visible = true;
                lbl_pol_voc_state.Visible = true;

                lbl_pol_pm_state.Text = labelformat(_base.PM25, _control.PM25);
                lbl_pol_so2_state.Text = labelformat(_base.SO2, _control.SO2);
                lbl_pol_nox_state.Text = labelformat(_base.NOx, _control.NOx);
                lbl_pol_nh3_state.Text = labelformat(_base.NH3, _control.NH3);
                lbl_pol_voc_state.Text = labelformat(_base.VOC, _control.VOC);

                btn_change_apply.Enabled = true;
            }
            else
            {
                btn_change_apply.Enabled = false;
            }
        }

        private void treeView_Locations_AfterSelect(object sender, TreeViewEventArgs e)
        {
        }

        private void label9_Click(object sender, EventArgs e)
        {
        }

        private void fixupradviewaggregates()
        {
            //fix up radview aggregates
            this.radGridView2.MasterTemplate.SummaryRowsTop[0].Clear();
            this.radGridView2.MasterTemplate.SummaryRowsBottom[0].Clear();

            var doAggregate = false;
            foreach (var column in radGridView2.MasterTemplate.Columns)
            {
                if (doAggregate)
                {
                    Telerik.WinControls.UI.GridViewSummaryItem topsumfld = new Telerik.WinControls.UI.GridViewSummaryItem();
                    topsumfld.Aggregate = Telerik.WinControls.UI.GridAggregateFunction.Sum;
                    topsumfld.AggregateExpression = null;
                    topsumfld.FormatString = "Total: {0}";
                    topsumfld.Name = column.FieldName;
                    this.radGridView2.MasterTemplate.SummaryRowsTop[0].Add(topsumfld);
                    Telerik.WinControls.UI.GridViewSummaryItem bottomsumfld = new Telerik.WinControls.UI.GridViewSummaryItem();
                    bottomsumfld.Aggregate = Telerik.WinControls.UI.GridAggregateFunction.Sum;
                    bottomsumfld.AggregateExpression = null;
                    bottomsumfld.FormatString = "Total: {0}";
                    bottomsumfld.Name = column.FieldName;
                    this.radGridView2.MasterTemplate.SummaryRowsBottom[0].Add(bottomsumfld);
                }
                doAggregate = doAggregate || (column.FieldName == "BASE_NOx");
            }

            try
            {
                this.radGridView2.Rows[0].IsSelected = true;
                this.radGridView2.CurrentRow = this.radGridView2.Rows[0];
                this.radGridView2.TableElement.VScrollBar.Value = 0;
            }
            catch { };
        }


        private void btn_change_apply_Click(object sender, EventArgs e)
        {
            Cursor.Current = Cursors.WaitCursor;
            executetierfilter(); //do this again just to make sure
            if (can_apply())
            {
                do_apply();
                executetierfilter();
                setDirty(true);

                val_pm.Value = 0;
                val_so2.Value = 0;
                val_nh3.Value = 0;
                val_nox.Value = 0;
                val_voc.Value = 0;
            }
            else
            {
                MessageBox.Show("Please make sure you do not try to reduce emissions by more than actually present in the baseline.");
            }
            fixupradviewaggregates();
            Cursor.Current = Cursors.Default;
            SystemSounds.Exclamation.Play();

        }


        private bool can_apply()
        {
            bool result = true;
            //check in case of pct
            //pm 2.5
            if (rb_pm_pct.Checked && rb_pm_dec.Checked)
            {
                result = result && (val_pm.Value <= 100); //can't reduce by more than 100%
            }
            //so2
            if (rb_so2_pct.Checked && rb_so2_dec.Checked)
            {
                result = result && (val_so2.Value <= 100); //can't reduce by more than 100%
            }
            //nox
            if (rb_nox_pct.Checked && rb_nox_dec.Checked)
            {
                result = result && (val_nox.Value <= 100); //can't reduce by more than 100%
            }
            //nh3
            if (rb_nh3_pct.Checked && rb_nh3_dec.Checked)
            {
                result = result && (val_nh3.Value <= 100); //can't reduce by more than 100%
            }
            //voc
            if (rb_voc_pct.Checked && rb_voc_dec.Checked)
            {
                result = result && (val_voc.Value <= 100); //can't reduce by more than 100%
            }
            // check absolute decrease - limited by what is actually there
            //pm 2.5
            if (!rb_pm_pct.Checked && rb_pm_dec.Checked)
            {
                result = result && (val_pm.Value <= (decimal)_base.PM25); //can't reduce by more than is there
            }
            //so2
            if (!rb_so2_pct.Checked && rb_so2_dec.Checked)
            {
                result = result && (val_so2.Value <= (decimal)_base.SO2); //can't reduce by more than is there
            }
            //nox
            if (!rb_nox_pct.Checked && rb_nox_dec.Checked)
            {
                result = result && (val_nox.Value <= (decimal)_base.NOx); //can't reduce by more than is there
            }
            //nh3
            if (!rb_nh3_pct.Checked && rb_nh3_dec.Checked)
            {
                result = result && (val_nh3.Value <= (decimal)_base.NH3); //can't reduce by more than is there
            }
            //voc
            if (!rb_voc_pct.Checked && rb_voc_dec.Checked)
            {
                result = result && (val_voc.Value <= (decimal)_base.VOC); //can't reduce by more than is there
            }

            return result;
        }

        private void createmissingemissionsrecords()
        {

            //get where to apply
            List<LocationDesc> locations = new List<LocationDesc>();
            foreach (TreeNode node in treeView_Locations.GetAllNodes())
            {
                if (node.Level == 2 && node.Checked)
                {
                    LocationDesc info = (LocationDesc)node.Tag;
                    locations.Add(info);
                }
            }

            if ((treeView_Tiers.SelectedNode != null) && (locations.Count > 0))
            {
                Nodedesc filterinfo = (Nodedesc)treeView_Tiers.SelectedNode.Tag;
                var strExpr = "";
                switch (filterinfo.level)
                {
                    case 1:
                        strExpr = "TIER1 = " + filterinfo.tier1.ToString();
                        break;
                    case 2:
                        strExpr = "TIER1 = " + filterinfo.tier1.ToString() + " and TIER2=" + filterinfo.tier2.ToString();
                        break;
                    default:
                        strExpr = "TIER1 = " + filterinfo.tier1.ToString() + " and TIER2=" + filterinfo.tier2.ToString() + " and TIER3=" + filterinfo.tier3.ToString();
                        break;
                }
                var selectedtiers = cobraDataSet.SYS_Tiers.Select(strExpr);


                //now we have fipses, tiers, and we know 4 stack types are involved
                string[] typenames = new string[] { "AREA", "LOW", "MEDIUM", "HIGH" };

                foreach (var row in selectedtiers)
                {
                    foreach (var location in locations) //yeah naming...
                    {
                        for (long stackidx = 1; stackidx <= 4; stackidx++)
                        {
                            cobraDataSet.SYS_Emissions.AddSYS_EmissionsRow(stackidx, location.sourceidx, location.stid, location.cyid, row.Field<Int64>("TIER1"), row.Field<Int64>("TIER2"), row.Field<Int64>("TIER3"), 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, row.Field<string>("TIER1NAME"), row.Field<string>("TIER2NAME"), row.Field<string>("TIER3NAME"), location.statefips + location.countyfips, location.statename, location.countyname, typenames[stackidx - 1], 0, 0, 0, 0, 0, 0);
                            //this.sYS_EmissionsTableAdapter.Insert(stackidx, location.sourceidx, location.stid, location.cyid, row.Field<Int64>("TIER1"), row.Field<Int64>("TIER2"), row.Field<Int64>("TIER3"), 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, row.Field<string>("TIER1NAME"), row.Field<string>("TIER2NAME"), row.Field<string>("TIER3NAME"), location.statefips + location.countyfips, location.statename, location.countyname, typenames[stackidx - 1]);
                        }
                    }
                }
                //cobraDataSet.sys

                //this.sYS_EmissionsTableAdapter.Fill(cobraDataSet.SYS_Emissions);
            }
        }


        private void do_apply()
        {
            if (selectedemissions.Count() == 0)
            {
                //need to actually create emissions records
                createmissingemissionsrecords();
                //reselect so the routines to come have records to work with
                executetierfilter();
            }

            List<changedata> changes = new List<changedata>();
            changes.Add(new changedata
            {
                ispctchange = rb_pm_pct.Checked,
                isdecrease = rb_pm_dec.Checked,
                value = (double)val_pm.Value,
                controlfieldname = "CTRL_PM25",
                basefieldname = "BASE_PM25",
                deltafieldname = "DELTA_PM25",
                baseline = _base.PM25
            });
            changes.Add(new changedata
            {
                ispctchange = rb_so2_pct.Checked,
                isdecrease = rb_so2_dec.Checked,
                value = (double)val_so2.Value,
                controlfieldname = "CTRL_SO2",
                basefieldname = "BASE_SO2",
                deltafieldname = "DELTA_SO2",
                baseline = _base.SO2
            });
            changes.Add(new changedata
            {
                ispctchange = rb_nox_pct.Checked,
                isdecrease = rb_nox_dec.Checked,
                value = (double)val_nox.Value,
                controlfieldname = "CTRL_NOx",
                basefieldname = "BASE_NOx",
                deltafieldname = "DELTA_NOx",
                baseline = _base.NOx
            });
            changes.Add(new changedata
            {
                ispctchange = rb_nh3_pct.Checked,
                isdecrease = rb_nh3_dec.Checked,
                value = (double)val_nh3.Value,
                controlfieldname = "CTRL_NH3",
                basefieldname = "BASE_NH3",
                deltafieldname = "DELTA_NH3",
                baseline = _base.NH3
            });
            changes.Add(new changedata
            {
                ispctchange = rb_voc_pct.Checked,
                isdecrease = rb_voc_dec.Checked,
                value = (double)val_voc.Value,
                controlfieldname = "CTRL_VOC",
                basefieldname = "BASE_VOC",
                deltafieldname = "DELTA_VOC",
                baseline = _base.VOC
            });
            /*
            UpdateControl(rb_pm_pct.Checked, rb_pm_dec.Checked, (double)val_pm.Value, "CTRL_PM25", "BASE_PM25", "DELTA_PM25", _base.PM25); //PM2.5
            UpdateControl(rb_so2_pct.Checked, rb_so2_dec.Checked, (double)val_so2.Value, "CTRL_SO2", "BASE_SO2", "DELTA_SO2", _base.SO2); //SO2
            UpdateControl(rb_nox_pct.Checked, rb_nox_dec.Checked, (double)val_nox.Value, "CTRL_NOx", "BASE_NOx", "DELTA_NOx", _base.NOx); //NOx
            UpdateControl(rb_nh3_pct.Checked, rb_nh3_dec.Checked, (double)val_nh3.Value, "CTRL_NH3", "BASE_NH3", "DELTA_NH3", _base.NH3); //NH3
            UpdateControl(rb_voc_pct.Checked, rb_voc_dec.Checked, (double)val_voc.Value, "CTRL_VOC", "BASE_VOC", "DELTA_VOC", _base.VOC); //VOC
            */
            UpdateEmissions(changes);
            sYS_EmissionsTableAdapter.Update(selectedemissions);
        }

        private void UpdateControl(bool ispctchange, bool isdecrease, double value, string controlfieldname, string basefieldname, string deltafieldname, double baseline)
        {
            foreach (DataRow row in selectedemissions)
            {
                row.SetField("MODIFIED", 1);
                if (ispctchange) //pct modification
                {
                    if (isdecrease)
                    {
                        row.SetField(controlfieldname, row.Field<double>(basefieldname) * (1 - (value / 100)));
                    }
                    else
                    {
                        row.SetField(controlfieldname, row.Field<double>(basefieldname) * (1 + (value / 100)));
                    }
                }
                else //absolute modification
                {
                    double weight = 0;
                    //compute weight as ratio of absolute - but hold on - what if there is none?
                    if (baseline == 0)
                    {
                        weight = (double)1 / selectedemissions.Count(); // no current emissions in set to equal weight
                    }
                    else
                    {
                        weight = row.Field<double>(basefieldname) / baseline;
                    }
                    //add or subtract
                    if (isdecrease)
                    {
                        row.SetField(controlfieldname, row.Field<double>(basefieldname) - weight * value);
                    }
                    else
                    {
                        row.SetField(controlfieldname, row.Field<double>(basefieldname) + weight * value);
                    }
                }
                row.SetField(deltafieldname, row.Field<double>(basefieldname) - row.Field<double>(controlfieldname));
            }
        }

        private void UpdateEmissions(List<changedata> thechanges)
        {
            foreach (DataRow row in selectedemissions)
            {
                //row.BeginEdit(); //this does not work for sqlite
                foreach (var change in thechanges)
                {
                    row.SetField("MODIFIED", 1);
                    if (change.ispctchange) //pct modification
                    {
                        if (change.isdecrease)
                        {
                            row.SetField(change.controlfieldname, row.Field<double>(change.basefieldname) * (1 - (change.value / 100)));
                        }
                        else
                        {
                            row.SetField(change.controlfieldname, row.Field<double>(change.basefieldname) * (1 + (change.value / 100)));
                        }
                    }
                    else //absolute modification
                    {
                        double weight = 0;
                        //compute weight as ratio of absolute - but hold on - what if there is none?
                        if (change.baseline == 0)
                        {
                            weight = (double)1 / selectedemissions.Count(); // no current emissions in set to equal weight
                        }
                        else
                        {
                            weight = row.Field<double>(change.basefieldname) / change.baseline;
                        }
                        //add or subtract
                        if (change.isdecrease)
                        {
                            row.SetField(change.controlfieldname, row.Field<double>(change.basefieldname) - weight * change.value);
                        }
                        else
                        {
                            row.SetField(change.controlfieldname, row.Field<double>(change.basefieldname) + weight * change.value);
                        }
                    }
                    row.SetField(change.deltafieldname, row.Field<double>(change.basefieldname) - row.Field<double>
                        (change.controlfieldname));
                }
                //row.EndEdit(); - see above
            }
            //selectedemissions[0].Table.AcceptChanges();
            //cobraDataSet.SYS_Emissions.AcceptChanges();- see above
        }


        private void button2_Click(object sender, EventArgs e)
        {
            Cursor.Current = Cursors.WaitCursor;
            SQLiteConnection dbConn = new SQLiteConnection(Globals.connectionstring);
            dbConn.Open();
            Dataloader loader = new Dataloader(dbConn);
            loader.executeQuery("Update SYS_Emissions set MODIFIED=0, CTRL_PM25=BASE_PM25, CTRL_SO2=BASE_SO2, CTRL_NOx=BASE_NOx, CTRL_NH3=BASE_NH3, CTRL_VOC=BASE_VOC, DELTA_PM25=0, DELTA_SO2=0, DELTA_NOx=0, DELTA_NH3=0, DELTA_VOC=0;");
            dbConn.Close();
            //sYS_EmissionsTableAdapter.Fill(cobraDataSet.SYS_Emissions);

            sYS_EmissionsTableAdapter.Fill(cobraDataSet.SYS_Emissions);
            PopulateTrees();
            setDirty(true);
            sYSEmissionsBindingSource.ResetBindings(false);
            sYSEmissionsBindingSource.Filter = "(Modified=True)";

            //reset edit info
            val_pm.Value = 0;
            val_so2.Value = 0;
            val_nh3.Value = 0;
            val_nox.Value = 0;
            val_voc.Value = 0;
            lbl_pol_pm_state.Text = "";
            lbl_pol_so2_state.Text = "";
            lbl_pol_nh3_state.Text = "";
            lbl_pol_nox_state.Text = "";
            lbl_pol_voc_state.Text = "";


            lockout = true;
            foreach (TreeNode node in treeView_Locations.GetAllNodes())
            {
                if (node.Checked)
                {
                    node.Checked = false;
                    node.Collapse(false);
                }
            }
            lockout = false;



            Cursor.Current = Cursors.Default;
            SystemSounds.Exclamation.Play();
        }
        private void button4_Click(object sender, EventArgs e)
        {
            saveFileDialogScenario.Filter = "CSV File (*.csv)|*.csv";
            if (saveFileDialogScenario.ShowDialog() == DialogResult.OK)
            {
                Cursor.Current = Cursors.WaitCursor;

                bool openExportFile = false;

                try
                {
                    using (StreamWriter sw = new StreamWriter(saveFileDialogScenario.FileName))
                    {
                        // Write the header row
                        string[] colnames = new string[]
{
    "ID",
    "destindx",
    "FIPS",
    "State",
    "County",
    "Base PM 2.5",
    "Control PM 2.5",
    "Delta PM 2.5",
    "Base O3",
    "Control O3",
    "Delta O3",
    "$ Total Health Benefits(low estimate)",
    "$ Total Health Benefits(high estimate)",
    "Total Mortality(low estimate)",
    "$ Total Mortality(low estimate)",
    "Total Mortality(high estimate)",
    "$ Total Mortality(high estimate)",
    "PM Mortality, All Cause (low)",
    "$ PM Mortality, All Cause (low)",
    "PM Mortality, All Cause (high)",
    "$ PM Mortality, All Cause (high)",
    "PM Infant Mortality",
    "$ PM Infant Mortality",
    "Total O3 Mortality",
    "$ Total O3 Mortality",
    "O3 Mortality (Short-term exposure)",
    "$ O3 Mortality (Short term exposure)",
    "O3 Mortality (Long-term exposure)",
    "$ O3 Mortality (Long-term exposure)",
    "Total Asthma Symptoms",
    "$ Total Asthma Symptoms",
    "PM Asthma Symptoms, Albuterol use",
    "$ PM Asthma Symptoms, Albuterol use",
    "O3 Asthma Symptoms, Chest Tightness",
    "$ O3 Asthma Symptoms, Chest Tightness",
    "O3 Asthma Symptoms, Cough",
    "$ O3 Asthma Symptoms, Cough",
    "O3 Asthma Symptoms, Shortness of Breath",
    "$ O3 Asthma Symptoms, Shortness of Breath",
    "O3 Asthma Symptoms, Wheeze",
    "$ O3 Asthma Symptoms, Wheeze",
    "Total Incidence, Asthma",
    "$ Total Incidence, Asthma",
    "PM Incidence, Asthma",
    "$ PM Incidence, Asthma",
    "O3 Incidence, Asthma",
    "$ O3 Incidence, Asthma",
    "Total Incidence, Hay Fever/Rhinitis",
    "$ Total Incidence, Hay Fever/Rhinitis",
    "PM Incidence, Hay Fever/Rhinitis",
    "$ PM Incidence, Hay Fever/Rhinitis",
    "O3 Incidence, Hay Fever/Rhinitis",
    "$ O3 Incidence, Hay Fever/Rhinitis",
    "Total ER Visits, Respiratory",
    "$ Total ER Visits, Respiratory",
    "PM ER Visits, Respiratory",
    "$ PM ER Visits, Respiratory",
    "O3 ER Visits, Respiratory",
    "$ O3 ER Visits, Respiratory",
    "Total Hospital Admits, All Respiratory",
    "$ Total Hospital Admits All Respiratory",
    "PM Hospital Admits, All Respiratory",
    "$ PM Hospital Admits All Respiratory",
    "O3 Hospital Admits, All Respiratory",
    "$ O3 Hospital Admits All Respiratory",
    "PM Nonfatal Heart Attacks",
    "$ PM Nonfatal Heart Attacks",
    "PM Minor Restricted Activity Days",
    "$ PM Minor Restricted Activity Days",
    "PM Work Loss Days",
    "$ PM Work Loss Days",
    "PM Incidence Lung Cancer",
    "$ PM Incidence Lung Cancer",
    "PM HA Cardio Cerebro and Peripheral Vascular Disease",
    "$ PM HA Cardio Cerebro and Peripheral Vascular Disease",
    "PM HA Alzheimers Disease",
    "$ PM HA Alzheimers Disease",
    "PM HA Parkinsons Disease",
    "$ PM HA Parkinsons Disease",
    "PM Incidence Stroke",
    "$ PM Incidence Stroke",
    "PM Incidence Out of Hospital Cardiac Arrest",
    "$ PM Incidence Out of Hospital Cardiac Arrest",
    "PM ER visits All Cardiac Outcomes",
    "$ PM ER visits All Cardiac Outcomes",
    "O3 ER Visits, Asthma",
    "$ O3 ER Visits, Asthma",
    "O3 School Loss Days, All Cause",
    "$ O3 School Loss Days, All Cause"
};

                        sw.WriteLine(string.Join(",", colnames.Select(col => $"\"{col}\"")));

                        // Placeholder for totals, initialize with 0
                        double[] totals = new double[outputCols.Length];

                        //pass through data to calculate totals
                        foreach (SYS_ResultsRow result in this.cobraDataSet.SYS_Results.Rows)
                        {


                            int i = 0;
                            foreach (string colval in outputCols)
                            {
                                //build excel file
                                var propertyInfo = result.GetType().GetProperty(colval);
                                object propertyValue = propertyInfo.GetValue(result);
                                string value = Convert.ToString(propertyValue);

                                if (i > 4)
                                {
                                    // Update totals 
                                    if (double.TryParse(value, out double numericValue))
                                    {
                                        totals[i] += numericValue;
                                    }
                                }
                                i++;
                            }


                        } //end of sys_results traversal
                          //now write the totals we want the first 5 values to be empty because we dont care to sum the fips, ID, destindx, state, county cols
                        var formattedTotals = totals.Select((total, index) => index < 5 ? "\"\"" : $"\"Total: {total}\"");
                        sw.WriteLine(string.Join(",", formattedTotals));


                        //pass through data to now write the individual row values
                        foreach (SYS_ResultsRow result in this.cobraDataSet.SYS_Results.Rows)
                        {
                            List<string> rowValues = new List<string>();

                            int i = 0;
                            foreach (string colval in outputCols)
                            {
                                //build excel file
                                var propertyInfo = result.GetType().GetProperty(colval);
                                object propertyValue = propertyInfo.GetValue(result);
                                string value = Convert.ToString(propertyValue);
                                rowValues.Add($"\"{value.Replace("\"", "\"\"")}\"");

                                if (i > 4)
                                {
                                    // Update totals 
                                    if (double.TryParse(value, out double numericValue))
                                    {
                                        totals[i] += numericValue;
                                    }
                                }
                                i++;
                            }
                            sw.WriteLine(string.Join(",", rowValues));


                        }
                    }

                    RadMessageBox.SetThemeName(this.radGridView1.ThemeName);
                    DialogResult dr = RadMessageBox.Show("The data in the grid was exported successfully. Do you want to open the file?", "Export to CSV", MessageBoxButtons.YesNo, RadMessageIcon.Question);
                    if (dr == DialogResult.Yes)
                    {
                        openExportFile = true;
                    }
                }
                catch (IOException ex)
                {
                    RadMessageBox.SetThemeName(this.radGridView1.ThemeName);
                    RadMessageBox.Show(this, ex.Message, "I/O Error", MessageBoxButtons.OK, RadMessageIcon.Error);
                }

                if (openExportFile)
                {
                    try
                    {
                        System.Diagnostics.Process.Start(saveFileDialogScenario.FileName);
                    }
                    catch (Exception ex)
                    {
                        string message = String.Format("The file cannot be opened on your system.\nError message: {0}", ex.Message);
                        RadMessageBox.Show(message, "Open File", MessageBoxButtons.OK, RadMessageIcon.Error);
                    }
                }

                Cursor.Current = Cursors.Default;
            }
        }

        private void btnApply_Click(object sender, EventArgs e)
        {
            Cursor.Current = Cursors.WaitCursor;
            SQLiteConnection dbConn = new SQLiteConnection(Globals.connectionstring);
            dbConn.Open();
            Dataloader loader = new Dataloader(dbConn);
            loader.loadinventoryintocombinedemissions((int)comboBox_EmissionsSelect.SelectedValue);
            dbConn.Close();
            completedataload(comboBox_EmissionsSelect.SelectedIndex == 0 ? 2023 : 2016);
            Cursor.Current = Cursors.Default;
        }

        private void btn_select_popfile_Click(object sender, EventArgs e)
        {
            if (openFileDialogBaseline.ShowDialog() == DialogResult.OK)
            {
                popfile = openFileDialogBaseline.FileName;

                Cursor.Current = Cursors.WaitCursor;
                SQLiteConnection dbConn = new SQLiteConnection(Globals.connectionstring);
                dbConn.Open();
                Dataloader loader = new Dataloader(dbConn);
                try
                {
                    loader.loadpopulationfile(popfile);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("An import error occured: " + ex.Message + " Please select another file.");
                    rblockout = true;
                    rb_pop_file.Checked = false;
                    rblockout = false;
                    dbConn.Close();
                    btn_select_popfile.Text = "select file";
                    Cursor.Current = Cursors.Default;
                    popfile = "";
                    return;
                }

                dbConn.Close();

                btn_select_popfile.Text = popfile;
                rblockout = true;
                rb_pop_file.Checked = true;
                rblockout = false;
                Cursor.Current = Cursors.Default;
            }
            else
            {
                btn_select_popfile.Text = "select file";
                popfile = "";
            }
            setDirty(true);
        }

        private void rb_pop_file_CheckedChanged(object sender, EventArgs e)
        {
            if (!rblockout)
            {
                popfile = "";
                if (rb_pop_file.Checked)
                {
                    btn_select_popfile_Click(null, null);
                }
            }
            setDirty(true);
        }

        private void btn_select_incidencefile_Click(object sender, EventArgs e)
        {
            if (openFileDialogBaseline.ShowDialog() == DialogResult.OK)
            {
                incidencefile = openFileDialogBaseline.FileName;

                Cursor.Current = Cursors.WaitCursor;
                SQLiteConnection dbConn = new SQLiteConnection(Globals.connectionstring);
                dbConn.Open();
                Dataloader loader = new Dataloader(dbConn);
                try
                {
                    loader.loadincidencefile(incidencefile);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("An import error occured: " + ex.Message + " Please select another file.");
                    rblockout = true;
                    rb_PM_Incidence_file.Checked = false;
                    rblockout = false;
                    dbConn.Close();
                    btn_select_incidencefile.Text = "select file";
                    Cursor.Current = Cursors.Default;
                    incidencefile = "";
                    return;
                }
                dbConn.Close();

                btn_select_incidencefile.Text = incidencefile;
                rblockout = true;
                rb_PM_Incidence_file.Checked = true;
                rblockout = false;
                Cursor.Current = Cursors.Default;
            }
            else
            {
                btn_select_incidencefile.Text = "select file";
                incidencefile = "";
            }
            setDirty(true);
        }

        private void btn_select_crfile_Click(object sender, EventArgs e)
        {
            if (openFileDialogBaseline.ShowDialog() == DialogResult.OK)
            {
                crfile = openFileDialogBaseline.FileName;

                Cursor.Current = Cursors.WaitCursor;
                SQLiteConnection dbConn = new SQLiteConnection(Globals.connectionstring);
                dbConn.Open();
                Dataloader loader = new Dataloader(dbConn);
                try
                {
                    loader.loadcrfile(crfile);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("An import error occured: " + ex.Message + " Please select another file.");
                    rblockout = true;
                    rb_cr_file.Checked = false;
                    rblockout = false;
                    dbConn.Close();
                    btn_select_crfile.Text = "select file";
                    Cursor.Current = Cursors.Default;
                    crfile = "";
                    return;
                }
                dbConn.Close();

                btn_select_crfile.Text = crfile;
                rblockout = true;
                rb_cr_file.Checked = true;
                rblockout = false;
                Cursor.Current = Cursors.Default;
            }
            else
            {
                btn_select_crfile.Text = "select file";
                crfile = "";
            }
            setDirty(true);
        }

        private void rb_cr_file_CheckedChanged(object sender, EventArgs e)
        {
            if (!rblockout)
            {
                crfile = "";
                if (rb_cr_file.Checked)
                {
                    btn_select_crfile_Click(null, null);
                }
            }
            setDirty(true);
        }

        private void btn_select_valfile_Click(object sender, EventArgs e)
        {
            if (openFileDialogBaseline.ShowDialog() == DialogResult.OK)
            {
                valuationfile = openFileDialogBaseline.FileName;

                Cursor.Current = Cursors.WaitCursor;
                SQLiteConnection dbConn = new SQLiteConnection(Globals.connectionstring);
                dbConn.Open();
                Dataloader loader = new Dataloader(dbConn);
                try
                {
                    loader.loadvaluationfile(valuationfile);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("An import error occured: " + ex.Message + " Please select another file.");
                    rblockout = true;
                    rb_val_file.Checked = false;
                    rblockout = false;
                    dbConn.Close();
                    btn_select_valfile.Text = "select file";
                    Cursor.Current = Cursors.Default;
                    valuationfile = "";
                    return;
                }
                dbConn.Close();

                btn_select_valfile.Text = valuationfile;
                rblockout = true;
                rb_val_file.Checked = true;
                rblockout = false;
                Cursor.Current = Cursors.Default;
            }
            else
            {
                btn_select_valfile.Text = "select file";
                valuationfile = "";
            }
            setDirty(true);
        }

        private void rb_val_file_CheckedChanged(object sender, EventArgs e)
        {
            if (!rblockout)
            {
                valuationfile = "";
                if (rb_val_file.Checked)
                {
                    btn_select_valfile_Click(null, null);
                }
            }
            setDirty(true);
        }

        private void radioButtonDR7_CheckedChanged(object sender, EventArgs e)
        {
            setDirty(true);
        }

        private string emissionsfieldnamefromfriendlyname(string friendlyname)
        {
            foreach (var column in radGridView2.Columns)
            {
                if (column.HeaderText == friendlyname)
                {
                    return column.FieldName;
                }
            }
            return "error";
        }

        private string fieldnamefromfriendlyname(string friendlyname)
        {
            foreach (var column in radGridView1.MasterTemplate.Columns)
            {
                if (column.HeaderText.ToUpper() == friendlyname.ToUpper())
                {
                    return column.FieldName;
                }
            }
            return "error";
        }

        private string friendlynamefromfieldname(string fieldname)
        {
            foreach (var column in radGridView1.MasterTemplate.Columns)
            {
                if (column.FieldName.ToUpper() == fieldname.ToUpper())
                {
                    return column.HeaderText;
                }
            }
            return "error";
        }


        /* excel export !!*/
        private void button1_Click(object sender, EventArgs e)
        {
            saveFileDialogScenario.Filter = "Excel (*.xlsx)|*.xlsx";
            if (saveFileDialogScenario.ShowDialog() == DialogResult.OK)
            {
                Cursor.Current = Cursors.WaitCursor;

                bool openExportFile = false;

                using (var workbook = new XLWorkbook())
                {
                    workbook.Author = "COBRA WEB API";
                    var worksheet = workbook.Worksheets.Add("Scenario");
                    string[] colnames = new string[]
{
    "ID",
    "destindx",
    "FIPS",
    "State",
    "County",
    "Base PM 2.5",
    "Control PM 2.5",
    "Delta PM 2.5",
    "Base O3",
    "Control O3",
    "Delta O3",
    "$ Total Health Benefits(low estimate)",
    "$ Total Health Benefits(high estimate)",
    "Total Mortality(low estimate)",
    "$ Total Mortality(low estimate)",
    "Total Mortality(high estimate)",
    "$ Total Mortality(high estimate)",
    "PM Mortality, All Cause (low)",
    "$ PM Mortality, All Cause (low)",
    "PM Mortality, All Cause (high)",
    "$ PM Mortality, All Cause (high)",
    "PM Infant Mortality",
    "$ PM Infant Mortality",
    "Total O3 Mortality",
    "$ Total O3 Mortality",
    "O3 Mortality (Short-term exposure)",
    "$ O3 Mortality (Short term exposure)",
    "O3 Mortality (Long-term exposure)",
    "$ O3 Mortality (Long-term exposure)",
    "Total Asthma Symptoms",
    "$ Total Asthma Symptoms",
    "PM Asthma Symptoms, Albuterol use",
    "$ PM Asthma Symptoms, Albuterol use",
    "O3 Asthma Symptoms, Chest Tightness",
    "$ O3 Asthma Symptoms, Chest Tightness",
    "O3 Asthma Symptoms, Cough",
    "$ O3 Asthma Symptoms, Cough",
    "O3 Asthma Symptoms, Shortness of Breath",
    "$ O3 Asthma Symptoms, Shortness of Breath",
    "O3 Asthma Symptoms, Wheeze",
    "$ O3 Asthma Symptoms, Wheeze",
    "Total Incidence, Asthma",
    "$ Total Incidence, Asthma",
    "PM Incidence, Asthma",
    "$ PM Incidence, Asthma",
    "O3 Incidence, Asthma",
    "$ O3 Incidence, Asthma",
    "Total Incidence, Hay Fever/Rhinitis",
    "$ Total Incidence, Hay Fever/Rhinitis",
    "PM Incidence, Hay Fever/Rhinitis",
    "$ PM Incidence, Hay Fever/Rhinitis",
    "O3 Incidence, Hay Fever/Rhinitis",
    "$ O3 Incidence, Hay Fever/Rhinitis",
    "Total ER Visits, Respiratory",
    "$ Total ER Visits, Respiratory",
    "PM ER Visits, Respiratory",
    "$ PM ER Visits, Respiratory",
    "O3 ER Visits, Respiratory",
    "$ O3 ER Visits, Respiratory",
    "Total Hospital Admits, All Respiratory",
    "$ Total Hospital Admits All Respiratory",
    "PM Hospital Admits, All Respiratory",
    "$ PM Hospital Admits All Respiratory",
    "O3 Hospital Admits, All Respiratory",
    "$ O3 Hospital Admits All Respiratory",
    "PM Nonfatal Heart Attacks",
    "$ PM Nonfatal Heart Attacks",
    "PM Minor Restricted Activity Days",
    "$ PM Minor Restricted Activity Days",
    "PM Work Loss Days",
    "$ PM Work Loss Days",
    "PM Incidence Lung Cancer",
    "$ PM Incidence Lung Cancer",
    "PM HA Cardio Cerebro and Peripheral Vascular Disease",
    "$ PM HA Cardio Cerebro and Peripheral Vascular Disease",
    "PM HA Alzheimers Disease",
    "$ PM HA Alzheimers Disease",
    "PM HA Parkinsons Disease",
    "$ PM HA Parkinsons Disease",
    "PM Incidence Stroke",
    "$ PM Incidence Stroke",
    "PM Incidence Out of Hospital Cardiac Arrest",
    "$ PM Incidence Out of Hospital Cardiac Arrest",
    "PM ER visits All Cardiac Outcomes",
    "$ PM ER visits All Cardiac Outcomes",
    "O3 ER Visits, Asthma",
    "$ O3 ER Visits, Asthma",
    "O3 School Loss Days, All Cause",
    "$ O3 School Loss Days, All Cause"
};
                    var colcount = 1;
                    foreach (string colname in colnames)
                    {
                        worksheet.Cell(1, colcount).Value = colname;
                        colcount++;


                    }

                    var rngHeader = worksheet.Range("A1:CK1");
                    rngHeader.Style.Font.Bold = true;
                    rngHeader.Style.Fill.BackgroundColor = XLColor.Aqua;

                    //function to create totals 

                    #region Header_Totals

                    int columnNumber = 12;

                    //starting with the 12th header val(colnames) aka Total health benefits
                    var cellWithFormulaA1 = worksheet.Cell(2, 12);
                    for (int i = 11; i < colnames.Length; i += 2)
                    {
                        cellWithFormulaA1 = worksheet.Cell(2, columnNumber);
                        string columnLetter = GetExcelColumnLetter(columnNumber);
                        cellWithFormulaA1.FormulaA1 = string.Format("=\"Total: \"&TEXT(ROUND(SUM({0}3:{0}3110),2),\"#,###,##0.00\")", columnLetter);

                        columnLetter = GetExcelColumnLetter(columnNumber + 1);
                        string formulaA1Currency = string.Format("=\"Total: \"&TEXT(ROUND(SUM({0}3:{0}3110),2),\"$#,###,##0.00\")", columnLetter);
                        worksheet.Cell(2, columnNumber + 1).FormulaA1 = formulaA1Currency;

                        columnNumber += 2;
                    }



                    //$ Total Health Benefits(low estimate)
                    /* var cellWithFormulaA1 = worksheet.Cell(2, 12);
                         cellWithFormulaA1.FormulaA1 = "=\"Total: \"&TEXT(ROUND(SUM(L3:L3110),2),\"$#,###,##0.00\")"; 
                     //$ Total Health Benefits(high estimate)
                     cellWithFormulaA1 = worksheet.Cell(2, 13);
                     cellWithFormulaA1.FormulaA1 = "=\"Total: \"&TEXT(ROUND(SUM(M3:M3110),2),\"$#,###,##0.00\")";
                     //Mortality(low estimate)*/



                    #endregion
                    var headerTotals = worksheet.Range("F2:CK2");
                    headerTotals.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                    int rowcount = 3;

                    //make sure to reset dict whenever this func is called:
                    foreach (string key in SummaryTableVals.Keys)
                    {
                        SummaryTableVals[key]["high"] = "0";
                        SummaryTableVals[key]["low"] = "0";
                        SummaryTableVals[key]["highval"] = "0";
                        SummaryTableVals[key]["lowval"] = "0";

                    }

                    foreach (SYS_ResultsRow result in this.cobraDataSet.SYS_Results.Rows)
                    {
                        var colnum = 1;


                        foreach (string colval in outputCols)
                        {
                            //build excel file
                            var propertyInfo = result.GetType().GetProperty(colval);
                            object propertyValue = propertyInfo.GetValue(result);
                            worksheet.Cell(rowcount, colnum).Value = propertyValue;

                            string summaryKey;

                            if (colPropertyDict.TryGetValue(colval, out summaryKey))
                            {

                                if (SummaryTableVals[summaryKey]["lowproperty"] == colval)
                                {
                                    double curVal = double.Parse(SummaryTableVals[summaryKey]["low"]);
                                    curVal += (double)propertyValue;
                                    SummaryTableVals[summaryKey]["low"] = curVal.ToString();
                                }
                                if (SummaryTableVals[summaryKey]["highproperty"] == colval)
                                {
                                    double curVal = double.Parse(SummaryTableVals[summaryKey]["high"]);
                                    curVal += (double)propertyValue;
                                    SummaryTableVals[summaryKey]["high"] = curVal.ToString();
                                }
                                if (SummaryTableVals[summaryKey]["highvalproperty"] == colval)
                                {
                                    double curVal = double.Parse(SummaryTableVals[summaryKey]["highval"]);
                                    curVal += (double)propertyValue;
                                    SummaryTableVals[summaryKey]["highval"] = curVal.ToString();
                                }
                                if (SummaryTableVals[summaryKey]["lowvalproperty"] == colval)
                                {
                                    double curVal = double.Parse(SummaryTableVals[summaryKey]["lowval"]);
                                    curVal += (double)propertyValue;
                                    SummaryTableVals[summaryKey]["lowval"] = curVal.ToString();
                                }

                            }
                            else
                            {
                                //Console.WriteLine($"Key {colval} not found for summary table results.");
                            }
                            /*if (propertyInfo != null)
                            {
                                object propertyValue = propertyInfo.GetValue(result);
                                worksheet.Cell(rowcount,colnum).Value = propertyValue;
                            }*/

                            colnum++;


                        }

                        rowcount++;
                    }


                    var rngData = worksheet.Range("F2:CK3111");
                    rngData.Style.NumberFormat.Format = "#,##0.00";

                    #region Footer_Totals


                    columnNumber = 12;

                    //starting with the 12th header val(colnames) aka Total health benefits
                    cellWithFormulaA1 = worksheet.Cell(3111, 12);
                    for (int i = 11; i < colnames.Length; i += 2)
                    {
                        cellWithFormulaA1 = worksheet.Cell(3111, columnNumber);
                        string columnLetter = GetExcelColumnLetter(columnNumber);
                        cellWithFormulaA1.FormulaA1 = string.Format("=\"Total: \"&TEXT(ROUND(SUM({0}3:{0}3110),2),\"#,###,##0.00\")", columnLetter);

                        columnLetter = GetExcelColumnLetter(columnNumber + 1);
                        string formulaA1Currency = string.Format("=\"Total: \"&TEXT(ROUND(SUM({0}3:{0}3110),2),\"$#,###,##0.00\")", columnLetter);
                        worksheet.Cell(3111, columnNumber + 1).FormulaA1 = formulaA1Currency;

                        columnNumber += 2;
                    }


                    //$ Total Health Benefits(low estimate)
                    /*cellWithFormulaA1 = worksheet.Cell(3111, 9);
						cellWithFormulaA1.FormulaA1 = "=\"Total: \"&TEXT(ROUND(SUM(I3:I3110),2),\"$#,###,##0.00\")";
                    //$ Total Health Benefits(high estimate)*/

                    #endregion



                    var footerTotals = worksheet.Range("F3111:CK3111");
                    footerTotals.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                    worksheet.Columns(1, 2).Hide();

                    worksheet.Columns().AdjustToContents();
                    workbook.SaveAs(saveFileDialogScenario.FileName);
                }
                try
                {
                    //excelExporter.RunExport(saveFileDialogScenario.FileName);
                    RadMessageBox.SetThemeName(this.radGridView1.ThemeName);
                    DialogResult dr = RadMessageBox.Show("The data in the grid was exported successfully. Do you want to open the file?", "Export to Excel", MessageBoxButtons.YesNo, RadMessageIcon.Question);
                    if (dr == DialogResult.Yes)
                    { openExportFile = true; }
                }
                catch (IOException ex)
                {
                    RadMessageBox.SetThemeName(this.radGridView1.ThemeName);
                    RadMessageBox.Show(this, ex.Message, "I/O Error", MessageBoxButtons.OK, RadMessageIcon.Error);
                }
                if (openExportFile)
                {
                    try
                    {
                        System.Diagnostics.Process.Start(saveFileDialogScenario.FileName);
                    }
                    catch (Exception ex)
                    {
                        string message = String.Format("The file cannot be opened on your system.\nError message: {0}", ex.Message);
                        RadMessageBox.Show(message, "Open File", MessageBoxButtons.OK, RadMessageIcon.Error);
                    }
                }
                Cursor.Current = Cursors.Default;
            }
        }

        private void MasterTemplate_Click(object sender, EventArgs e)
        {

        }

        private void radGridView2_ViewCellFormatting(object sender, CellFormattingEventArgs e)
        {
            /*
            if (e.CellElement.RowInfo.Group == null && e.CellElement is GridSummaryCellElement)
{
    e.CellElement.ForeColor = Color.Blue;
}
else
{
    e.CellElement.ForeColor = Color.Black;
}*/
        }

        private void radGridView1_ViewCellFormatting(object sender, CellFormattingEventArgs e)
        {
            /*
            if (e.CellElement.RowInfo.Group == null && e.CellElement is GridSummaryCellElement)
            {
                e.CellElement.ForeColor = Color.Blue;
            }
            else
            {
                e.CellElement.ForeColor = Color.Black;
            }
            */
        }

        private void btnManual_Click(object sender, EventArgs e)
        {
            try
            {
                string path = System.Reflection.Assembly.GetExecutingAssembly().CodeBase;
                var directory = System.IO.Path.GetDirectoryName(path);
                System.Diagnostics.Process.Start(directory + "\\manual\\COBRA user manual.pdf");
            }
            catch (Exception ex)
            {
                string message = String.Format("The file cannot be opened on your system.\nError message: {0}", ex.Message);
                RadMessageBox.Show(message, "Open File", MessageBoxButtons.OK, RadMessageIcon.Error);
            }
        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {

        }

        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void showManualToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                string path = System.Reflection.Assembly.GetExecutingAssembly().CodeBase;
                var directory = System.IO.Path.GetDirectoryName(path);
                System.Diagnostics.Process.Start(directory + "\\manual\\COBRA user manual.pdf");
            }
            catch (Exception ex)
            {
                string message = String.Format("The file cannot be opened on your system.\nError message: {0}", ex.Message);
                RadMessageBox.Show(message, "Open File", MessageBoxButtons.OK, RadMessageIcon.Error);
            }

        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RadMessageBox.SetThemeName("ControlDefault");
            string message = String.Format("Co-Benefits Risk Assessment (COBRA) Health Impacts Screening and Mapping Tool\n\n Developed by Abt Associates, Inc. under contract with EPA's State and Local Energy and Environment Program. For more information, please contact Emma Zinsmeister at +1 (202) 343-9043 or Zinsmeister.Emma@epa.gov.");
            RadMessageBox.Show(message, "About COBRA", MessageBoxButtons.OK);
        }

        private void radioButton_AY_CheckedChanged(object sender, EventArgs e)
        {
            //set visibility on options
            /*   if (radioButton_AY.Checked)
               {
                   //using Analysis Year Approach
                   comboBox_EmissionsSelect.Enabled = false;
                   btnApplyEmissions.Enabled = false;
                   comboBox_AnalysisYearSelect.Enabled = true;
                   button_ApplyAnalysisyear.Enabled = true;
                   panel_AdvOptions.Visible = false;
               }
               else
               {
                   //using manual setup process
                   comboBox_EmissionsSelect.Enabled = true;
                   btnApplyEmissions.Enabled = true;
                   comboBox_AnalysisYearSelect.Enabled = false;
                   button_ApplyAnalysisyear.Enabled = false;
                   panel_AdvOptions.Visible = true;
               }  */

        }

        private void button_ApplyAnalysisyear_Click(object sender, EventArgs e)
        {
            Cursor.Current = Cursors.WaitCursor;
            //set radio buttons
            radioButton_PreloadIncidence.Checked = true;
            radioButton_PreloadPopulance.Checked = true;
            radioButton_PreloadCR.Checked = true;
            radioButton_PreloadValue.Checked = true;
            //set dropdowns, using trickery as the indices are synchronized
            comboBox_Incidence.SelectedIndex = comboBox_AnalysisYearSelect.SelectedIndex;
            comboBox_PopData.SelectedIndex = comboBox_AnalysisYearSelect.SelectedIndex;
            //comboBox_CRFunctions.SelectedValue = comboBox_AnalysisYearSelect.SelectedValue;
            comboBox_ValueFunctions.SelectedIndex = comboBox_AnalysisYearSelect.SelectedIndex;
            //load emissions data
            SQLiteConnection dbConn = new SQLiteConnection(Globals.connectionstring);
            dbConn.Open();
            Dataloader loader = new Dataloader(dbConn);
            //DataRowView selectedItem = (DataRowView)comboBox_EmissionsSelect.SelectedValue;
            loader.loadinventoryintocombinedemissions((int)comboBox_AnalysisYearSelect.SelectedValue);


            dbConn.Close();

            int year = 0;
            switch (comboBox_AnalysisYearSelect.SelectedIndex)
            {
                case 0:
                    year = 2016;
                    break;
                case 1:
                    year = 2023;
                    break;
                default:
                    year = 2028;
                    break;
            }

            completedataload(year);

            tabControl_Main.SelectedIndex = 2;
            tabControlCombinedEmissions.SelectedIndex = 0;


            Cursor.Current = Cursors.Default;
        }

        private void button_clear_states_Click(object sender, EventArgs e)
        {
            Boolean storedlockout = lockout;
            lockout = true;
            treeView_Locations.SelectedNode = null;
            foreach (TreeNode item in treeView_Locations.Nodes)
            {
                item.Checked = false;
                Boolean isexpanded = item.IsExpanded;
                if (item.Nodes.Count > 0)
                {
                    this.CheckTreeViewNodeExp(item, false, true);
                    //item.Collapse();
                }
                if (isexpanded) { item.Expand(); } else { item.Collapse(true); };
            }
            lockout = storedlockout;
        }

        private void button_clear_tiers_Click(object sender, EventArgs e)
        {
            Boolean storedlockout = lockout;
            lockout = true;
            treeView_Tiers.SelectedNode = null;
            lockout = storedlockout;

        }

        private void radioButton_data1_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton_data1.Checked)
            {
                comboBox_EmissionsSelect.Enabled = true;
                btnApplyEmissions.Enabled = true;
                btnEmissions.Enabled = false;
                btnBaseControl.Enabled = false;
            };
            if (radioButton_data2.Checked)
            {
                comboBox_EmissionsSelect.Enabled = false;
                btnApplyEmissions.Enabled = false;
                btnEmissions.Enabled = true;
                btnBaseControl.Enabled = false;
            };
            if (radioButton_data3.Checked)
            {
                comboBox_EmissionsSelect.Enabled = false;
                btnApplyEmissions.Enabled = false;
                btnEmissions.Enabled = false;
                btnBaseControl.Enabled = true;
            };



        }

        private void button_Proceed_Click(object sender, EventArgs e)
        {
            tabControl_Main.SelectedIndex = 2;
            tabControlCombinedEmissions.SelectedIndex = 0;
        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void panel2_Paint(object sender, PaintEventArgs e)
        {

        }

        private void label17_Click(object sender, EventArgs e)
        {

        }

        private void executetierfilter(string query)
        {
            //set summaries to 0
            _base.NOx = 0;
            _base.SO2 = 0;
            _base.NH3 = 0;
            _base.PM25 = 0;
            _base.VOC = 0;
            _control.NOx = 0;
            _control.SO2 = 0;
            _control.NH3 = 0;
            _control.PM25 = 0;
            _control.VOC = 0;

            selectedemissions = cobraDataSet.SYS_Emissions.Select(query);

            // create summaries for selected records
            foreach (var row in selectedemissions)
            {
                _base.NOx = _base.NOx + row.Field<double>("BASE_NOx");
                _base.SO2 = _base.SO2 + row.Field<double>("BASE_SO2");
                _base.NH3 = _base.NH3 + row.Field<double>("BASE_NH3");
                _base.PM25 = _base.PM25 + row.Field<double>("BASE_PM25");
                _base.VOC = _base.VOC + row.Field<double>("BASE_VOC");

                _control.NOx = _control.NOx + row.Field<double>("CTRL_NOx");
                _control.SO2 = _control.SO2 + row.Field<double>("CTRL_SO2");
                _control.NH3 = _control.NH3 + row.Field<double>("CTRL_NH3");
                _control.PM25 = _control.PM25 + row.Field<double>("CTRL_PM25");
                _control.VOC = _control.VOC + row.Field<double>("CTRL_VOC");
            }

        }

        private void fixfips(AvertRec record)
        {
            string targetfips = record.FIPS.ToString().PadLeft(5, '0');
            if (targetfips == "51515")  //does not exists
            {
                record.COUNTY = "Bedford";
                record.FIPS = 51019;
            }
        }





        private void btnAvert_Click(object sender, EventArgs e)
        {
            bool warnAvertAmt = false;

            //if (datayear == 2025)
            //{
            //    MessageBox.Show("Warning: you have the 2025 COBRA baseline selected. We strongly recommend using the 2017 COBRA baseline when importing results from AVERT. To select the 2017 baseline, go to the Select Analysis Year tab, select 2017 in the dropdown menu, and click Apply Analysis Year Data.", "Data Year");
            //}

            string msgAvert = "You are about to import an AVERT file. The AVERT changes will overwrite any emissions changes you may have entered into COBRA. This will affect only the Fuel Combustion: Electric Utility and Highway Vehicles tiers for the counties included in the AVERT file. Other tiers and counties will be unaffected. Do you wish to proceed?";
            DialogResult dialogResult = MessageBox.Show(msgAvert, "AVERT import", MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.Yes)
            {
                if (openFileDialogBaseline.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        if (datayear == 0)
                        {
                            MessageBox.Show("Loading 2016 baseline.", "Data Year");
                            comboBox_AnalysisYearSelect.SelectedIndex = 1;
                            button_ApplyAnalysisyear_Click(null, null);
                        }

                        Cursor.Current = Cursors.WaitCursor;
                        using (TextReader fileReader = File.OpenText(openFileDialogBaseline.FileName))
                        {

                            try
                            {
                                CsvReader csv = new CsvReader(fileReader, System.Globalization.CultureInfo.CurrentCulture);
                                csv.Configuration.RegisterClassMap(new AvertRecMap());
                                csv.Configuration.HeaderValidated = null;
                                List<AvertRec> records = csv.GetRecords<AvertRec>().ToList();

                                List<AvertRec> groupedrecords = records.GroupBy(i => new { i.FIPS, i.STATE, i.COUNTY, i.TIER1NAME }).Select(g => new AvertRec
                                {
                                    FIPS = g.Key.FIPS,
                                    STATE = g.Key.STATE,
                                    COUNTY = g.Key.COUNTY,
                                    TIER1NAME = g.Key.TIER1NAME,
                                    NOx_REDUCTIONS_TONS = g.Sum(i => i.NOx_REDUCTIONS_TONS),
                                    SO2_REDUCTIONS_TONS = g.Sum(i => i.SO2_REDUCTIONS_TONS),
                                    PM25_REDUCTIONS_TONS = g.Sum(i => i.PM25_REDUCTIONS_TONS),
                                    VOCS_REDUCTIONS_TONS = g.Sum(i => i.VOCS_REDUCTIONS_TONS),
                                    NH3_REDUCTIONS_TONS = g.Sum(i => i.NH3_REDUCTIONS_TONS)
                                }).ToList();

                                foreach (var record in groupedrecords)
                                {
                                    if ((record.TIER1NAME.ToUpper().Trim() != "FUEL COMBUSTION: ELECTRIC UTILITY") && (record.TIER1NAME.ToUpper().Trim() != "FUEL COMB. ELEC. UTIL.") && record.TIER1NAME.ToUpper().Trim() != "HIGHWAY VEHICLES")
                                    {
                                        MessageBox.Show("Encountered a record other than FUEL COMBUSTION: ELECTRIC UTILITY or HIGHWAY VEHICLES (" + record.TIER1NAME.Trim() + "). Skipping record.", "Data Format");
                                    }
                                    //else
                                    {
                                        fixfips(record);

                                        string targettier = "1"; //default
                                        if (record.TIER1NAME.ToUpper().Trim() == "HIGHWAY VEHICLES")
                                        {
                                            targettier = "11";
                                        }

                                        string targetfips = record.FIPS.ToString().PadLeft(5, '0');

                                        var strExpr = "(TIER1 = " + targettier + ") and FIPS='" + targetfips + "'";
                                        executetierfilter(strExpr);

                                        bool canapply = true;  //new avert permissivenes

                                        // check absolute decrease - limited by what is actually there, fix for sign
                                        //pm 2.5
                                        //bool canapply = (-1 * record.PM25_REDUCTIONS_TONS.GetValueOrDefault(0) <= _base.PM25); //can't reduce by more than is there
                                        if (!canapply)
                                        {
                                            //MessageBox.Show("The PM emissions reductions for " + record.COUNTY + ", " + record.STATE + ", in the AVERT output of " + record.PM25_REDUCTIONS_TONS.GetValueOrDefault(0).ToString() + " tons are greater than the baseline PM emissions in COBRA of " + _base.PM25.ToString() + " tons. Therefore, the PM emissions have been set to 0 tons for this county.", "Data Value");
                                            warnAvertAmt = true;
                                            record.PM25_REDUCTIONS_TONS = -_base.PM25;
                                        }
                                        //canapply = (-1 * record.SO2_REDUCTIONS_TONS.GetValueOrDefault(0) <= _base.SO2); //can't reduce by more than is there
                                        if (!canapply)
                                        {
                                            //MessageBox.Show("The SO2 emissions reductions for " + record.COUNTY + ", " + record.STATE + ", in the AVERT output of " + record.SO2_REDUCTIONS_TONS.GetValueOrDefault(0).ToString() + " tons are greater than the baseline SO2 emissions in COBRA of " + _base.SO2.ToString() + " tons. Therefore, the SO2 emissions have been set to 0 tons for this county.", "Data Value");
                                            warnAvertAmt = true;
                                            record.SO2_REDUCTIONS_TONS = -_base.SO2;
                                        }
                                        //canapply = (-1 * record.NOx_REDUCTIONS_TONS.GetValueOrDefault(0) <= _base.NOx); //can't reduce by more than is there
                                        if (!canapply)
                                        {
                                            //MessageBox.Show("The NOx emissions reductions for " + record.COUNTY + ", " + record.STATE + ", in the AVERT output of " + record.NOx_REDUCTIONS_TONS.GetValueOrDefault(0).ToString() + " tons are greater than the baseline NOx emissions in COBRA of " + _base.NOx.ToString() + " tons. Therefore, the NOx emissions have been set to 0 tons for this county.", "Data Value");
                                            warnAvertAmt = true;
                                            record.NOx_REDUCTIONS_TONS = -_base.NOx;
                                        }



                                        //because we reduce reduction to match canapply is always true
                                        if (true)
                                        {
                                            if (selectedemissions.Count() == 0)
                                            {
                                                //need to actually create emissions records
                                                var selectedtiers = cobraDataSet.SYS_Tiers.Select("TIER1=" + targettier);
                                                //now we have fipses, tiers, and we know 4 stack types are involved
                                                string[] typenames = new string[] { "AREA", "LOW", "MEDIUM", "HIGH" };
                                                foreach (var row in selectedtiers)
                                                {
                                                    //need the right location, cheap by iterating through tree until you find it.
                                                    LocationDesc location = null;
                                                    foreach (TreeNode node in treeView_Locations.GetAllNodes())
                                                    {
                                                        location = (LocationDesc)node.Tag;
                                                        if (targetfips == (location.statefips + location.countyfips))
                                                        {
                                                            break;
                                                        }

                                                    }
                                                    for (long stackidx = 1; stackidx <= 4; stackidx++)
                                                    {
                                                        cobraDataSet.SYS_Emissions.AddSYS_EmissionsRow(stackidx, location.sourceidx, location.stid, location.cyid, row.Field<Int64>("TIER1"), row.Field<Int64>("TIER2"), row.Field<Int64>("TIER3"), 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, row.Field<string>("TIER1NAME"), row.Field<string>("TIER2NAME"), row.Field<string>("TIER3NAME"), location.statefips + location.countyfips, location.statename, location.countyname, typenames[stackidx - 1], 0, 0, 0, 0, 0, 0);
                                                    }
                                                }
                                                //reselect so the routines to come have records to work with
                                                executetierfilter(strExpr);
                                            }
                                            bool isdecrease = record.PM25_REDUCTIONS_TONS.GetValueOrDefault(0) < 0;
                                            UpdateControl(false, isdecrease, Math.Abs(record.PM25_REDUCTIONS_TONS.GetValueOrDefault(0)), "CTRL_PM25", "BASE_PM25", "DELTA_PM25", _base.PM25); //PM2.5
                                            isdecrease = record.SO2_REDUCTIONS_TONS.GetValueOrDefault(0) < 0;
                                            UpdateControl(false, isdecrease, Math.Abs(record.SO2_REDUCTIONS_TONS.GetValueOrDefault(0)), "CTRL_SO2", "BASE_SO2", "DELTA_SO2", _base.SO2); //SO2
                                            isdecrease = record.NOx_REDUCTIONS_TONS.GetValueOrDefault(0) < 0;
                                            UpdateControl(false, isdecrease, Math.Abs(record.NOx_REDUCTIONS_TONS.GetValueOrDefault(0)), "CTRL_NOx", "BASE_NOx", "DELTA_NOx", _base.NOx); //NOx

                                            isdecrease = record.NH3_REDUCTIONS_TONS.GetValueOrDefault(0) < 0;
                                            UpdateControl(false, isdecrease, Math.Abs(record.NH3_REDUCTIONS_TONS.GetValueOrDefault(0)), "CTRL_NH3", "CTRL_NH3", "DELTA_NH3", _base.NH3);
                                            isdecrease = record.VOCS_REDUCTIONS_TONS.GetValueOrDefault(0) < 0;
                                            UpdateControl(false, isdecrease, Math.Abs(record.VOCS_REDUCTIONS_TONS.GetValueOrDefault(0)), "CTRL_VOC", "BASE_VOC", "DELTA_VOC", _base.VOC);

                                            sYS_EmissionsTableAdapter.Update(selectedemissions);
                                            setDirty(true);
                                        }
                                    }

                                }
                            }
                            catch (Exception exca)
                            {
                                MessageBox.Show("Error: There was an error processing the AVERT output file. Please check the file format.", "Error.");
                                Cursor.Current = Cursors.Default;
                                return;
                            }
                        }
                        Cursor.Current = Cursors.Default;
                        /* if (warnAvertAmt)
                         {
                             MessageBox.Show("AVERT output file has been imported. One or more emissions reductions in the AVERT output are greater than the baseline emissions in COBRA. Emissions have been set to 0 tons in these cases.", "Done");
                         }
                         else*/
                        {
                            MessageBox.Show("AVERT output file has been imported.", "Done");
                        }
                    }
                    catch (Exception exc)
                    {
                        MessageBox.Show("Error: Please close the AVERT output file before importing it into COBRA.", "Error.");
                    }
                }
                Cursor.Current = Cursors.Default;
            }

        }

        private void richTextBox2_TextChanged(object sender, EventArgs e)
        {
            //auto created when i dragged and dropped the rhich text box editor

        }

        private void label21_Click(object sender, EventArgs e)
        {

        }

        private void label22_Click(object sender, EventArgs e)
        {

        }

        private void comboBox_States_SelectedIndexChanged_1(object sender, EventArgs e)
        {


        }

        private void comboBox_PopData_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void groupBox1_Enter(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void radioButtonDR3_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void tabPageHealth_Click(object sender, EventArgs e)
        {

        }

        private void label14_Click(object sender, EventArgs e)
        {

        }

        private void radioButton_data3_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void label16_Click(object sender, EventArgs e)
        {

        }

        private void tabPage8_Click(object sender, EventArgs e)
        {

        }

        private void tabPage3_Click(object sender, EventArgs e)
        {

        }

        private void label4_Click(object sender, EventArgs e)
        {

        }

        private void groupBox_DiscountRate_Enter(object sender, EventArgs e)
        {

        }

        private void map1_Load(object sender, EventArgs e)
        {

        }

        private void legend1_Click(object sender, EventArgs e)
        {

        }

        private void groupBox4_Enter(object sender, EventArgs e)
        {

        }

        private void label_Advise_Click(object sender, EventArgs e)
        {

        }

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {

        }
    }
}

