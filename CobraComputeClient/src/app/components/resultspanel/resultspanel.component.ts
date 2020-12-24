import { Component, ViewEncapsulation, OnInit, Input, Output, EventEmitter } from '@angular/core';
import { Token } from '../../Token';
import { CobraDataService } from '../../cobra-data-service.service';
import * as L from 'leaflet';
import 'leaflet-choropleth';
import county_data from '../resultspanel/map_data/county_map.json';
import state_data from '../resultspanel/map_data/state_data.json';

@Component({
  selector: 'app-resultspanel',
  templateUrl: './resultspanel.component.html',
  styleUrls: ['../../../theme/styles.scss','./resultspanel.component.scss'],
  encapsulation: ViewEncapsulation.None
})

export class ResultspanelComponent implements OnInit {
  @Input() token: Token;

  constructor(private cobraDataService: CobraDataService) { }

  /* variables used to show and hide different results screens */
  public showNoResultsScreen = true;
  public showPendingResultsScreen = false;
  // public showResultsScreen = true;
  public showHeartbeat = false;
  // public showResultsPanelContent = false;

  /* variables related to state and county dropdowns for filtering */
  public state_clr_structure: any[] = [];
  public counties_for_state: any[] = [];
  public showStateName: boolean = false;
  public showCountyName: boolean = false;
  public countyFIPS: any;
  public countyName: any;

  /* variables that store data after running scenario */
  public items: any[] = null;
  public summary: any[] = null;

  /* variables used to show table data */
  public TotalHealthBenefitsValue_high = ''
  public TotalHealthBenefitsValue_low = '';
  public Mortality_low = '';
  public Mortality_high = '';
  public NonfatalHeartAttacks_low = '';
  public NonfatalHeartAttacks_high = '';
  public InfantMortality = '';
  public HospitalAdmitsAllRespiratory = '';
  public HospitalAdmitsCardiovascularexceptheartattacks = '';
  public AcuteBronchitis = '';
  public UpperRespiratorySymptoms = '';
  public LowerRespiratorySymptoms = '';
  public EmergencyRoomVisitsAsthma = '';
  public AsthmaExacerbation = '';
  public MinorRestrictedActivityDays = '';
  public WorkLossDays = '';
  public MortalityValue_low = '';
  public MortalityValue_high = '';
  public NonfatalHeartAttacksValue_low = '';
  public NonfatalHeartAttacksValue_high = '';
  public InfantMortalityValue = '';
  public HospitalAdmitsAllRespiratoryValue = '';
  public HospitalAdmitsCardiovascularexceptheartattacksValue = '';
  public AcuteBronchitisValue = '';
  public UpperRespiratorySymptomsValue = '';
  public LowerRespiratorySymptomsValue = '';
  public EmergencyRoomVisitsAsthmaValue = '';
  public AsthmaExacerbationValue = '';
  public MinorRestrictedActivityDaysValue = '';
  public WorkLossDaysValue = '';

  /* variables used as arguments in cobraDataService.getResults() */
  public filtervalue = "00";
  public discountRate = "3";

  //map parts
  private map;
  statesLayer;
  countyLayer;
  selectedMapLayer = 'C__Total_Health_Benefits_Low_Value';
  legend;
  mapLayerDisplayName = [
      {value: "BASE_FINAL_PM", name: "Baseline PM2.5 Concentrations", legendTitle: "PM2.5 concentration (&#181;g/m<sup>3</sup>)", units1: "", units2: "&#181;g/m<sup>3</sup>",popupStyle: 1, popupTextName: 'baseline PM2.5 concentration'},
      {value: "CTRL_FINAL_PM", name: "Scenario PM2.5 Concentrations", legendTitle: "PM2.5 concentration (&#181;g/m<sup>3</sup>)", units1: "", units2: "&#181;g/m<sup>3</sup>",popupStyle: 1, popupTextName: ''},
      {value: "DELTA_FINAL_PM", name: "Delta PM2.5 Concentrations", legendTitle: "PM2.5 concentration (&#181;g/m<sup>3</sup>)", units1: "", units2: "&#181;g/m<sup>3</sup>",popupStyle: 1, popupTextName: ''},
      {value: "C__Total_Health_Benefits_Low_Value", name: "Total Health Benefits ($, low estimate)", legendTitle: "Monetary value ($)", units1: "$", units2: "",popupStyle:2, popupTextName:''},
      {value: "C__Total_Health_Benefits_High_Value", name: "Total Health Benefits ($, high estimate)", legendTitle: "Monetary value ($)", units1: "$", units2: "",popupStyle:2, popupTextName:''},
      {value: "Acute_Bronchitis", name: "Acute Bronchitis (cases)", legendTitle: "Change in Incidence (cases)", units1: "", units2: "",popupStyle:3, popupTextName:''},
      {value: "C__Acute_Bronchitis", name: "Acute Bronchitis ($)", legendTitle: "Monetary value ($)", units1: "$", units2: "cases of",popupStyle:4, popupTextName:''},
      {value: "Asthma_Exacerbation_Cough", name: "Asthma Exacerbation, Cough (cases)", legendTitle: "Change in Incidence (cases)", units1: "", units2: "cases of",popupStyle:3, popupTextName:''},
      {value: "Asthma_Exacerbation_Shortness_of_Breath", name: "Asthma Exacerbation, Shortness of Breath (cases)", legendTitle: "Change in Incidence (cases)", units1: "", units2: "cases of",popupStyle:3, popupTextName:''},
      {value: "Asthma_Exacerbation_Wheeze", name: "Asthma Exacerbation, Wheeze (cases)", legendTitle: "Change in Incidence (cases)", units1: "", units2: "cases of",popupStyle:3, popupTextName:''},
      {value: "C__Asthma_Exacerbation", name: "Asthma Exacerbation ($)", legendTitle: "Monetary value ($)", units1: "$", units2: "",popupStyle:4, popupTextName:''},
      {value: "Emergency_Room_Visits_Asthma", name: "Emergency Room Visits, Asthma (cases)", legendTitle: "Change in Incidence (cases)", units1: "", units2: "cases of",popupStyle:3, popupTextName:''},
      {value: "C__Emergency_Room_Visits_Asthma", name: "Emergency Room Visits, Asthma ($)", legendTitle: "Monetary value ($)", units1: "$", units2: "",popupStyle:4, popupTextName:''},
      {value: "HA_All_Respiratory", name: "Hospital Admits, All Respiratory (cases)", legendTitle: "Change in Incidence (cases)", units1: "", units2: "cases of",popupStyle:3, popupTextName:''},
      {value: "HA_Asthma", name: "Hospital Admits, Asthma (cases)", legendTitle: "Change in Incidence (cases)", units1: "", units2: "cases of",popupStyle:3, popupTextName:''},
      {value: "HA_Chronic_Lung_Disease", name: "Hospital Admits, Chronic Lung Disease (cases)", legendTitle: "Change in Incidence (cases)", units1: "", units2: "cases of",popupStyle:3, popupTextName:''},
      {value: "C__Resp_Hosp_Adm", name: "Hospital Admits, All Respiratory ($)", legendTitle: "Monetary value ($)", units1: "$", units2: "",popupStyle:4, popupTextName:''},
      {value: "HA_All_Cardiovascular__less_Myocardial_Infarctions_", name: "Hospital Admits, Cardiovascular (except heart attacks) (cases)", legendTitle: "Change in Incidence (cases)", units1: "", units2: "cases of",popupStyle:3, popupTextName:''},
      {value: "C__CVD_Hosp_Adm", name: "$ Hospital Admits, Cardiovascular (except heart attacks) ($)", legendTitle: "Monetary value ($)", units1: "$", units2: "",popupStyle:4, popupTextName:''},
      {value: "Infant_Mortality", name: "Infant Mortality (cases)", legendTitle: "Change in Incidence (cases)", units1: "", units2: "cases of",popupStyle:3, popupTextName:''},
      {value: "C__Infant_Mortality", name: "Infant Mortality ($)", legendTitle: "Monetary value ($)", units1: "$", units2: "",popupStyle:4, popupTextName:''},
      {value: "Lower_Respiratory_Symptoms", name: "Lower Respiratory Symptoms (cases)", legendTitle: "Change in Incidence (cases)", units1: "", units2: "cases of",popupStyle:3, popupTextName:''},
      {value: "C__Lower_Respiratory_Symptoms", name: "Lower Respiratory Symptoms ($)", legendTitle: "Monetary value ($)", units1: "$", units2: "",popupStyle:4, popupTextName:''},
      {value: "Minor_Restricted_Activity_Days", name: "Minor Restricted Activity Days (cases)", legendTitle: "Change in Incidence (cases)", units1: "", units2: "cases of",popupStyle:3, popupTextName:''},
      {value: "C__Minor_Restricted_Activity_Days", name: "Minor Restricted Activity Days ($)", legendTitle: "Monetary value ($)", units1: "$", units2: "",popupStyle:4, popupTextName:''},
      {value: "Mortality_All_Cause__low_", name: "Mortality (cases, low estimate)", legendTitle: "Change in Incidence (cases)", units1: "", units2: "cases of",popupStyle:3, popupTextName:''},
      {value: "C__Mortality_All_Cause__low_", name: "Mortality ($, low estimate)", legendTitle: "Monetary value ($)", units1: "$", units2: "",popupStyle:4, popupTextName:''},
      {value: "Mortality_All_Cause__high_", name: "Mortality (cases, high estimate)", legendTitle: "Change in Incidence (cases)", units1: "", units2: "cases of",popupStyle:3, popupTextName:''},
      {value: "C__Mortality_All_Cause__high_", name: "Mortality ($, high estimate)", legendTitle: "Monetary value ($)", units1: "$", units2: "",popupStyle:4, popupTextName:''},
      {value: "Acute_Myocardial_Infarction_Nonfatal__low_", name: "Nonfatal Heart Attacks (cases, low estimate)", legendTitle: "Change in Incidence (cases)", units1: "", units2: "cases of",popupStyle:3, popupTextName:''},
      {value: "C__Acute_Myocardial_Infarction_Nonfatal__low_", name: "Nonfatal Heart Attacks ($, low estimate)", legendTitle: "Monetary value ($)", units1: "$", units2: "",popupStyle:4, popupTextName:''},
      {value: "Acute_Myocardial_Infarction_Nonfatal__high_", name: "Nonfatal Heart Attacks (cases, high estimate)", legendTitle: "Change in Incidence (cases)", units1: "", units2: "cases of",popupStyle:3, popupTextName:''},
      {value: "C__Acute_Myocardial_Infarction_Nonfatal__high_", name: "$ Nonfatal Heart Attacks ($, high estimate)", legendTitle: "Monetary value ($)", units1: "$", units2: "",popupStyle:4, popupTextName:''},
      {value: "Upper_Respiratory_Symptoms", name: "Upper Respiratory Symptoms (cases)", legendTitle: "Change in Incidence (cases)", units1: "", units2: "cases of",popupStyle:3, popupTextName:''},
      {value: "C__Upper_Respiratory_Symptoms", name: "$ Upper Respiratory Symptoms ($)", legendTitle: "Monetary value ($)", units1: "$", units2: "",popupStyle:4, popupTextName:''},
      {value: "Work_Loss_Days", name: "Work Loss Days (cases)", legendTitle: "Change in Incidence (cases)", units1: "", units2: "cases of",popupStyle:3, popupTextName:''},
      {value: "C__Work_Loss_Days", name: "Work Loss Days ($)", legendTitle: "Monetary value ($)", units1: "$", units2: "",popupStyle:4, popupTextName:''}
    ];

    stateAbbrev = [
      {name: "Alabama",abbrev: "AL"},
      {name: "Alaska",abbrev: "AK"},
      {name: "American Samoa",abbrev: "AS"},
      {name: "Arizona",abbrev: "AZ"},
      {name: "Arkansas",abbrev: "AR"},
      {name: "California",abbrev: "CA"},
      {name: "Colorado",abbrev: "CO"},
      {name: "Connecticut",abbrev: "CT"},
      {name: "Delaware",abbrev: "DE"},
      {name: "District Of Columbia",abbrev: "DC"},
      {name: "Federated States Of Micronesia",abbrev: "FM"},
      {name: "Florida",abbrev: "FL"},
      {name: "Georgia",abbrev: "GA"},
      {name: "Guam",abbrev: "GU"},
      {name: "Hawaii",abbrev: "HI"},
      {name: "Idaho",abbrev: "ID"},
      {name: "Illinois",abbrev: "IL"},
      {name: "Indiana",abbrev: "IN"},
      {name: "Iowa",abbrev: "IA"},
      {name: "Kansas",abbrev: "KS"},
      {name: "Kentucky",abbrev: "KY"},
      {name: "Louisiana",abbrev: "LA"},
      {name: "Maine",abbrev: "ME"},
      {name: "Marshall Islands",abbrev: "MH"},
      {name: "Maryland",abbrev: "MD"},
      {name: "Massachusetts",abbrev: "MA"},
      {name: "Michigan",abbrev: "MI"},
      {name: "Minnesota",abbrev: "MN"},
      {name: "Mississippi",abbrev: "MS"},
      {name: "Missouri",abbrev: "MO"},
      {name: "Montana",abbrev: "MT"},
      {name: "Nebraska",abbrev: "NE"},
      {name: "Nevada",abbrev: "NV"},
      {name: "New Hampshire",abbrev: "NH"},
      {name: "New Jersey",abbrev: "NJ"},
      {name: "New Mexico",abbrev: "NM"},
      {name: "New York",abbrev: "NY"},
      {name: "North Carolina",abbrev: "NC"},
      {name: "North Dakota",abbrev: "ND"},
      {name: "Northern Mariana Islands",abbrev: "MP"},
      {name: "Ohio",abbrev: "OH"},
      {name: "Oklahoma",abbrev: "OK"},
      {name: "Oregon",abbrev: "OR"},
      {name: "Palau",abbrev: "PW"},
      {name: "Pennsylvania",abbrev: "PA"},
      {name: "Puerto Rico",abbrev: "PR"},
      {name: "Rhode Island",abbrev: "RI"},
      {name: "South Carolina",abbrev: "SC"},
      {name: "South Dakota",abbrev: "SD"},
      {name: "Tennessee",abbrev: "TN"},
      {name: "Texas",abbrev: "TX"},
      {name: "Utah",abbrev: "UT"},
      {name: "Vermont",abbrev: "VT"},
      {name: "Virgin Islands",abbrev: "VI"},
      {name: "Virginia",abbrev: "VA"},
      {name: "Washington",abbrev: "WA"},
      {name: "West Virginia",abbrev: "WV"},
      {name: "Wisconsin",abbrev: "WI"},
      {name: "Wyoming",abbrev: "WY"}
  ]

  ngOnInit(): void {
    //build map
    this.map = L.map('map',{
      center: [37, -96],
      zoomDelta: 0.1,
      zoom: 3.8
    });

    function centerMap(){
      this.map.setView([37, -96],3.5);
    }

    var homeControl = L.Control.extend({
      options:{
        position: 'topleft'
      },
      
      onAdd: function(map){ 
        var homeButton = L.DomUtil.create('input', 'homeButton');
        homeButton.innerHTML = '<clr-icon shape="home" class="solid"></clr-icon>';
        homeButton.type = 'button';
        homeButton.setAttribute('class', 'leaflet-control leaflet-touch home-button leaflet-control-command');
        homeButton.onclick = function(){
          centerMap()
        };
        return homeButton;
      }
    });
    //this.map.addControl(homeControl);

    var OpenStreetMap_Mapnik = L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      maxZoom: 19,
      attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
    });

    this.statesLayer = L.geoJSON(state_data,{
      style: {
        color: '#000',
        weight: 1,
        fill: false
      }
    });

    this.map.addLayer(OpenStreetMap_Mapnik);
    this.map.addLayer(this.statesLayer);
  }

  // <--------------------------------------------- Receives data from emissionspanel ----------------------------------------->
  /* This function receives state_clr_structure from emissionspanel when the page is loaded. The emitter "emissionspanelToResultspanelEmitter" sends this data to resultspanle component. state_clr_structure is needed in results panel in order to create state and county dropdowns for filtering. */
  receiveStateClrStructure(data: any) {
    this.state_clr_structure = data;
  }
  // <------------------------------------------- Receives data from emissionspanel/End --------------------------------------->

  // <---------------------------- Shows pending screen when addNewComponent() is called in reviewpanel ----------------------->
  showPendingScreen() {
    this.showNoResultsScreen = false;
    this.showPendingResultsScreen = true;
  }
  // <-------------------------- Shows pending screen when addNewComponent() is called in reviewpanel/End --------------------->

  // <------------------------------ Shows pending screen when runScenario() is called in reviewpanel ------------------------->
  /* This function is called when Run Scenario button is clicked to show heartbeat animation and hide the content in the results panel. */
  showHeartbeatAnimation() {
    this.showPendingResultsScreen = false;
    var results_screen = document.getElementById("results-screen");
    results_screen.removeAttribute("hidden");
    var results_panel_content = document.getElementById("results_panel_content");
    results_panel_content.setAttribute("hidden", "true");
    this.showHeartbeat = true;
  }
  // <---------------------------- Shows pending screen when runScenario() is called in reviewpanel/End ----------------------->

  // <----------------------------------------------- Receives data from reviewpanel ------------------------------------------>
  /* This function receives data from reviewpanel when Run Scenario button is clicked, actually after click and after the update for all components in review panel is done, and calls getResults() to create results in the table. The emitter "reviewpanelToResultspanelEmitter" sends this data to resultspanle component. */
  receiveDiscountRateAndGetResults(dataFromReviewPanel: any) {
    this.discountRate = dataFromReviewPanel["discountRate"];
    if(this.map.hasLayer(this.countyLayer)){
      this.map.removeLayer(this.countyLayer);
    }
    this.getResults();
  }
  // <--------------------------------------------- Receives data from reviewpanel/End ---------------------------------------->

  // <---------------------------------- Removes filters and shows table data for all states ---------------------------------->
  showTableDataForAllStates() {
    this.TotalHealthBenefitsValue_high = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0, minimumFractionDigits: 0 }).format(this.summary["TotalHealthBenefitsValue_high"]);
    this.TotalHealthBenefitsValue_low = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0, minimumFractionDigits: 0 }).format(this.summary["TotalHealthBenefitsValue_low"]);
    this.Mortality_low = new Intl.NumberFormat('en-US', { style: 'decimal', maximumFractionDigits: 3, minimumFractionDigits: 3 }).format(this.summary["Mortality_All_Cause__low_"]);
    this.Mortality_high = new Intl.NumberFormat('en-US', { style: 'decimal', maximumFractionDigits: 3, minimumFractionDigits: 3 }).format(this.summary["Mortality_All_Cause__high_"]);
    this.NonfatalHeartAttacks_low = new Intl.NumberFormat('en-US', { style: 'decimal', maximumFractionDigits: 3, minimumFractionDigits: 3 }).format(this.summary["Acute_Myocardial_Infarction_Nonfatal__low_"]);
    this.NonfatalHeartAttacks_high = new Intl.NumberFormat('en-US', { style: 'decimal', maximumFractionDigits: 3, minimumFractionDigits: 3 }).format(this.summary["Acute_Myocardial_Infarction_Nonfatal__high_"]);
    this.InfantMortality = new Intl.NumberFormat('en-US', { style: 'decimal', maximumFractionDigits: 3, minimumFractionDigits: 3 }).format(this.summary["Infant_Mortality"]);
    this.HospitalAdmitsAllRespiratory = new Intl.NumberFormat('en-US', { style: 'decimal', maximumFractionDigits: 3, minimumFractionDigits: 3 }).format(this.summary["HA_All_Respiratory"] + this.summary["HA_Chronic_Lung_Disease"]);
    this.HospitalAdmitsCardiovascularexceptheartattacks = new Intl.NumberFormat('en-US', { style: 'decimal', maximumFractionDigits: 3, minimumFractionDigits: 3 }).format(this.summary["HA_All_Cardiovascular__less_Myocardial_Infarctions_"]);
    this.AcuteBronchitis = new Intl.NumberFormat('en-US', { style: 'decimal', maximumFractionDigits: 3, minimumFractionDigits: 3 }).format(this.summary["Acute_Bronchitis"]);
    this.UpperRespiratorySymptoms = new Intl.NumberFormat('en-US', { style: 'decimal', maximumFractionDigits: 3, minimumFractionDigits: 3 }).format(this.summary["Upper_Respiratory_Symptoms"]);
    this.LowerRespiratorySymptoms = new Intl.NumberFormat('en-US', { style: 'decimal', maximumFractionDigits: 3, minimumFractionDigits: 3 }).format(this.summary["Lower_Respiratory_Symptoms"]);
    this.EmergencyRoomVisitsAsthma = new Intl.NumberFormat('en-US', { style: 'decimal', maximumFractionDigits: 3, minimumFractionDigits: 3 }).format(this.summary["Emergency_Room_Visits_Asthma"]);
    this.AsthmaExacerbation = new Intl.NumberFormat('en-US', { style: 'decimal', maximumFractionDigits: 3, minimumFractionDigits: 3 }).format(this.summary["Asthma_Exacerbation_Cough"] +this.summary["Asthma_Exacerbation_Shortness_of_Breath"] + this.summary["Asthma_Exacerbation_Wheeze"]);
    this.MinorRestrictedActivityDays = new Intl.NumberFormat('en-US', { style: 'decimal', maximumFractionDigits: 3, minimumFractionDigits: 3 }).format(this.summary["Minor_Restricted_Activity_Days"]);
    this.WorkLossDays = new Intl.NumberFormat('en-US', { style: 'decimal', maximumFractionDigits: 3, minimumFractionDigits: 3 }).format(this.summary["Work_Loss_Days"]);
    this.MortalityValue_low = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0, minimumFractionDigits: 0 }).format(this.summary["C__Mortality_All_Cause__low_"]);
    this.MortalityValue_high = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0, minimumFractionDigits: 0 }).format(this.summary["C__Mortality_All_Cause__high_"]);
    this.NonfatalHeartAttacksValue_low = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0, minimumFractionDigits: 0 }).format(this.summary["C__Acute_Myocardial_Infarction_Nonfatal__low_"]);
    this.NonfatalHeartAttacksValue_high = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0, minimumFractionDigits: 0 }).format(this.summary["C__Acute_Myocardial_Infarction_Nonfatal__high_"]);
    this.InfantMortalityValue = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0, minimumFractionDigits: 0 }).format(this.summary["C__Infant_Mortality"]);
    this.HospitalAdmitsAllRespiratoryValue = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0, minimumFractionDigits: 0 }).format(this.summary["C__CVD_Hosp_Adm"]);
    this.HospitalAdmitsCardiovascularexceptheartattacksValue = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0, minimumFractionDigits: 0 }).format(this.summary["C__Resp_Hosp_Adm"]);
    this.AcuteBronchitisValue = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0, minimumFractionDigits: 0 }).format(this.summary["C__Acute_Bronchitis"]);
    this.UpperRespiratorySymptomsValue = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0, minimumFractionDigits: 0 }).format(this.summary["C__Upper_Respiratory_Symptoms"]);
    this.LowerRespiratorySymptomsValue = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0, minimumFractionDigits: 0 }).format(this.summary["C__Lower_Respiratory_Symptoms"]);
    this.EmergencyRoomVisitsAsthmaValue = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0, minimumFractionDigits: 0 }).format(this.summary["C__Emergency_Room_Visits_Asthma"]);
    this.AsthmaExacerbationValue = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0, minimumFractionDigits: 0 }).format(this.summary["C__Asthma_Exacerbation"]);
    this.MinorRestrictedActivityDaysValue = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0, minimumFractionDigits: 0 }).format(this.summary["C__Minor_Restricted_Activity_Days"]);
    this.WorkLossDaysValue = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0, minimumFractionDigits: 0 }).format(this.summary["C__Work_Loss_Days"]);
  }
  // <-------------------------------- Removes filters and shows table data for all states/End -------------------------------->

  // <-------------------------------------------- Filters data for state selection ------------------------------------------->
  filterDataForStateSelection() {
    var TotalHealthBenefitsValue_high_temp = 0;
    var TotalHealthBenefitsValue_low_temp = 0;
    var Mortality_low_temp = 0;
    var Mortality_high_temp = 0;
    var NonfatalHeartAttacks_low_temp = 0;
    var NonfatalHeartAttacks_high_temp = 0;
    var InfantMortality_temp = 0;
    var HospitalAdmitsAllRespiratory_temp = 0;
    var HospitalAdmitsCardiovascularexceptheartattacks_temp = 0;
    var AcuteBronchitis_temp = 0;
    var UpperRespiratorySymptoms_temp = 0;
    var LowerRespiratorySymptoms_temp = 0;
    var EmergencyRoomVisitsAsthma_temp = 0;
    var AsthmaExacerbation_temp = 0;
    var MinorRestrictedActivityDays_temp = 0;
    var WorkLossDays_temp = 0;
    var MortalityValue_low_temp = 0;
    var MortalityValue_high_temp = 0;
    var NonfatalHeartAttacksValue_low_temp = 0;
    var NonfatalHeartAttacksValue_high_temp = 0;
    var InfantMortalityValue_temp = 0;
    var HospitalAdmitsAllRespiratoryValue_temp = 0;
    var HospitalAdmitsCardiovascularexceptheartattacksValue_temp = 0;
    var AcuteBronchitisValue_temp = 0;
    var UpperRespiratorySymptomsValue_temp = 0;
    var LowerRespiratorySymptomsValue_temp = 0;
    var EmergencyRoomVisitsAsthmaValue_temp = 0;
    var AsthmaExacerbationValue_temp = 0;
    var MinorRestrictedActivityDaysValue_temp = 0;
    var WorkLossDaysValue_temp = 0;
    this.items.map(item =>
      {
        if(item.FIPS.substr(0,2) == this.filtervalue) {
          TotalHealthBenefitsValue_high_temp = TotalHealthBenefitsValue_high_temp + item["C__Total_Health_Benefits_High_Value"];
          TotalHealthBenefitsValue_low_temp = TotalHealthBenefitsValue_low_temp + item["C__Total_Health_Benefits_Low_Value"];
          Mortality_low_temp = Mortality_low_temp + item["Mortality_All_Cause__low_"];
          Mortality_high_temp = Mortality_high_temp + item["Mortality_All_Cause__high_"];
          NonfatalHeartAttacks_low_temp = NonfatalHeartAttacks_low_temp + item["Acute_Myocardial_Infarction_Nonfatal__low_"];
          NonfatalHeartAttacks_high_temp = NonfatalHeartAttacks_high_temp + item["Acute_Myocardial_Infarction_Nonfatal__high_"];
          InfantMortality_temp = InfantMortality_temp + item["Infant_Mortality"];
          HospitalAdmitsAllRespiratory_temp = HospitalAdmitsAllRespiratory_temp + (item["HA_All_Respiratory"] + item["HA_Chronic_Lung_Disease"]);
          HospitalAdmitsCardiovascularexceptheartattacks_temp = HospitalAdmitsCardiovascularexceptheartattacks_temp + item["HA_All_Cardiovascular__less_Myocardial_Infarctions_"];
          AcuteBronchitis_temp = AcuteBronchitis_temp + item["Acute_Bronchitis"];
          UpperRespiratorySymptoms_temp = UpperRespiratorySymptoms_temp + item["Upper_Respiratory_Symptoms"];
          LowerRespiratorySymptoms_temp = LowerRespiratorySymptoms_temp + item["Lower_Respiratory_Symptoms"];
          EmergencyRoomVisitsAsthma_temp = EmergencyRoomVisitsAsthma_temp + item["Emergency_Room_Visits_Asthma"];
          AsthmaExacerbation_temp = AsthmaExacerbation_temp + (item["Asthma_Exacerbation_Cough"] + item["Asthma_Exacerbation_Shortness_of_Breath"] + item["Asthma_Exacerbation_Wheeze"]);
          MinorRestrictedActivityDays_temp = MinorRestrictedActivityDays_temp + item["Minor_Restricted_Activity_Days"];
          WorkLossDays_temp = WorkLossDays_temp + item["Work_Loss_Days"];
          MortalityValue_low_temp = MortalityValue_low_temp + item["C__Mortality_All_Cause__low_"];
          MortalityValue_high_temp = MortalityValue_high_temp + item["C__Mortality_All_Cause__high_"];
          NonfatalHeartAttacksValue_low_temp = NonfatalHeartAttacksValue_low_temp + item["C__Acute_Myocardial_Infarction_Nonfatal__low_"];
          NonfatalHeartAttacksValue_high_temp = NonfatalHeartAttacksValue_high_temp + item["C__Acute_Myocardial_Infarction_Nonfatal__high_"];
          InfantMortalityValue_temp = InfantMortalityValue_temp + item["C__Infant_Mortality"];
          HospitalAdmitsAllRespiratoryValue_temp = HospitalAdmitsAllRespiratoryValue_temp + item["C__CVD_Hosp_Adm"];
          HospitalAdmitsCardiovascularexceptheartattacksValue_temp = HospitalAdmitsCardiovascularexceptheartattacksValue_temp + item["C__Resp_Hosp_Adm"];
          AcuteBronchitisValue_temp = AcuteBronchitisValue_temp + item["C__Acute_Bronchitis"];
          UpperRespiratorySymptomsValue_temp = UpperRespiratorySymptomsValue_temp + item["C__Upper_Respiratory_Symptoms"];
          LowerRespiratorySymptomsValue_temp = LowerRespiratorySymptomsValue_temp + item["C__Lower_Respiratory_Symptoms"];
          EmergencyRoomVisitsAsthmaValue_temp = EmergencyRoomVisitsAsthmaValue_temp + item["C__Emergency_Room_Visits_Asthma"];
          AsthmaExacerbationValue_temp = AsthmaExacerbationValue_temp + item["C__Asthma_Exacerbation"];
          MinorRestrictedActivityDaysValue_temp = MinorRestrictedActivityDaysValue_temp + item["C__Minor_Restricted_Activity_Days"];
          WorkLossDaysValue_temp = WorkLossDaysValue_temp + item["C__Work_Loss_Days"];
        }
      }
    );
    this.TotalHealthBenefitsValue_high = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0, minimumFractionDigits: 0 }).format(TotalHealthBenefitsValue_high_temp);
    this.TotalHealthBenefitsValue_low = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0, minimumFractionDigits: 0 }).format(TotalHealthBenefitsValue_low_temp);
    this.Mortality_low = new Intl.NumberFormat('en-US', { style: 'decimal', maximumFractionDigits: 3, minimumFractionDigits: 3 }).format(Mortality_low_temp);
    this.Mortality_high = new Intl.NumberFormat('en-US', { style: 'decimal', maximumFractionDigits: 3, minimumFractionDigits: 3 }).format(Mortality_high_temp);
    this.NonfatalHeartAttacks_low = new Intl.NumberFormat('en-US', { style: 'decimal', maximumFractionDigits: 3, minimumFractionDigits: 3 }).format(NonfatalHeartAttacks_low_temp);
    this.NonfatalHeartAttacks_high = new Intl.NumberFormat('en-US', { style: 'decimal', maximumFractionDigits: 3, minimumFractionDigits: 3 }).format(NonfatalHeartAttacks_high_temp);
    this.InfantMortality = new Intl.NumberFormat('en-US', { style: 'decimal', maximumFractionDigits: 3, minimumFractionDigits: 3 }).format(InfantMortality_temp);
    this.HospitalAdmitsAllRespiratory = new Intl.NumberFormat('en-US', { style: 'decimal', maximumFractionDigits: 3, minimumFractionDigits: 3 }).format(HospitalAdmitsAllRespiratory_temp);
    this.HospitalAdmitsCardiovascularexceptheartattacks = new Intl.NumberFormat('en-US', { style: 'decimal', maximumFractionDigits: 3, minimumFractionDigits: 3 }).format(HospitalAdmitsCardiovascularexceptheartattacks_temp);
    this.AcuteBronchitis = new Intl.NumberFormat('en-US', { style: 'decimal', maximumFractionDigits: 3, minimumFractionDigits: 3 }).format(AcuteBronchitis_temp);
    this.UpperRespiratorySymptoms = new Intl.NumberFormat('en-US', { style: 'decimal', maximumFractionDigits: 3, minimumFractionDigits: 3 }).format(UpperRespiratorySymptoms_temp);
    this.LowerRespiratorySymptoms = new Intl.NumberFormat('en-US', { style: 'decimal', maximumFractionDigits: 3, minimumFractionDigits: 3 }).format(LowerRespiratorySymptoms_temp);
    this.EmergencyRoomVisitsAsthma = new Intl.NumberFormat('en-US', { style: 'decimal', maximumFractionDigits: 3, minimumFractionDigits: 3 }).format(EmergencyRoomVisitsAsthma_temp);
    this.AsthmaExacerbation = new Intl.NumberFormat('en-US', { style: 'decimal', maximumFractionDigits: 3, minimumFractionDigits: 3 }).format(AsthmaExacerbation_temp);
    this.MinorRestrictedActivityDays = new Intl.NumberFormat('en-US', { style: 'decimal', maximumFractionDigits: 3, minimumFractionDigits: 3 }).format(MinorRestrictedActivityDays_temp);
    this.WorkLossDays = new Intl.NumberFormat('en-US', { style: 'decimal', maximumFractionDigits: 3, minimumFractionDigits: 3 }).format(WorkLossDays_temp);
    this.MortalityValue_low = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0, minimumFractionDigits: 0 }).format(MortalityValue_low_temp);
    this.MortalityValue_high = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0, minimumFractionDigits: 0 }).format(MortalityValue_high_temp);
    this.NonfatalHeartAttacksValue_low = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0, minimumFractionDigits: 0 }).format(NonfatalHeartAttacksValue_low_temp);
    this.NonfatalHeartAttacksValue_high = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0, minimumFractionDigits: 0 }).format(NonfatalHeartAttacksValue_high_temp);
    this.InfantMortalityValue = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0, minimumFractionDigits: 0 }).format(InfantMortalityValue_temp);
    this.HospitalAdmitsAllRespiratoryValue = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0, minimumFractionDigits: 0 }).format(HospitalAdmitsAllRespiratoryValue_temp);
    this.HospitalAdmitsCardiovascularexceptheartattacksValue = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0, minimumFractionDigits: 0 }).format(HospitalAdmitsCardiovascularexceptheartattacksValue_temp);
    this.AcuteBronchitisValue = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0, minimumFractionDigits: 0 }).format(AcuteBronchitisValue_temp);
    this.UpperRespiratorySymptomsValue = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0, minimumFractionDigits: 0 }).format(UpperRespiratorySymptomsValue_temp);
    this.LowerRespiratorySymptomsValue = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0, minimumFractionDigits: 0 }).format(LowerRespiratorySymptomsValue_temp);
    this.EmergencyRoomVisitsAsthmaValue = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0, minimumFractionDigits: 0 }).format(EmergencyRoomVisitsAsthmaValue_temp);
    this.AsthmaExacerbationValue = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0, minimumFractionDigits: 0 }).format(AsthmaExacerbationValue_temp);
    this.MinorRestrictedActivityDaysValue = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0, minimumFractionDigits: 0 }).format(MinorRestrictedActivityDaysValue_temp);
    this.WorkLossDaysValue = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0, minimumFractionDigits: 0 }).format(WorkLossDaysValue_temp);
  }
  // <-------------------------------------------- Filters data for state selection ------------------------------------------->

  // <--------------------------- Updates county dropdown once changing selection in state dropdown --------------------------->
  updateCountyDropDownAndFilterVal(index: any) {
    var counties_dd = document.getElementById("county");
    if (index == "") {
      this.counties_for_state = [];
      counties_dd.setAttribute("disabled", "");
      this.filtervalue = "00";
      this.showTableDataForAllStates();
    } else {
      this.counties_for_state = this.state_clr_structure[index].counties;
      counties_dd.removeAttribute("disabled");
      this.filtervalue = this.state_clr_structure[index].STFIPS;
      this.filterDataForStateSelection();
    }
    var countyValue = "";
    this.showHideStateCountyNameAndUpdateFilterVal(index, countyValue);
    var info_text_table = document.getElementById("info_text_table");
    info_text_table.setAttribute("hidden", "true");
  }
  // <------------------------- Updates county dropdown once changing selection in state dropdown/End ------------------------->

  // <------------------------------------------- Filters data for county selection ------------------------------------------->
  filterDataForCountySelection() {
    this.items.map(item =>
      {
        if(item.FIPS == this.filtervalue) {
          this.TotalHealthBenefitsValue_high = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0, minimumFractionDigits: 0 }).format(item["C__Total_Health_Benefits_High_Value"]);
          this.TotalHealthBenefitsValue_low = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0, minimumFractionDigits: 0 }).format(item["C__Total_Health_Benefits_Low_Value"]);
          this.Mortality_low = new Intl.NumberFormat('en-US', { style: 'decimal', maximumFractionDigits: 3, minimumFractionDigits: 3 }).format(item["Mortality_All_Cause__low_"]);
          this.Mortality_high = new Intl.NumberFormat('en-US', { style: 'decimal', maximumFractionDigits: 3, minimumFractionDigits: 3 }).format(item["Mortality_All_Cause__high_"]);
          this.NonfatalHeartAttacks_low = new Intl.NumberFormat('en-US', { style: 'decimal', maximumFractionDigits: 3, minimumFractionDigits: 3 }).format(item["Acute_Myocardial_Infarction_Nonfatal__low_"]);
          this.NonfatalHeartAttacks_high = new Intl.NumberFormat('en-US', { style: 'decimal', maximumFractionDigits: 3, minimumFractionDigits: 3 }).format(item["Acute_Myocardial_Infarction_Nonfatal__high_"]);
          this.InfantMortality = new Intl.NumberFormat('en-US', { style: 'decimal', maximumFractionDigits: 3, minimumFractionDigits: 3 }).format(item["Infant_Mortality"]);
          this.HospitalAdmitsAllRespiratory = new Intl.NumberFormat('en-US', { style: 'decimal', maximumFractionDigits: 3, minimumFractionDigits: 3 }).format(item["HA_All_Respiratory"] + item["HA_Chronic_Lung_Disease"]);
          this.HospitalAdmitsCardiovascularexceptheartattacks = new Intl.NumberFormat('en-US', { style: 'decimal', maximumFractionDigits: 3, minimumFractionDigits: 3 }).format(item["HA_All_Cardiovascular__less_Myocardial_Infarctions_"]);
          this.AcuteBronchitis = new Intl.NumberFormat('en-US', { style: 'decimal', maximumFractionDigits: 3, minimumFractionDigits: 3 }).format(item["Acute_Bronchitis"]);
          this.UpperRespiratorySymptoms = new Intl.NumberFormat('en-US', { style: 'decimal', maximumFractionDigits: 3, minimumFractionDigits: 3 }).format(item["Upper_Respiratory_Symptoms"]);
          this.LowerRespiratorySymptoms = new Intl.NumberFormat('en-US', { style: 'decimal', maximumFractionDigits: 3, minimumFractionDigits: 3 }).format(item["Lower_Respiratory_Symptoms"]);
          this.EmergencyRoomVisitsAsthma = new Intl.NumberFormat('en-US', { style: 'decimal', maximumFractionDigits: 3, minimumFractionDigits: 3 }).format(item["Emergency_Room_Visits_Asthma"]);
          this.AsthmaExacerbation = new Intl.NumberFormat('en-US', { style: 'decimal', maximumFractionDigits: 3, minimumFractionDigits: 3 }).format(item["Asthma_Exacerbation_Cough"] + item["Asthma_Exacerbation_Shortness_of_Breath"] + item["Asthma_Exacerbation_Wheeze"]);
          this.MinorRestrictedActivityDays = new Intl.NumberFormat('en-US', { style: 'decimal', maximumFractionDigits: 3, minimumFractionDigits: 3 }).format(item["Minor_Restricted_Activity_Days"]);
          this.WorkLossDays = new Intl.NumberFormat('en-US', { style: 'decimal', maximumFractionDigits: 3, minimumFractionDigits: 3 }).format(item["Work_Loss_Days"]);
          this.MortalityValue_low = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0, minimumFractionDigits: 0 }).format(item["C__Mortality_All_Cause__low_"]);
          this.MortalityValue_high = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0, minimumFractionDigits: 0 }).format(item["C__Mortality_All_Cause__high_"]);
          this.NonfatalHeartAttacksValue_low = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0, minimumFractionDigits: 0 }).format(item["C__Acute_Myocardial_Infarction_Nonfatal__low_"]);
          this.NonfatalHeartAttacksValue_high = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0, minimumFractionDigits: 0 }).format(item["C__Acute_Myocardial_Infarction_Nonfatal__high_"]);
          this.InfantMortalityValue = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0, minimumFractionDigits: 0 }).format(item["C__Infant_Mortality"]);
          this.HospitalAdmitsAllRespiratoryValue = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0, minimumFractionDigits: 0 }).format(item["C__CVD_Hosp_Adm"]);
          this.HospitalAdmitsCardiovascularexceptheartattacksValue = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0, minimumFractionDigits: 0 }).format(item["C__Resp_Hosp_Adm"]);
          this.AcuteBronchitisValue = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0, minimumFractionDigits: 0 }).format(item["C__Acute_Bronchitis"]);
          this.UpperRespiratorySymptomsValue = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0, minimumFractionDigits: 0 }).format(item["C__Upper_Respiratory_Symptoms"]);
          this.LowerRespiratorySymptomsValue = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0, minimumFractionDigits: 0 }).format(item["C__Lower_Respiratory_Symptoms"]);
          this.EmergencyRoomVisitsAsthmaValue = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0, minimumFractionDigits: 0 }).format(item["C__Emergency_Room_Visits_Asthma"]);
          this.AsthmaExacerbationValue = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0, minimumFractionDigits: 0 }).format(item["C__Asthma_Exacerbation"]);
          this.MinorRestrictedActivityDaysValue = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0, minimumFractionDigits: 0 }).format(item["C__Minor_Restricted_Activity_Days"]);
          this.WorkLossDaysValue = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0, minimumFractionDigits: 0 }).format(item["C__Work_Loss_Days"]);
        }
      }
    );
  }
  // <----------------------------------------- Filters data for county selection/End ----------------------------------------->

  // <----------------------------------------- Shows and hides state and county names ---------------------------------------->
  showHideStateCountyNameAndUpdateFilterVal(index: any, countyValue: any) {
    this.showStateName = false;
    this.showCountyName = false;
    if (index != "" && countyValue == "") {
      this.showStateName = true;
      this.filtervalue = this.state_clr_structure[index].STFIPS;
    }
    if (countyValue != "") {
      this.countyFIPS = countyValue.substr(countyValue.length - 5);
      this.countyName = countyValue.substr(0, countyValue.length - 5);
      this.showCountyName = true;
      this.filtervalue = this.countyFIPS;
      this.filterDataForCountySelection();
    }
  }
  // <--------------------------------------- Shows and hides state and county names/End -------------------------------------->

  // <-------------------------------------------------- getResults() function ------------------------------------------------>
  getResults(): void {
    var results_panel_content = document.getElementById("results_panel_content");
    results_panel_content.setAttribute("hidden", "true");
    this.showHeartbeat = true;

    this.cobraDataService.getResults(this.filtervalue, this.discountRate).subscribe(
      data => {
        this.items = data["Impacts"];
        this.summary = data["Summary"];
        this.TotalHealthBenefitsValue_high = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0, minimumFractionDigits: 0 }).format(this.summary["TotalHealthBenefitsValue_high"]);
        this.TotalHealthBenefitsValue_low = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0, minimumFractionDigits: 0 }).format(this.summary["TotalHealthBenefitsValue_low"]);
        this.Mortality_low = new Intl.NumberFormat('en-US', { style: 'decimal', maximumFractionDigits: 3, minimumFractionDigits: 3 }).format(this.summary["Mortality_All_Cause__low_"]);
        this.Mortality_high = new Intl.NumberFormat('en-US', { style: 'decimal', maximumFractionDigits: 3, minimumFractionDigits: 3 }).format(this.summary["Mortality_All_Cause__high_"]);
        this.NonfatalHeartAttacks_low = new Intl.NumberFormat('en-US', { style: 'decimal', maximumFractionDigits: 3, minimumFractionDigits: 3 }).format(this.summary["Acute_Myocardial_Infarction_Nonfatal__low_"]);
        this.NonfatalHeartAttacks_high = new Intl.NumberFormat('en-US', { style: 'decimal', maximumFractionDigits: 3, minimumFractionDigits: 3 }).format(this.summary["Acute_Myocardial_Infarction_Nonfatal__high_"]);
        this.InfantMortality = new Intl.NumberFormat('en-US', { style: 'decimal', maximumFractionDigits: 3, minimumFractionDigits: 3 }).format(this.summary["Infant_Mortality"]);
        this.HospitalAdmitsAllRespiratory = new Intl.NumberFormat('en-US', { style: 'decimal', maximumFractionDigits: 3, minimumFractionDigits: 3 }).format(this.summary["HA_All_Respiratory"] + this.summary["HA_Chronic_Lung_Disease"]);
        this.HospitalAdmitsCardiovascularexceptheartattacks = new Intl.NumberFormat('en-US', { style: 'decimal', maximumFractionDigits: 3, minimumFractionDigits: 3 }).format(this.summary["HA_All_Cardiovascular__less_Myocardial_Infarctions_"]);
        this.AcuteBronchitis = new Intl.NumberFormat('en-US', { style: 'decimal', maximumFractionDigits: 3, minimumFractionDigits: 3 }).format(this.summary["Acute_Bronchitis"]);
        this.UpperRespiratorySymptoms = new Intl.NumberFormat('en-US', { style: 'decimal', maximumFractionDigits: 3, minimumFractionDigits: 3 }).format(this.summary["Upper_Respiratory_Symptoms"]);
        this.LowerRespiratorySymptoms = new Intl.NumberFormat('en-US', { style: 'decimal', maximumFractionDigits: 3, minimumFractionDigits: 3 }).format(this.summary["Lower_Respiratory_Symptoms"]);
        this.EmergencyRoomVisitsAsthma = new Intl.NumberFormat('en-US', { style: 'decimal', maximumFractionDigits: 3, minimumFractionDigits: 3 }).format(this.summary["Emergency_Room_Visits_Asthma"]);
        this.AsthmaExacerbation = new Intl.NumberFormat('en-US', { style: 'decimal', maximumFractionDigits: 3, minimumFractionDigits: 3 }).format(this.summary["Asthma_Exacerbation_Cough"] +this.summary["Asthma_Exacerbation_Shortness_of_Breath"] + this.summary["Asthma_Exacerbation_Wheeze"]);
        this.MinorRestrictedActivityDays = new Intl.NumberFormat('en-US', { style: 'decimal', maximumFractionDigits: 3, minimumFractionDigits: 3 }).format(this.summary["Minor_Restricted_Activity_Days"]);
        this.WorkLossDays = new Intl.NumberFormat('en-US', { style: 'decimal', maximumFractionDigits: 3, minimumFractionDigits: 3 }).format(this.summary["Work_Loss_Days"]);
        this.MortalityValue_low = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0, minimumFractionDigits: 0 }).format(this.summary["C__Mortality_All_Cause__low_"]);
        this.MortalityValue_high = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0, minimumFractionDigits: 0 }).format(this.summary["C__Mortality_All_Cause__high_"]);
        this.NonfatalHeartAttacksValue_low = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0, minimumFractionDigits: 0 }).format(this.summary["C__Acute_Myocardial_Infarction_Nonfatal__low_"]);
        this.NonfatalHeartAttacksValue_high = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0, minimumFractionDigits: 0 }).format(this.summary["C__Acute_Myocardial_Infarction_Nonfatal__high_"]);
        this.InfantMortalityValue = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0, minimumFractionDigits: 0 }).format(this.summary["C__Infant_Mortality"]);
        this.HospitalAdmitsAllRespiratoryValue = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0, minimumFractionDigits: 0 }).format(this.summary["C__CVD_Hosp_Adm"]);
        this.HospitalAdmitsCardiovascularexceptheartattacksValue = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0, minimumFractionDigits: 0 }).format(this.summary["C__Resp_Hosp_Adm"]);
        this.AcuteBronchitisValue = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0, minimumFractionDigits: 0 }).format(this.summary["C__Acute_Bronchitis"]);
        this.UpperRespiratorySymptomsValue = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0, minimumFractionDigits: 0 }).format(this.summary["C__Upper_Respiratory_Symptoms"]);
        this.LowerRespiratorySymptomsValue = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0, minimumFractionDigits: 0 }).format(this.summary["C__Lower_Respiratory_Symptoms"]);
        this.EmergencyRoomVisitsAsthmaValue = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0, minimumFractionDigits: 0 }).format(this.summary["C__Emergency_Room_Visits_Asthma"]);
        this.AsthmaExacerbationValue = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0, minimumFractionDigits: 0 }).format(this.summary["C__Asthma_Exacerbation"]);
        this.MinorRestrictedActivityDaysValue = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0, minimumFractionDigits: 0 }).format(this.summary["C__Minor_Restricted_Activity_Days"]);
        this.WorkLossDaysValue = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0, minimumFractionDigits: 0 }).format(this.summary["C__Work_Loss_Days"]);

        //push results data into geojson for map
        //let feature = county_data.features;
        for (let index = 0; index < county_data.features.length; index++) {
          let county = this.items.find(x=> x.FIPS === county_data.features[index].properties.GEOID)
          if(county != undefined){
            county_data.features[index].properties.DATA = county;
          }     
        }
        //add geojson data layer to the map
        this.countyLayer = L.geoJSON(county_data);
        this.styleMap(this.selectedMapLayer);
        if(!this.map.hasLayer(this.countyLayer)){
          this.map.addLayer(this.countyLayer);
          this.map.addLayer(this.statesLayer);
        }
      },
      err => {
        console.error('An error occured retrieving results: ' + err);
        alert('An error occured retrieving results: ' + err);
      },
      () => {
        console.log('Retrieved results.');
        this.showHeartbeat = false;
        results_panel_content.removeAttribute("hidden");
      }
    );
  }
  // <------------------------------------------------ getResults() function/End ---------------------------------------------->

  // <-------------------------------------------------- updates results panel ------------------------------------------------>
  updateResultsPanelAfterAllComponentsRemoved() {
    var results_screen = document.getElementById("results-screen");
    results_screen.setAttribute("hidden", "true");
    if(this.map.hasLayer(this.countyLayer)){
      this.map.removeLayer(this.countyLayer);
    }
    this.showPendingResultsScreen = false;
    this.showNoResultsScreen = true;
  }
  // <------------------------------------------------ updates results panel/End ---------------------------------------------->

  //change map styling on dropdown selection
  styleMap(layerValue){
    let number = new Intl.NumberFormat('en-US', { style: 'decimal', maximumFractionDigits: 2, minimumFractionDigits: 2 });

    let dataValue = "C__Total_Health_Benefits_Low_Value";
    if(layerValue != undefined){
      dataValue = layerValue;
      this.selectedMapLayer = layerValue;
    }

    let mapTitle = this.mapLayerDisplayName.find( x => x.value == dataValue);
    let stateAbbrev = this.stateAbbrev;
    document.getElementById("mapTitle").innerHTML= "Displaying: " + mapTitle.name;
    this.map.removeLayer(this.countyLayer);
    this.countyLayer = L.choropleth(county_data,{
      valueProperty: function (feature){
        return feature.properties.DATA[dataValue]  
      },
      scale: ['#f6eff7','#bdc9e1','#67a9cf','#1c9099','#016c59'],
      steps: 5,
      mode: 'q', //q for quantile, e for equidistant, k for k-means, l for logarithmic
      style: {
        color: '#808080',
        weight: 1,
        opacity: 1,
        fillOpacity: 0.7
      }
    }).
    bindPopup(function (layer){
      let state = stateAbbrev.find( x => x.name == layer.feature.properties.DATA.STATE);
      let rateChange = 'avoided'
      if(layer.feature.properties.DATA[dataValue] > 0){
        rateChange = 'increased'
      }
      switch(mapTitle.popupStyle){
        case 1: return 'The ' + mapTitle.name + ' in ' + layer.feature.properties.NAME + ', ' + state.abbrev + ' is ' + mapTitle.units1 + number.format(layer.feature.properties.DATA[dataValue]) + ' ' + mapTitle.units2;
        case 2: return 'The ' + mapTitle.name + ' in ' + layer.feature.properties.NAME + ', ' + state.abbrev + ' are ' + mapTitle.units1 + number.format(layer.feature.properties.DATA[dataValue]);
        case 3: return layer.feature.properties.NAME + ', ' + state.abbrev + ' ' + rateChange + ' ' + number.format(layer.feature.properties.DATA[dataValue]) + ' ' + mapTitle.units2 + ' ' + mapTitle.name;
        case 4: return 'The monetary value of the change in ' + mapTitle.name + ' in ' + layer.feature.properties.NAME + ', ' + state.abbrev + ' are ' + mapTitle.units1 + number.format(layer.feature.properties.DATA[dataValue]);
      }
    });
    if(this.legend != undefined){
      this.map.removeControl(this.legend);
    }
    this.legend = L.control({ position: 'bottomleft' })
    let countyLayer = this.countyLayer;
    this.legend.onAdd = function(map){
      var div = L.DomUtil.create('div', 'legend');
      var limits = countyLayer.options.limits;
      var colors = countyLayer.options.colors;
      var labels = [];

      // Add min & max
      div.innerHTML = '<div class="labels"><p id="legendTitle">'+mapTitle.legendTitle+'</p>';
      
      limits.forEach(function (limit, index) {
        labels.push('<li style="background-color: ' + colors[index] + '"></li>')
      })
  
      div.innerHTML += '<ul>' + labels.join('') + '</ul><div class="min">' + mapTitle.units1 + number.format(limits[0]) + '</div> \
        <div class="max">' + mapTitle.units1 + number.format(limits[limits.length - 1]) + '</div></div>'
      return div;
    };
    this.legend.addTo(this.map)
    this.map.addLayer(this.countyLayer);
    this.statesLayer.bringToFront();
  }

}
