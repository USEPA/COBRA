import {
  Component,
  ViewEncapsulation,
  OnInit,
  AfterViewInit,
  Input,
  Output,
  EventEmitter,
} from '@angular/core';

import L from 'leaflet';
import chroma from 'chroma-js';
//import * as turf from '@turf/turf';
import 'leaflet-choropleth';
import 'leaflet.pattern';
import { featureLayer } from 'esri-leaflet';
import 'leaflet.markercluster';
import 'leaflet.markercluster/dist/MarkerCluster.Default.css';
import 'leaflet.markercluster/dist/MarkerCluster.css';
import 'leaflet.vectorgrid';

import { saveAs } from 'file-saver';

import { Token } from '../../Token';
import { CobraDataService } from '../../cobra-data-service.service';

import county_data from '../../../assets/map_data/county_map.json';
const iraPatternUrl = '.../../../assets/images/diagonal.png';

@Component({
  selector: 'app-resultspanel',
  templateUrl: './resultspanel.component.html',
  styleUrls: ['./resultspanel.component.scss'],
  encapsulation: ViewEncapsulation.None,
})
export class ResultspanelComponent implements OnInit, AfterViewInit {
  @Input() token: Token;
  @Output() resultspanelToEmissionspanelBuildNewScenarioEmitter =
    new EventEmitter<any>();
  @Output() resultspanelToReviewpanelBuildNewScenarioEmitter =
    new EventEmitter<any>();
  @Output() resultspanelToReviewpanelRetrievedResultsEmitter =
    new EventEmitter<any>();
  @Input() populationInfo: {
    [dest_tract: string]: {
      population: number;
      FIPS: string;
      CJEST: number;
      IRA_fraction: number;
    };
  } = {}; //dict keyed by dest_tract
  currentZoom: number = 0;

  constructor(private cobraDataService: CobraDataService) {}

  /* variables used to show and hide different results screens */
  public showNoResultsScreen = true;
  public showPendingResultsScreen = false;
  // public showResultsScreen = true;
  public showHeartbeat = false;
  // public showResultsPanelContent = false;

  /* variable to show and hide build new scenario confirmation modal */
  public showBuildNewConfirmationModal: boolean = false;

  public stateGeojson: any = null;
  public tract_data: any = null;
  public full_tract_data: any = null;
  public full_tract_data_complex: any = null;
  public tractsLayer: any = null;
  public IRAlayer: any = null;
  public CJESTlayer: any = null;
  isFirstVisit: boolean = false;
  globalChoroplethLimits: number[] = []; // Stores breakpoints for the choropleth
  globalChoroplethTractLimits: number[] = []; // Stores breakpoints for the choropleth
  globalChoroplethColors: string[] = [
    '#f6eff7',
    '#bdc9e1',
    '#67a9cf',
    '#1c9099',
    '#016c59',
  ];
  public allTribes: string[];
  public filteredTribes: string[];
  public tribeSearchInput: string = '';
  selectedTribe: string = '';
  showTribeDropdown: boolean = false;
  public groupedByFIPS: any = {};

  setShowReportPreview(value: boolean) {
    this.showReportPreview = value;
  }

  /* variables related to state and county dropdowns for filtering */
  public state_clr_structure: any[] = [];
  public counties_for_state: any[] = [];
  public countyFIPS: any;
  public countyName: any;
  public selectedTableState: string = 'all state';
  public selectedStateIndex: any = '';
  public selectedCountyIndex: any = '';
  public tableStates: any = {
    'all state': 'All Contiguous U.S. States',
    'selected state': '',
    'selected county': '',
  };
  public showBreakdown = {
    mortality: false,
    asthma_onset: false,
    asthma_symptoms: false,
    hay_fever: false,
    all_resp: false,
    er_resp: false,
  };

  /* variables that store data after running scenario */
  public items: any[] = null;
  public summary: any[] = null;

  /* variables used to show table data */
  public TotalHealthBenefitsValue_high = 0;
  public TotalHealthBenefitsValue_low = 0;

  public TotalPM_high = 0;
  public TotalPM_low = 0;
  public TotalO3 = 0;

  public Mortality_low = 0;
  public MortalityValue_low = 0;

  public MortalityValue_high = 0;
  public Mortality_high = 0;

  public PM_Mortality_low = 0;
  public PM_Mortality_high = 0;
  public PM_MortalityValue_low = 0;
  public PM_MortalityValue_high = 0;

  public O3_Mortality_long = 0;
  public O3_Mortality_short = 0;
  public O3_MortalityValue_long = 0;
  public O3_MortalityValue_short = 0;

  public NonfatalHeartAttacks = 0;
  public NonfatalHeartAttacksValue = 0;

  public InfantMortality = 0;
  public InfantMortalityValue = 0;

  public HospitalAdmitsAllRespiratory = 0;
  public HospitalAdmitsAllRespiratoryValue = 0;

  public PMHospitalAdmitsAllRespiratory = 0;
  public PMHospitalAdmitsAllRespiratoryValue = 0;

  public O3HospitalAdmitsAllRespiratory = 0;
  public O3HospitalAdmitsAllRespiratoryValue = 0;

  public EmergencyRoomVisitsAsthma = 0;
  public EmergencyRoomVisitsAsthmaValue = 0;

  public MinorRestrictedActivityDays = 0;
  public MinorRestrictedActivityDaysValue = 0;

  public HospitalAdmitsAlzheimersDisease = 0;
  public HospitalAdmitsAlzheimersDiseaseValue = 0;

  public HospitalAdmitsParkinsonsDisease = 0;
  public HospitalAdmitsParkinsonsDiseaseValue = 0;

  public IncidenceStroke = 0;
  public IncidenceStrokeValue = 0;

  public IncidenceOutOfHospitalCardiacArrest = 0;
  public IncidenceOutOfHospitalCardiacArrestValue = 0;

  public IncidenceAsthma = 0;
  public IncidenceAsthmaValue = 0;

  public PMIncidenceAsthma = 0;
  public PMIncidenceAsthmaValue = 0;
  public O3IncidenceAsthma = 0;
  public O3IncidenceAsthmaValue = 0;

  public AsthmaSymptoms = 0;
  public AsthmaSymptomsValue = 0;
  public AlbuterolUse = 0;
  public AlbuterolUseValue = 0;
  public Cough = 0;
  public CoughValue = 0;
  public ChestTightness = 0;
  public ChestTightnessValue = 0;
  public ShortnessOfBreath = 0;
  public ShortnessOfBreathValue = 0;
  public Wheeze = 0;
  public WheezeValue = 0;

  public IncidenceHayFeverRhinitis = 0;
  public IncidenceHayFeverRhinitisValue = 0;
  public PMIncidenceHayFeverRhinitis = 0;
  public PMIncidenceHayFeverRhinitisValue = 0;
  public O3IncidenceHayFeverRhinitis = 0;
  public O3IncidenceHayFeverRhinitisValue = 0;

  public HA_HCCPV_Disease = 0;
  public HA_HCCPV_DiseaseValue = 0;

  public IncidenceLungCancer = 0;
  public IncidenceLungCancerValue = 0;

  public ERVisitsAllCardiacOutcomes = 0;
  public ERVisitsAllCardiacOutcomesValue = 0;

  public ERVisitsAllRespiratory = 0;
  public ERVisitsAllRespiratoryValue = 0;
  public PMERVisitsAllRespiratory = 0;
  public PMERVisitsAllRespiratoryValue = 0;
  public O3ERVisitsAllRespiratory = 0;
  public O3ERVisitsAllRespiratoryValue = 0;

  public SchoolLossDays = 0;
  public SchoolLossDaysValue = 0;

  public WorkLossDays = 0;
  public WorkLossDaysValue = 0;

  /* variables used as arguments in cobraDataService.getResults() */
  public filtervalue = '00';
  public discountRate = '2';

  /* variables to show and hide Exporting status for CSV exports */
  public showAllResultsBtn = true;
  public showCurrentViewBtn = true;
  public showReportPreview = false;
  public itemLookup = {};

  //map parts
  private map;
  statesLayer;
  countyLayer;
  selectedMapLayer = 'C__Total_Health_Benefits_Low_Value';
  legend;
  mapLayerDisplayName = [
    {
      value: 'BASE_FINAL_PM',
      name: 'Baseline PM\u2082.\u2085  Concentrations',
      legendTitle: 'PM<sub>2.5</sub> concentration (&#181;g/m<sup>3</sup>)',
      units1: '',
      units2: '&#181;g/m<sup>3</sup>',
      popupStyle: 1,
      popupTextName: 'Baseline PM<sub>2.5</sub> concentrations',
    },
    {
      value: 'CTRL_FINAL_PM',
      name: 'Scenario PM\u2082.\u2085 Concentrations',
      legendTitle: 'PM<sub>2.5</sub> concentration (&#181;g/m<sup>3</sup>)',
      units1: '',
      units2: '&#181;g/m<sup>3</sup>',
      popupStyle: 1,
      popupTextName: 'Scenario PM<sub>2.5</sub> concentrations',
    },
    {
      value: 'DELTA_FINAL_PM',
      name: 'Delta PM\u2082.\u2085  Concentrations',
      legendTitle: 'PM<sub>2.5</sub> concentration (&#181;g/m<sup>3</sup>)',
      units1: '',
      units2: '&#181;g/m<sup>3</sup>',
      popupStyle: 1,
      popupTextName: 'Delta PM<sub>2.5</sub> concentrations',
    },
    {
      value: 'BASE_FINAL_O3',
      name: 'Baseline O3 Concentrations',
      legendTitle: 'O<sub>3</sub> concentration (&#181;g/m<sup>3</sup>)',
      units1: '',
      units2: '&#181;g/m<sup>3</sup>',
      popupStyle: 1,
      popupTextName: 'Baseline O<sub>3</sub> concentrations',
    },
    {
      value: 'CTRL_FINAL_O3',
      name: 'Scenario O3 Concentrations',
      legendTitle: 'O<sub>3</sub> concentration (&#181;g/m<sup>3</sup>)',
      units1: '',
      units2: '&#181;g/m<sup>3</sup>',
      popupStyle: 1,
      popupTextName: 'Scenario O<sub>3</sub> concentrations',
    },
    {
      value: 'DELTA_FINAL_O3',
      name: 'Delta O3 Concentrations',
      legendTitle: 'O<sub>3</sub> concentration (&#181;g/m<sup>3</sup>)',
      units1: '',
      units2: '&#181;g/m<sup>3</sup>',
      popupStyle: 1,
      popupTextName: 'Delta O<sub>3</sub> concentrations',
    },
    {
      value: 'C__Total_Health_Benefits_Low_Value',
      name: 'Total Health Benefits ($, low estimate)',
      legendTitle: 'Monetary value ($)',
      units1: '$',
      units2: '',
      popupStyle: 2,
      popupTextName: 'Total Health Benefits',
    },
    {
      value: 'C__Total_Health_Benefits_High_Value',
      name: 'Total Health Benefits ($, high estimate)',
      legendTitle: 'Monetary value ($)',
      units1: '$',
      units2: '',
      popupStyle: 2,
      popupTextName: 'Total Health Benefits',
    },
    {
      value: 'ER_Visits_Asthma',
      name: 'Emergency Room Visits, Asthma (cases)',
      legendTitle: 'Change in Incidence (cases)',
      units1: '',
      units2: 'cases of',
      popupStyle: 3,
      popupTextName: 'Emergency Room Visits, Asthma',
    },
    {
      value: 'C__ER_Visits_Asthma',
      name: 'Emergency Room Visits, Asthma ($)',
      legendTitle: 'Monetary value ($)',
      units1: '$',
      units2: '',
      popupStyle: 4,
      popupTextName: 'Emergency Room Visits, Asthma',
    },
    {
      value: 'HA_All_Respiratory',
      name: 'Hospital Admits, All Respiratory (cases)',
      legendTitle: 'Change in Incidence (cases)',
      units1: '',
      units2: 'cases of',
      popupStyle: 3,
      popupTextName: 'Hospital Admits, All Respiratory',
    },
    {
      value: 'C__HA_All_Respiratory',
      name: 'Hospital Admits, All Respiratory ($)',
      legendTitle: 'Monetary value ($)',
      units1: '$',
      units2: '',
      popupStyle: 4,
      popupTextName: 'Hospital Admits, All Respiratory',
    },
    {
      value: 'Infant_Mortality',
      name: 'Infant Mortality (cases)',
      legendTitle: 'Change in Incidence (cases)',
      units1: '',
      units2: 'cases of',
      popupStyle: 3,
      popupTextName: 'Infant Mortality',
    },
    {
      value: 'C__Infant_Mortality',
      name: 'Infant Mortality ($)',
      legendTitle: 'Monetary value ($)',
      units1: '$',
      units2: '',
      popupStyle: 4,
      popupTextName: 'Infant Mortality',
    },
    {
      value: 'Minor_Restricted_Activity_Days',
      name: 'Minor Restricted Activity Days (cases)',
      legendTitle: 'Change in Incidence (cases)',
      units1: '',
      units2: 'cases of',
      popupStyle: 3,
      popupTextName: 'Minor Restricted Activity Days',
    },
    {
      value: 'C__Minor_Restricted_Activity_Days',
      name: 'Minor Restricted Activity Days ($)',
      legendTitle: 'Monetary value ($)',
      units1: '$',
      units2: '',
      popupStyle: 4,
      popupTextName: 'Minor Restricted Activity Days',
    },
    {
      value: 'Mortality_All_Cause__low_',
      name: 'Mortality (cases, low estimate)',
      legendTitle: 'Change in Incidence (cases)',
      units1: '',
      units2: 'cases of',
      popupStyle: 3,
      popupTextName: 'Mortality',
    },
    {
      value: 'C__Mortality_All_Cause__low_',
      name: 'Mortality ($, low estimate)',
      legendTitle: 'Monetary value ($)',
      units1: '$',
      units2: '',
      popupStyle: 4,
      popupTextName: 'Mortality',
    },
    {
      value: 'Mortality_All_Cause__high_',
      name: 'Mortality (cases, high estimate)',
      legendTitle: 'Change in Incidence (cases)',
      units1: '',
      units2: 'cases of',
      popupStyle: 3,
      popupTextName: 'Mortality',
    },
    {
      value: 'C__Mortality_All_Cause__high_',
      name: 'Mortality ($, high estimate)',
      legendTitle: 'Monetary value ($)',
      units1: '$',
      units2: '',
      popupStyle: 4,
      popupTextName: 'Mortality',
    },

    {
      value: 'Acute_Myocardial_Infarction_Nonfatal',
      name: 'Nonfatal Heart Attacks (cases)',
      legendTitle: 'Change in Incidence (cases)',
      units1: '',
      units2: 'cases of',
      popupStyle: 3,
      popupTextName: 'Nonfatal Heart Attacks',
    },
    {
      value: 'C__Acute_Myocardial_Infarction_Nonfatal',
      name: 'Nonfatal Heart Attacks ($)',
      legendTitle: 'Monetary value ($)',
      units1: '$',
      units2: '',
      popupStyle: 4,
      popupTextName: 'Nonfatal Heart Attacks',
    },
    {
      value: 'HA_Cardio_Cerebro_and_Peripheral_Vascular_Disease',
      name: 'Hospital Admits, Cardio-Cerebro/Peripheral Vascular Disease (cases)',
      legendTitle: 'Change in Incidence (cases)',
      units1: '',
      units2: 'cases of',
      popupStyle: 3,
      popupTextName:
        'Hospital Admits, Cardio-Cerebro/Peripheral Vascular Disease',
    },
    {
      value: 'C__HA_Cardio_Cerebro_and_Peripheral_Vascular_Disease',
      name: 'Hospital Admits, Cardio-Cerebro/Peripheral Vascular Disease ($)',
      legendTitle: 'Monetary value ($)',
      units1: '$',
      units2: '',
      popupStyle: 4,
      popupTextName:
        'Hospital Admits, Cardio-Cerebro/Peripheral Vascular Disease',
    },
    {
      value: 'HA_Alzheimers_Disease',
      name: 'Hospital Admits, Alzheimers Disease (cases)',
      legendTitle: 'Change in Incidence (cases)',
      units1: '',
      units2: 'cases of',
      popupStyle: 3,
      popupTextName: 'Hospital Admits, Alzheimers Disease',
    },
    {
      value: 'C__HA_Alzheimers_Disease',
      name: 'Hospital Admits, Alzheimers Disease ($)',
      legendTitle: 'Monetary value ($)',
      units1: '$',
      units2: '',
      popupStyle: 4,
      popupTextName: 'Hospital Admits, Alzheimers Disease',
    },
    {
      value: 'HA_Parkinsons_Disease',
      name: 'Hospital Admits, Parkinsons Disease (cases)',
      legendTitle: 'Change in Incidence (cases)',
      units1: '',
      units2: 'cases of',
      popupStyle: 3,
      popupTextName: 'Hospital Admits, Parkinsons Disease',
    },
    {
      value: 'C__HA_Parkinsons_Disease',
      name: 'Hospital Admits, Parkinsons Disease ($)',
      legendTitle: 'Monetary value ($)',
      units1: '$',
      units2: '',
      popupStyle: 4,
      popupTextName: 'Hospital Admits, Parkinsons Disease',
    },
    {
      value: 'Incidence_Stroke',
      name: 'Stroke (cases)',
      legendTitle: 'Change in Incidence (cases)',
      units1: '',
      units2: 'cases of',
      popupStyle: 3,
      popupTextName: 'Stroke',
    },
    {
      value: 'C__Incidence_Stroke',
      name: 'Stroke ($)',
      legendTitle: 'Monetary value ($)',
      units1: '$',
      units2: '',
      popupStyle: 4,
      popupTextName: 'Stroke',
    },
    {
      value: 'Incidence_Lung_Cancer',
      name: 'Lung Cancer (cases)',
      legendTitle: 'Change in Incidence (cases)',
      units1: '',
      units2: 'cases of',
      popupStyle: 3,
      popupTextName: 'Lung Cancer',
    },
    {
      value: 'C__Incidence_Lung_Cancer',
      name: 'Lung Cancer ($)',
      legendTitle: 'Monetary value ($)',
      units1: '$',
      units2: '',
      popupStyle: 4,
      popupTextName: 'Lung Cancer',
    },
    {
      value: 'Incidence_Out_of_Hospital_Cardiac_Arrest',
      name: 'Cardiac Arrest, Out of Hospital (cases)',
      legendTitle: 'Change in Incidence (cases)',
      units1: '',
      units2: 'cases of',
      popupStyle: 3,
      popupTextName: 'Cardiac Arrest, Out of Hospital',
    },
    {
      value: 'C__Incidence_Out_of_Hospital_Cardiac_Arrest',
      name: 'Cardiac Arrest, Out of Hospital ($)',
      legendTitle: 'Monetary value ($)',
      units1: '$',
      units2: '',
      popupStyle: 4,
      popupTextName: 'Cardiac Arrest, Out of Hospital',
    },
    {
      value: 'Incidence_Asthma',
      name: 'Asthma (cases)',
      legendTitle: 'Change in Incidence (cases)',
      units1: '',
      units2: 'cases of',
      popupStyle: 3,
      popupTextName: 'Asthma',
    },
    {
      value: 'C__Incidence_Asthma',
      name: 'Asthma ($)',
      legendTitle: 'Monetary value ($)',
      units1: '$',
      units2: '',
      popupStyle: 4,
      popupTextName: 'Asthma',
    },
    {
      value: 'Asthma_Symptoms',
      name: 'Asthma Symptoms (cases)',
      legendTitle: 'Change in Incidence (cases)',
      units1: '',
      units2: 'cases of',
      popupStyle: 3,
      popupTextName: 'Asthma Symptoms',
    },
    {
      value: 'C__Asthma_Symptoms',
      name: 'Asthma Symptoms ($)',
      legendTitle: 'Monetary value ($)',
      units1: '$',
      units2: '',
      popupStyle: 4,
      popupTextName: 'Asthma Symptoms',
    },
    {
      value: 'Incidence_Hay_Fever_Rhinitis',
      name: 'Hay Fever/Rhinitis (cases)',
      legendTitle: 'Change in Incidence (cases)',
      units1: '',
      units2: 'cases of',
      popupStyle: 3,
      popupTextName: 'Hay Fever/Rhinitis',
    },
    {
      value: 'C__Incidence_Hay_Fever_Rhinitis',
      name: 'Hay Fever/Rhinitis ($)',
      legendTitle: 'Monetary value ($)',
      units1: '$',
      units2: '',
      popupStyle: 4,
      popupTextName: 'Hay Fever/Rhinitis',
    },
    {
      value: 'ER_visits_All_Cardiac_Outcomes',
      name: 'Emergency Room Visits, All Cardiac (cases)',
      legendTitle: 'Change in Incidence (cases)',
      units1: '',
      units2: 'cases of',
      popupStyle: 3,
      popupTextName: 'Emergency Room Visits, All Cardiac',
    },
    {
      value: 'C__ER_visits_All_Cardiac_Outcomes',
      name: 'Emergency Room Visits, All Cardiac ($)',
      legendTitle: 'Monetary value ($)',
      units1: '$',
      units2: '',
      popupStyle: 4,
      popupTextName: 'Emergency Room Visits, All Cardiac',
    },
    {
      value: 'ER_visits_respiratory',
      name: 'Emergency Room Visits, Respiratory (cases)',
      legendTitle: 'Change in Incidence (cases)',
      units1: '',
      units2: 'cases of',
      popupStyle: 3,
      popupTextName: 'Emergency Room Visits, Respiratory',
    },
    {
      value: 'C__ER_visits_respiratory',
      name: 'Emergency Room Visits, Respiratory ($)',
      legendTitle: 'Monetary value ($)',
      units1: '$',
      units2: '',
      popupStyle: 4,
      popupTextName: 'Emergency Room Visits, Respiratory',
    },
    {
      value: 'School_Loss_Days',
      name: 'School Loss Days (cases)',
      legendTitle: 'Change in Incidence (cases)',
      units1: '',
      units2: 'cases of',
      popupStyle: 3,
      popupTextName: 'School Loss Days',
    },
    {
      value: 'C__School_Loss_Days',
      name: 'School Loss Days ($)',
      legendTitle: 'Monetary value ($)',
      units1: '$',
      units2: '',
      popupStyle: 4,
      popupTextName: 'School Loss Days',
    },
    {
      value: 'Work_Loss_Days',
      name: 'Work Loss Days (cases)',
      legendTitle: 'Change in Incidence (cases)',
      units1: '',
      units2: 'cases of',
      popupStyle: 3,
      popupTextName: 'Work Loss Days',
    },
    {
      value: 'C__Work_Loss_Days',
      name: 'Work Loss Days ($)',
      legendTitle: 'Monetary value ($)',
      units1: '$',
      units2: '',
      popupStyle: 4,
      popupTextName: 'Work Loss Days',
    },
  ];

  stateAbbrev = [
    { name: 'Alabama', abbrev: 'AL' },
    { name: 'Alaska', abbrev: 'AK' },
    { name: 'American Samoa', abbrev: 'AS' },
    { name: 'Arizona', abbrev: 'AZ' },
    { name: 'Arkansas', abbrev: 'AR' },
    { name: 'California', abbrev: 'CA' },
    { name: 'Colorado', abbrev: 'CO' },
    { name: 'Connecticut', abbrev: 'CT' },
    { name: 'Delaware', abbrev: 'DE' },
    { name: 'District Of Columbia', abbrev: 'DC' },
    { name: 'Federated States Of Micronesia', abbrev: 'FM' },
    { name: 'Florida', abbrev: 'FL' },
    { name: 'Georgia', abbrev: 'GA' },
    { name: 'Guam', abbrev: 'GU' },
    { name: 'Hawaii', abbrev: 'HI' },
    { name: 'Idaho', abbrev: 'ID' },
    { name: 'Illinois', abbrev: 'IL' },
    { name: 'Indiana', abbrev: 'IN' },
    { name: 'Iowa', abbrev: 'IA' },
    { name: 'Kansas', abbrev: 'KS' },
    { name: 'Kentucky', abbrev: 'KY' },
    { name: 'Louisiana', abbrev: 'LA' },
    { name: 'Maine', abbrev: 'ME' },
    { name: 'Marshall Islands', abbrev: 'MH' },
    { name: 'Maryland', abbrev: 'MD' },
    { name: 'Massachusetts', abbrev: 'MA' },
    { name: 'Michigan', abbrev: 'MI' },
    { name: 'Minnesota', abbrev: 'MN' },
    { name: 'Mississippi', abbrev: 'MS' },
    { name: 'Missouri', abbrev: 'MO' },
    { name: 'Montana', abbrev: 'MT' },
    { name: 'Nebraska', abbrev: 'NE' },
    { name: 'Nevada', abbrev: 'NV' },
    { name: 'New Hampshire', abbrev: 'NH' },
    { name: 'New Jersey', abbrev: 'NJ' },
    { name: 'New Mexico', abbrev: 'NM' },
    { name: 'New York', abbrev: 'NY' },
    { name: 'North Carolina', abbrev: 'NC' },
    { name: 'North Dakota', abbrev: 'ND' },
    { name: 'Northern Mariana Islands', abbrev: 'MP' },
    { name: 'Ohio', abbrev: 'OH' },
    { name: 'Oklahoma', abbrev: 'OK' },
    { name: 'Oregon', abbrev: 'OR' },
    { name: 'Palau', abbrev: 'PW' },
    { name: 'Pennsylvania', abbrev: 'PA' },
    { name: 'Puerto Rico', abbrev: 'PR' },
    { name: 'Rhode Island', abbrev: 'RI' },
    { name: 'South Carolina', abbrev: 'SC' },
    { name: 'South Dakota', abbrev: 'SD' },
    { name: 'Tennessee', abbrev: 'TN' },
    { name: 'Texas', abbrev: 'TX' },
    { name: 'Utah', abbrev: 'UT' },
    { name: 'Vermont', abbrev: 'VT' },
    { name: 'Virgin Islands', abbrev: 'VI' },
    { name: 'Virginia', abbrev: 'VA' },
    { name: 'Washington', abbrev: 'WA' },
    { name: 'West Virginia', abbrev: 'WV' },
    { name: 'Wisconsin', abbrev: 'WI' },
    { name: 'Wyoming', abbrev: 'WY' },
  ];

  centerMap(map) {
    map.setView([37, -96], 3.8);
  }

  ngAfterViewInit(): void {
    //build map
    this.map = L.map('map', {
      center: [37, -96],
      zoomSnap: 0.1,
      zoomDelta: 0.1,
      zoom: 3.8,
    });

    function centerMap(map) {
      map.setView([37, -96], 3.8);
    }

    L.Control.HomeControl = L.Control.extend({
      options: {
        position: 'topleft',
      },

      onAdd: function (map) {
        var homeButton = L.DomUtil.create('div', 'homeButton');
        homeButton.innerHTML =
          '<a title="National View" role="button" aria-label="National View"><clr-icon shape="home" class="is-solid"></clr-icon></a>';
        homeButton.setAttribute(
          'class',
          'leaflet-control leaflet-touch leaflet-control-command control-zoom-full'
        );
        homeButton.onclick = function () {
          centerMap(map);
        };
        return homeButton;
      },

      onRemove: function (map) {
        //nothing
      },
    });

    L.control.homecontrol = function (opts) {
      return new L.Control.HomeControl(opts);
    };

    L.control.homecontrol({ position: 'topleft' }).addTo(this.map);
    //this.map.addControl(homeControl);

    var OpenStreetMap_Mapnik = L.tileLayer(
      'https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png',
      {
        maxZoom: 19,
        attribution:
          '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors',
      }
    );
    // Load the pattern image
    const loadPatternImage = (url, callback) => {
      const img = new Image();
      img.src = url;
      img.onload = () => callback(img);
    };

    this.cobraDataService.getPopulationData().subscribe((popInfo) => {
      console.log("first popinfo is:", popInfo["01001020200"]);
      const canvasRenderer = L.canvas();
      var tribalLands = featureLayer({
        url: 'https://geopub.epa.gov/arcgis/rest/services/EMEF/tribal/MapServer/2',
        style: {
          color: '#f50202', //'#404040',
          weight: 0,
          fill: true,
        },
        renderer: canvasRenderer,
      });
      /*fetch('../../../assets/map_data/us_tract_centroids.geojson')
        .then((response) => response.json())
        .then((data) => {
          const IRAfeatures = data.features.filter(
            (feature) => popInfo[feature.properties.GEOID]?.IRA_fraction > 0
          );
          const CJESTfeatures = data.features.filter(
            (feature) => popInfo[feature.properties.GEOID]?.CJEST > 0
          );

          /**** points */
          /*this.CJESTlayer = L.geoJSON(CJESTfeatures, {
            pointToLayer: function (feature, latlng) {
              return L.circleMarker(
                { lat: latlng.lat + 0.02, lng: latlng.lng + 0.02 },
                {
                  radius: 1,
                  fillColor: '#005EA2',
                  color: '#000',
                  weight: 0,
                  fillOpacity: 1,
                }
              );
            },
            renderer: canvasRenderer, // Use canvas renderer
          });*/

          /*this.IRAlayer = L.geoJSON(IRAfeatures, {
            pointToLayer: function (feature, latlng) {
              return L.circleMarker(latlng, {
                radius: 1,
                fillColor: '#673091',
                color: '#000',
                weight: 0,
                fillOpacity: 1,
              });
            },
            renderer: canvasRenderer, // Use canvas renderer
          });

          // Handle the overlayadd event to apply patterns dynamically
          this.map.on('overlayadd', (event) => {
            if (event.layer === this.IRAlayer) {
              event.layer.bringToFront();
            } /*else if (event.layer === this.CJESTlayer) {
              event.layer.bringToFront();
            }*/
          //});

          // Fetch and add the new Tribal Lands Test layer
          fetch('../../../assets/map_data/tribal_layers.geojson')
            .then((response) => response.json())
            .then((tribalLandsData) => {
              const tribalLandsTestLayer = L.geoJSON(tribalLandsData, {
                style: {
                  color: '#f50202', // Border color
                  weight: 1, // Line thickness
                  fillColor: '#f50202', // Fill color
                  fillOpacity: 0.5, // Opacity of the fill
                },
                renderer: L.canvas(), // Use canvas for performance
              });

              // Add it to the overlay layers
              var overlayLayers = {
                'Tribal Lands': tribalLandsTestLayer, // Add the new layer
                //'IRA Census Tracts': this.IRAlayer,
                //'CEJST Census Tracts': this.CJESTlayer,
              };

              // Add control to map
              L.control.layers(null, overlayLayers).addTo(this.map);
            })
            .catch((error) =>
              console.error('Error loading Tribal Lands GeoJSON:', error)
            );
          });


    this.map.addLayer(OpenStreetMap_Mapnik);
    //src\app\components\resultspanel\map_data\simplified_GEOJSON_TRACTS\01.geojson
    const geojsonUrl = '../../../assets/map_data/cb_2018_us_state_20m.geojson';
    fetch(geojsonUrl)
      .then((response) => response.json())
      .then((data) => {
        this.statesLayer = L.geoJSON(data, {
          style: {
            color: '#000',
            weight: 1,
            fill: false,
          },
        });
        this.map.addLayer(this.statesLayer);
        this.statesLayer.bringToFront();
      })
      .catch((error) => console.error('Error loading GeoJSON:', error));

    //listen to map move changes/when center of view or zoom changes:
    this.map.on('moveend', () => {
      this.onMoveEnd();
    });

    this.map.on('zoomend', () => {
      const zoom = this.map.getZoom();

      if (zoom >= 7 && this.IRAlayer) {
        // Update CJEST layer circleMarker radius
        /* this.CJESTlayer.eachLayer((layer) => {
          if (layer instanceof L.CircleMarker) {
            layer.setStyle({ radius: 2 });
          }
        });*/

        // Update IRA layer circleMarker radius
        this.IRAlayer.eachLayer((layer) => {
          if (layer instanceof L.CircleMarker) {
            layer.setStyle({ radius: 2 });
          }
        });
      } /*else if (this.CJESTlayer && this.IRAlayer) {
        this.CJESTlayer.eachLayer((layer) => {
          if (layer instanceof L.CircleMarker) {
            layer.setStyle({ radius: 1 });
          }
        });

        // Update IRA layer circleMarker radius
        this.IRAlayer.eachLayer((layer) => {
          if (layer instanceof L.CircleMarker) {
            layer.setStyle({ radius: 1 });
          }
        });
      }*/
    });
  }

  ngOnInit(): void {
    const geojsonUrl = '../../../assets/map_data/cb_2018_us_state_20m.geojson';
    fetch(geojsonUrl)
      .then((response) => response.json())
      .then((data) => {
        this.stateGeojson = data;
      })
      .catch((error) => console.error('Error loading state GeoJSON:', error));

    /*const tractUrl =
      '../../../assets/map_data/cb2023_census_simplified_merged2020CT.geojson';
    fetch(tractUrl)
      .then((response) => response.json())
      .then((data) => {
        this.full_tract_data = data;
      });*/

    /*const complextractUrl =
      '../../../assets/map_data/cb2023_census_unsimplified_merged2020CT.geojson';
    fetch(complextractUrl)
      .then((response) => response.json())
      .then((data) => {
        this.full_tract_data_complex = data;
      });*/

    this.checkFirstVisit();

    this.cobraDataService.getAllTribes().subscribe((data: any) => {
      this.allTribes = data.tribes.sort();
      this.filteredTribes = [...this.allTribes];
    });

    this.cobraDataService
      .getGeoDataforCensusTracts('full')
      .subscribe((data: any) => {
        this.full_tract_data_complex = data;
      });
    this.cobraDataService
      .getGeoDataforCensusTracts('simple')
      .subscribe((data: any) => {
        this.full_tract_data = data;
      });
  }

  checkFirstVisit() {
    const hasVisited = localStorage.getItem('hasVisited');
    this.isFirstVisit = !localStorage.getItem('hasVisited');

    if (!hasVisited) {
      //first visit
      localStorage.setItem('hasVisited', 'true');
    } else {
      // Returning visitor
    }
  }

  // Utility function to check if a feature is in view
  isFeatureInView(feature, bounds, mapCenter) {
    const featureBounds = L.geoJSON(feature).getBounds(); // Get bounds of the feature
    // Check if the bounds intersect at all
    if (!bounds.intersects(featureBounds)) {
      return false;
    }

    /*****
   * 
   *   // Check if the feature bounds are entirely within the map bounds
    const isFeatureWithinView = bounds.contains(featureBounds.getSouthWest()) && bounds.contains(featureBounds.getNorthEast());
  
    // Check if the map bounds are entirely within the feature bounds
    const isViewWithinFeature = featureBounds.contains(bounds.getSouthWest()) && featureBounds.contains(bounds.getNorthEast());
  
   */
    // Calculate the feature's bounding box area
    const featureArea = Math.abs(
      (featureBounds.getNorthEast().lat - featureBounds.getSouthWest().lat) *
        (featureBounds.getNorthEast().lng - featureBounds.getSouthWest().lng)
    );

    // Calculate the intersection bounds manually
    const intersectBounds = {
      southWest: {
        lat: Math.max(
          bounds.getSouthWest().lat,
          featureBounds.getSouthWest().lat
        ),
        lng: Math.max(
          bounds.getSouthWest().lng,
          featureBounds.getSouthWest().lng
        ),
      },
      northEast: {
        lat: Math.min(
          bounds.getNorthEast().lat,
          featureBounds.getNorthEast().lat
        ),
        lng: Math.min(
          bounds.getNorthEast().lng,
          featureBounds.getNorthEast().lng
        ),
      },
    };

    // If there's no intersection, return false
    if (
      intersectBounds.southWest.lat > intersectBounds.northEast.lat ||
      intersectBounds.southWest.lng > intersectBounds.northEast.lng
    ) {
      return false;
    }

    if (!intersectBounds) {
      return false; // No intersection
    }

    // Calculate the intersection area
    const intersectArea = Math.abs(
      (intersectBounds.northEast.lat - intersectBounds.southWest.lat) *
        (intersectBounds.northEast.lng - intersectBounds.southWest.lng)
    );

    //
    return (
      // Check if over 1% of the feature is in view
      intersectArea >= featureArea * 0.01 || featureBounds.contains(mapCenter)
    );

    //return bounds.intersects(featureBounds); // Check if bounds intersect

    // Check if the map center is within the feature bounds
    //return featureBounds.contains(mapCenter);
  }

  onMoveEnd() {
    const zoom = this.map.getZoom();
    const center = this.map.getCenter();
    const bounds = this.map.getBounds(); // Get current map bounds
    //loadFeaturesWithinBounds(bounds);

    if (zoom >= 7.6) {
      let visibleFeatures = [];
      // Loop through your features (assuming you have a GeoJSON object)
      county_data.features.forEach((feature) => {
        //find which US State the center of the map is within and add the corresponding censusTract Layer
        if (this.isFeatureInView(feature, bounds, center)) {
          //stateInView = feature;
          //no need to loop through rest of features once state has been found
          //return;

          visibleFeatures.push(feature);
        }
      });

      // Add visible features as layers
      //if (stateInView) {
      //get statecodes of visibleFeatures
      const countycodes = visibleFeatures.map((f) => f.properties.GEOID);

      if (countycodes.length > 0) {
        this.loadCensusGeoJSON(countycodes, zoom);
      } else {
        if (this.tractsLayer) {
          this.map.removeLayer(this.tractsLayer);
          this.tractsLayer = null;
        }
      }
    } else {
      if (this.tractsLayer) {
        this.map.removeLayer(this.tractsLayer);
        this.tractsLayer = null;
      }
      //configure legend for county layer
      if (this.items) {
        this.configureLegend(
          false,
          this.selectedMapLayer || 'C__Total_Health_Benefits_Low_Value'
        );
      }
    }
  }

  loadCensusGeoJSON(countycodes: string[], zoom) {
    const combinedFeatures = {
      type: 'FeatureCollection',
      crs: {
        type: 'name',
        properties: { name: 'urn:ogc:def:crs:OGC:1.3:CRS84' },
      },
      features: [],
    };

    //subset to only visible states
    let filteredFeatures = [];

    if (zoom >= 9.6) {
      //load more complex census layer
      filteredFeatures = this.full_tract_data_complex.features.filter(
        (feature) => countycodes.includes(feature.properties.GEOID.slice(0, 5))
      );

      combinedFeatures.features = filteredFeatures;

      if (this.items) {
        this.styleMap(this.selectedMapLayer, combinedFeatures);
      }
    } else {
      combinedFeatures.features = this.full_tract_data.features.filter(
        (feature) => {
          return countycodes.includes(feature.properties.GEOID.slice(0, 5));
        }
      );

      if (this.items) {
        this.styleMap(this.selectedMapLayer, combinedFeatures);
      }
    }
  }

  // <--------------------------------------------- Receives data from emissionspanel ----------------------------------------->
  /* This function receives state_clr_structure from emissionspanel when the page is loaded. The emitter "emissionspanelToResultspanelEmitter" sends this data to resultspanel component. state_clr_structure is needed in results panel in order to create state and county dropdowns for filtering. */
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
    // show results-screen
    // var pending_results_screen = document.getElementById("pending-results-screen-id");
    // pending_results_screen.setAttribute("hidden", "true");
    this.showPendingResultsScreen = false;
    var results_screen = document.getElementById('results-screen');
    results_screen.removeAttribute('hidden');

    // this.showResultsScreen = true;

    // show heartbeat animation
    // var heartbeat = document.getElementById("heartbeat");
    // heartbeat.removeAttribute("hidden");
    var results_panel_content = document.getElementById(
      'results_panel_content'
    );
    results_panel_content.setAttribute('hidden', 'true');
    this.showHeartbeat = true;
    // this.showResultsPanelContent = false;
  }
  // <---------------------------- Shows pending screen when runScenario() is called in reviewpanel/End ----------------------->

  // <----------------------------------------------- Receives data from reviewpanel ------------------------------------------>
  /* This function receives data from reviewpanel when Run Scenario button is clicked, actually after click and after the update for all components in review panel is done, and calls getResults() to create results in the table. The emitter "reviewpanelToResultspanelEmitter" sends this data to resultspanle component. */
  receiveDiscountRateAndGetResults(dataFromReviewPanel: any) {
    this.discountRate = dataFromReviewPanel['discountRate'];
    if (this.map.hasLayer(this.countyLayer)) {
      this.map.removeLayer(this.countyLayer);
    }
    this.getResults();
    // make sure the map displays consistently and invalidate the size so that it sizes itself correctly once results have been received
  }
  // <--------------------------------------------- Receives data from reviewpanel/End ---------------------------------------->

  // <------------------------------- Calls resultspanelToEmissionspanelBuildNewScenarioEmitter ------------------------------->
  emitFromResultspanelToEmissionspanelBuildNewScenario() {
    this.resultspanelToEmissionspanelBuildNewScenarioEmitter.emit(null);
  }
  // <----------------------------- Calls resultspanelToEmissionspanelBuildNewScenarioEmitter/End ----------------------------->

  // <--------------------------------- Calls resultspanelToReviewpanelBuildNewScenarioEmitter -------------------------------->
  emitFromResultspanelToReviewpanelBuildNewScenario() {
    this.resultspanelToReviewpanelBuildNewScenarioEmitter.emit(null);
  }
  // <------------------------------- Calls resultspanelToReviewpanelBuildNewScenarioEmitter/End ------------------------------>

  // <------------------------------------- Resets all dropdowns and data in results panel ------------------------------------>
  resetResultspanelDropdownsAndData() {
    // set state and county dropdowns to default, update filtervalue and update table results to be shown for all states
    var state_dropdown = document.getElementById(
      'state_dd'
    ) as HTMLSelectElement;
    var county_dropdown = document.getElementById(
      'county_dd'
    ) as HTMLSelectElement;
    state_dropdown.selectedIndex = 0;
    county_dropdown.selectedIndex = 0;
    this.updateCountyDropDownAndFilterVal('');
    // set map dropdown and map view to default
    var map_dropdown = document.getElementById('maplayer') as HTMLSelectElement;
    map_dropdown.selectedIndex = 3;
    this.selectedMapLayer = 'C__Total_Health_Benefits_Low_Value';
    if (
      this.map.hasLayer(this.tractsLayer) &&
      (this.full_tract_data || this.full_tract_data_complex)
    ) {
      //need to recompute choropleth values for tracts
      if (this.map.getZoom >= 9.6)
        this.precomputeChoroplethBreaks(this.full_tract_data_complex, true);
      else this.precomputeChoroplethBreaks(this.full_tract_data, true);
      this.onMoveEnd();
    } else {
      this.precomputeChoroplethBreaks(county_data);
      this.onMoveEnd();
    }
  }

  updateMapLayer(layerValue: string) {
    console.log('in updatemap layer with event:', event);
    console.log('selected layer is:', layerValue);
    this.selectedMapLayer = layerValue || this.selectedMapLayer;

    //updatecounty data
    const excludedFields = [
      'ID',
      'destindx',
      'tract_id',
      'FIPS',
      'STATE',
      'COUNTY',
      'IRA_fraction',
      'CJEST',
    ];


    //re style county layer
    county_data.features.forEach((feature) => {
      const fips = feature.properties.GEOID;
      if (this.groupedByFIPS[fips]) {
        const summedData = this.sumProperties(
          this.groupedByFIPS[fips],
          excludedFields
        );
        feature.properties.DATA = summedData;
      } else {
        feature.properties.DATA = {}; // No matching data found
      }
    });
    this.precomputeChoroplethBreaks(county_data);
    this.styleMap(this.selectedMapLayer);



    if (
      this.map.hasLayer(this.tractsLayer) &&
      (this.full_tract_data || this.full_tract_data_complex)
    ) {
      //need to recompute choropleth values for tracts
      if (this.map.getZoom >= 9.6)
        this.precomputeChoroplethBreaks(this.full_tract_data_complex, true);
      else this.precomputeChoroplethBreaks(this.full_tract_data, true);
      this.onMoveEnd();
    } 
  }
  // <----------------------------------- Resets all dropdowns and data in results panel/End ---------------------------------->

  // <--------------------------- Returns the app to its initial state in order to build a new scenario ----------------------->
  /* This function is called when the user confirms to build a new scenario. The confirmation happens in the modal that pops up after clicking on Build New Scenario button. This returns the app to its initial state. */
  buildNewScenario() {
    this.showBuildNewConfirmationModal = false;
    this.showNoResultsScreen = true;
    document.getElementById('results-screen').setAttribute('hidden', 'true');
    this.resetResultspanelDropdownsAndData();
    if (window.screen.width <= 991) {
      document.getElementById('step2').style.visibility = 'hidden';
      document.getElementById('step3').style.visibility = 'hidden';
    }
    this.emitFromResultspanelToEmissionspanelBuildNewScenario();
    this.emitFromResultspanelToReviewpanelBuildNewScenario();
  }
  // <------------------------- Returns the app to its initial state in order to build a new scenario/End --------------------->

  // <---------------------------------- Removes filters and shows table data for all states ---------------------------------->
  showTableDataForAllStates() {
    if (!this.items || !this.summary) {
      return;
    }
    this.TotalHealthBenefitsValue_high =
      this.summary['TotalHealthBenefitsValue_high'];
    this.TotalHealthBenefitsValue_low =
      this.summary['TotalHealthBenefitsValue_low'];
    this.TotalPM_high = this.summary['TotalPMValue_high'];
    this.TotalPM_low = this.summary['TotalPMValue_low'];
    this.TotalO3 = this.summary['TotalO3Value'];

    this.Mortality_low = this.summary['Mortality_All_Cause__low_'];
    this.Mortality_high = this.summary['PM_Mortality_All_Cause__high_'];
    this.PM_Mortality_low = this.summary['PM_Mortality_All_Cause__low_'];
    this.PM_Mortality_high = this.summary['PM_Mortality_All_Cause__high_'];

    this.NonfatalHeartAttacks =
      this.summary['Acute_Myocardial_Infarction_Nonfatal'];

    this.InfantMortality = this.summary['Infant_Mortality'];

    this.HospitalAdmitsAllRespiratory = this.summary['HA_All_Respiratory'];
    this.PMHospitalAdmitsAllRespiratory = this.summary['PM_HA_All_Respiratory'];
    this.PMHospitalAdmitsAllRespiratory = this.summary['PM_HA_All_Respiratory'];

    this.EmergencyRoomVisitsAsthma = this.summary['ER_Visits_Asthma'];

    this.MinorRestrictedActivityDays =
      this.summary['Minor_Restricted_Activity_Days'];
    this.WorkLossDays = this.summary['Work_Loss_Days'];
    this.MortalityValue_low = this.summary['C__Mortality_All_Cause__low_'];
    this.MortalityValue_high = this.summary['C__Mortality_All_Cause__high_'];
    this.PM_MortalityValue_high =
      this.summary['C__PM_Mortality_All_Cause__high_'];
    this.PM_MortalityValue_low =
      this.summary['C__PM_Mortality_All_Cause__low_'];

    this.NonfatalHeartAttacksValue =
      this.summary['C__Acute_Myocardial_Infarction_Nonfatal'];
    this.InfantMortalityValue = this.summary['C__Infant_Mortality'];
    this.HospitalAdmitsAllRespiratoryValue =
      this.summary['C__HA_All_Respiratory'];
    this.PMHospitalAdmitsAllRespiratoryValue =
      this.summary['C__PM_HA_All_Respiratory'];
    this.O3HospitalAdmitsAllRespiratoryValue =
      this.summary['C__O3_HA_All_Respiratory'];

    this.MinorRestrictedActivityDaysValue =
      this.summary['C__Minor_Restricted_Activity_Days'];
    this.WorkLossDaysValue = this.summary['C__Work_Loss_Days'];

    //new 2023 health endpoints
    this.IncidenceLungCancer = this.summary['Incidence_Lung_Cancer'];
    this.IncidenceLungCancerValue = this.summary['C__Incidence_Lung_Cancer'];

    this.HA_HCCPV_Disease =
      this.summary['HA_Cardio_Cerebro_and_Peripheral_Vascular_Disease'];
    this.HA_HCCPV_DiseaseValue =
      this.summary['C__HA_Cardio_Cerebro_and_Peripheral_Vascular_Disease'];

    this.HospitalAdmitsParkinsonsDisease =
      this.summary['HA_Parkinsons_Disease'];
    this.HospitalAdmitsParkinsonsDiseaseValue =
      this.summary['C__HA_Parkinsons_Disease'];

    this.HospitalAdmitsAlzheimersDisease =
      this.summary['HA_Alzheimers_Disease'];
    this.HospitalAdmitsAlzheimersDiseaseValue =
      this.summary['C__HA_Alzheimers_Disease'];

    this.IncidenceStroke = this.summary['Incidence_Stroke'];
    this.IncidenceStrokeValue = this.summary['C__Incidence_Stroke'];

    this.IncidenceOutOfHospitalCardiacArrest =
      this.summary['Incidence_Out_of_Hospital_Cardiac_Arrest'];
    this.IncidenceOutOfHospitalCardiacArrestValue =
      this.summary['C__Incidence_Out_of_Hospital_Cardiac_Arrest'];

    this.IncidenceAsthma = this.summary['Incidence_Asthma'];
    this.IncidenceAsthmaValue = this.summary['C__Incidence_Asthma'];
    this.PMIncidenceAsthma = this.summary['PM_Incidence_Asthma'];
    this.PMIncidenceAsthmaValue = this.summary['C__PM_Incidence_Asthma'];
    this.O3IncidenceAsthma = this.summary['O3_Incidence_Asthma'];
    this.O3IncidenceAsthmaValue = this.summary['C__O3_Incidence_Asthma'];

    this.IncidenceHayFeverRhinitis =
      this.summary['Incidence_Hay_Fever_Rhinitis'];
    this.IncidenceHayFeverRhinitisValue =
      this.summary['C__Incidence_Hay_Fever_Rhinitis'];
    this.PMIncidenceHayFeverRhinitis =
      this.summary['PM_Incidence_Hay_Fever_Rhinitis'];
    this.PMIncidenceHayFeverRhinitisValue =
      this.summary['C__PM_Incidence_Hay_Fever_Rhinitis'];
    this.O3IncidenceHayFeverRhinitis =
      this.summary['O3_Incidence_Hay_Fever_Rhinitis'];
    this.O3IncidenceHayFeverRhinitisValue =
      this.summary['C__O3_Incidence_Hay_Fever_Rhinitis'];

    this.ERVisitsAllCardiacOutcomes =
      this.summary['ER_visits_All_Cardiac_Outcomes'];
    this.ERVisitsAllCardiacOutcomesValue =
      this.summary['C__ER_visits_All_Cardiac_Outcomes'];

    this.ERVisitsAllRespiratory = this.summary['ER_visits_respiratory'];
    this.ERVisitsAllRespiratoryValue = this.summary['C__ER_visits_respiratory'];
    this.PMERVisitsAllRespiratory = this.summary['PM_ER_visits_respiratory'];
    this.PMERVisitsAllRespiratoryValue =
      this.summary['C__PM_ER_visits_respiratory'];

    this.AsthmaSymptoms = this.summary['Asthma_Symptoms'];
    this.AsthmaSymptomsValue = this.summary['C__Asthma_Symptoms'];
    this.AlbuterolUse = this.summary['PM_Asthma_Symptoms_Albuterol_use'];
    this.AlbuterolUseValue =
      this.summary['C__PM_Asthma_Symptoms_Albuterol_use'];
    this.Cough = this.summary['O3_Asthma_Symptoms_Cough'];
    this.CoughValue = this.summary['C__O3_Asthma_Symptoms_Cough'];
    this.ChestTightness = this.summary['O3_Asthma_Symptoms_Chest_Tightness'];
    this.ChestTightnessValue =
      this.summary['C__O3_Asthma_Symptoms_Chest_Tightness'];
    this.ShortnessOfBreath =
      this.summary['O3_Asthma_Symptoms_Shortness_of_Breath'];
    this.ShortnessOfBreathValue =
      this.summary['C__O3_Asthma_Symptoms_Shortness_of_Breath'];
    this.Wheeze = this.summary['O3_Asthma_Symptoms_Wheeze'];
    this.WheezeValue = this.summary['C__O3_Asthma_Symptoms_Wheeze'];

    this.SchoolLossDays = this.summary['School_Loss_Days'];
    this.SchoolLossDaysValue = this.summary['C__School_Loss_Days'];
  }
  // <-------------------------------- Removes filters and shows table data for all states/End -------------------------------->

  // <-------------------------------------------- Filters data for state selection ------------------------------------------->
  resetTableVals() {
    //reset everything to zero
    this.TotalHealthBenefitsValue_high = 0;
    this.TotalHealthBenefitsValue_low = 0;

    this.TotalPM_high = 0;
    this.TotalPM_low = 0;
    this.TotalO3 = 0;

    this.Mortality_low = 0;
    this.MortalityValue_low = 0;

    this.MortalityValue_high = 0;
    this.Mortality_high = 0;

    this.PM_Mortality_low = 0;
    this.PM_Mortality_high = 0;
    this.PM_MortalityValue_low = 0;
    this.PM_MortalityValue_high = 0;

    this.O3_Mortality_long = 0;
    this.O3_Mortality_short = 0;
    this.O3_MortalityValue_long = 0;
    this.O3_MortalityValue_short = 0;

    this.NonfatalHeartAttacks = 0;
    this.NonfatalHeartAttacksValue = 0;

    this.InfantMortality = 0;
    this.InfantMortalityValue = 0;

    this.HospitalAdmitsAllRespiratory = 0;
    this.HospitalAdmitsAllRespiratoryValue = 0;

    this.PMHospitalAdmitsAllRespiratory = 0;
    this.PMHospitalAdmitsAllRespiratoryValue = 0;

    this.O3HospitalAdmitsAllRespiratory = 0;
    this.O3HospitalAdmitsAllRespiratoryValue = 0;

    this.EmergencyRoomVisitsAsthma = 0;
    this.EmergencyRoomVisitsAsthmaValue = 0;

    this.MinorRestrictedActivityDays = 0;
    this.MinorRestrictedActivityDaysValue = 0;

    this.HospitalAdmitsAlzheimersDisease = 0;
    this.HospitalAdmitsAlzheimersDiseaseValue = 0;

    this.HospitalAdmitsParkinsonsDisease = 0;
    this.HospitalAdmitsParkinsonsDiseaseValue = 0;

    this.IncidenceStroke = 0;
    this.IncidenceStrokeValue = 0;

    this.IncidenceOutOfHospitalCardiacArrest = 0;
    this.IncidenceOutOfHospitalCardiacArrestValue = 0;

    this.IncidenceAsthma = 0;
    this.IncidenceAsthmaValue = 0;

    this.PMIncidenceAsthma = 0;
    this.PMIncidenceAsthmaValue = 0;
    this.O3IncidenceAsthma = 0;
    this.O3IncidenceAsthmaValue = 0;

    this.AsthmaSymptoms = 0;
    this.AsthmaSymptomsValue = 0;
    this.AlbuterolUse = 0;
    this.AlbuterolUseValue = 0;
    this.Cough = 0;
    this.CoughValue = 0;
    this.ChestTightness = 0;
    this.ChestTightnessValue = 0;
    this.ShortnessOfBreath = 0;
    this.ShortnessOfBreathValue = 0;
    this.Wheeze = 0;
    this.WheezeValue = 0;

    this.IncidenceHayFeverRhinitis = 0;
    this.IncidenceHayFeverRhinitisValue = 0;
    this.PMIncidenceHayFeverRhinitis = 0;
    this.PMIncidenceHayFeverRhinitisValue = 0;
    this.O3IncidenceHayFeverRhinitis = 0;
    this.O3IncidenceHayFeverRhinitisValue = 0;

    this.HA_HCCPV_Disease = 0;
    this.HA_HCCPV_DiseaseValue = 0;

    this.IncidenceLungCancer = 0;
    this.IncidenceLungCancerValue = 0;

    this.ERVisitsAllCardiacOutcomes = 0;
    this.ERVisitsAllCardiacOutcomesValue = 0;

    this.ERVisitsAllRespiratory = 0;
    this.ERVisitsAllRespiratoryValue = 0;
    this.PMERVisitsAllRespiratory = 0;
    this.PMERVisitsAllRespiratoryValue = 0;
    this.O3ERVisitsAllRespiratory = 0;
    this.O3ERVisitsAllRespiratoryValue = 0;

    this.SchoolLossDays = 0;
    this.SchoolLossDaysValue = 0;

    this.WorkLossDays = 0;
    this.WorkLossDaysValue = 0;
  }
  filterDataForStateSelection() {
    if (!this.items) return;

    this.resetTableVals();
    this.items.map((item, i) => {
      if (item.FIPS.substr(0, 2) == this.filtervalue) {
        this.TotalHealthBenefitsValue_high +=
          item['C__Total_Health_Benefits_High_Value'];
        this.TotalHealthBenefitsValue_low +=
          item['C__Total_Health_Benefits_Low_Value'];
        this.TotalPM_high += item['C__Total_PM_High_Value'];
        this.TotalPM_low += item['C__Total_PM_Low_Value'];
        this.TotalO3 += item['C__Total_O3_Value'];
        this.Mortality_low +=
          item['PM_Mortality_All_Cause__low_'] +
          item['O3_Mortality_Longterm_exposure'] +
          item['O3_Mortality_Shortterm_exposure'];
        this.Mortality_high +=
          item['PM_Mortality_All_Cause__high_'] +
          item['O3_Mortality_Longterm_exposure'] +
          item['O3_Mortality_Shortterm_exposure'];
        this.PM_Mortality_high += item['PM_Mortality_All_Cause__high_'];
        this.PM_MortalityValue_high += item['C__PM_Mortality_All_Cause__high_'];
        this.PM_Mortality_low += item['PM_Mortality_All_Cause__low_'];
        this.PM_MortalityValue_low += item['C__PM_Mortality_All_Cause__low_'];
        this.O3_Mortality_long += item['O3_Mortality_Longterm_exposure'];
        this.O3_MortalityValue_long +=
          item['C__O3_Mortality_Longterm_exposure'];
        this.O3_Mortality_short += item['O3_Mortality_Shortterm_exposure'];
        this.O3_MortalityValue_short +=
          item['C__O3_Mortality_Shortterm_exposure'];

        this.NonfatalHeartAttacks +=
          item['PM_Acute_Myocardial_Infarction_Nonfatal'];

        this.InfantMortality += item['PM_Infant_Mortality'];

        this.HospitalAdmitsAllRespiratory +=
          item['PM_HA_All_Respiratory'] +
          item['PM_HA_Respiratory2'] +
          item['O3_HA_All_Respiratory'];
        this.PMHospitalAdmitsAllRespiratory +=
          item['PM_HA_All_Respiratory'] + item['PM_HA_Respiratory2'];
        this.PMHospitalAdmitsAllRespiratoryValue +=
          item['C__PM_Resp_Hosp_Adm'] + item['C__PM_HA_Respiratory2'];

        this.O3HospitalAdmitsAllRespiratory += item['O3_HA_All_Respiratory'];
        this.O3HospitalAdmitsAllRespiratoryValue +=
          item['C__O3_HA_All_Respiratory'];

        this.EmergencyRoomVisitsAsthma += item['O3_ER_Visits_Asthma'];
        this.MinorRestrictedActivityDays +=
          item['PM_Minor_Restricted_Activity_Days'];
        this.WorkLossDays += item['PM_Work_Loss_Days'];
        this.MortalityValue_low +=
          item['C__PM_Mortality_All_Cause__low_'] +
          item['C__O3_Mortality_Longterm_exposure'] +
          item['C__O3_Mortality_Shortterm_exposure'];
        this.MortalityValue_high +=
          item['C__PM_Mortality_All_Cause__high_'] +
          item['C__O3_Mortality_Longterm_exposure'] +
          item['C__O3_Mortality_Shortterm_exposure'];
        this.NonfatalHeartAttacksValue +=
          item['C__PM_Acute_Myocardial_Infarction_Nonfatal'];

        this.InfantMortalityValue += item['C__PM_Infant_Mortality'];
        this.HospitalAdmitsAllRespiratoryValue +=
          item['C__PM_Resp_Hosp_Adm'] +
          item['C__O3_HA_All_Respiratory'] +
          item['C__PM_HA_Respiratory2'];
        this.EmergencyRoomVisitsAsthmaValue += item['C__O3_ER_Visits_Asthma'];
        this.MinorRestrictedActivityDaysValue +=
          item['C__PM_Minor_Restricted_Activity_Days'];
        this.WorkLossDaysValue += item['C__PM_Work_Loss_Days'];

        //new 2023 health endpoints
        this.IncidenceLungCancer += item['PM_Incidence_Lung_Cancer'];
        this.IncidenceLungCancerValue += item['C__PM_Incidence_Lung_Cancer'];

        this.HA_HCCPV_Disease +=
          item['PM_HA_Cardio_Cerebro_and_Peripheral_Vascular_Disease'];
        this.HA_HCCPV_DiseaseValue +=
          item['C__PM_HA_Cardio_Cerebro_and_Peripheral_Vascular_Disease'];

        this.HospitalAdmitsParkinsonsDisease +=
          item['PM_HA_Parkinsons_Disease'];
        this.HospitalAdmitsParkinsonsDiseaseValue +=
          item['C__PM_HA_Parkinsons_Disease'];

        this.HospitalAdmitsAlzheimersDisease +=
          item['PM_HA_Alzheimers_Disease'];
        this.HospitalAdmitsAlzheimersDiseaseValue +=
          item['C__PM_HA_Alzheimers_Disease'];

        this.IncidenceStroke += item['PM_Incidence_Stroke'];
        this.IncidenceStrokeValue += item['C__PM_Incidence_Stroke'];

        this.IncidenceOutOfHospitalCardiacArrest +=
          item['PM_Incidence_Out_of_Hospital_Cardiac_Arrest'];
        this.IncidenceOutOfHospitalCardiacArrestValue +=
          item['C__PM_Incidence_Out_of_Hospital_Cardiac_Arrest'];

        this.IncidenceAsthma +=
          item['PM_Incidence_Asthma'] + item['O3_Incidence_Asthma'];
        this.IncidenceAsthmaValue =
          item['C__PM_Incidence_Asthma'] + item['C__O3_Incidence_Asthma'];
        this.PMIncidenceAsthma += item['PM_Incidence_Asthma'];
        this.PMIncidenceAsthmaValue += item['C__PM_Incidence_Asthma'];
        this.O3IncidenceAsthma += item['O3_Incidence_Asthma'];
        this.O3IncidenceAsthmaValue += item['C__O3_Incidence_Asthma'];

        this.IncidenceHayFeverRhinitis +=
          item['PM_Incidence_Hay_Fever_Rhinitis'] +
          item['O3_Incidence_Hay_Fever_Rhinitis'];
        this.IncidenceHayFeverRhinitisValue +=
          item['C__PM_Incidence_Hay_Fever_Rhinitis'] +
          item['C__O3_Incidence_Hay_Fever_Rhinitis'];

        this.PMIncidenceHayFeverRhinitis +=
          item['PM_Incidence_Hay_Fever_Rhinitis'];

        this.PMIncidenceHayFeverRhinitisValue +=
          item['C__PM_Incidence_Hay_Fever_Rhinitis'];
        this.O3IncidenceHayFeverRhinitis +=
          item['O3_Incidence_Hay_Fever_Rhinitis'];

        this.O3IncidenceHayFeverRhinitisValue +=
          item['C__O3_Incidence_Hay_Fever_Rhinitis'];

        this.ERVisitsAllCardiacOutcomes +=
          item['PM_ER_visits_All_Cardiac_Outcomes'];
        this.ERVisitsAllCardiacOutcomesValue +=
          item['C__PM_ER_visits_All_Cardiac_Outcomes'];

        this.ERVisitsAllRespiratory +=
          item['PM_ER_visits_respiratory'] + item['O3_ER_visits_respiratory'];
        this.ERVisitsAllRespiratoryValue +=
          item['C__PM_ER_visits_respiratory'] +
          item['C__O3_ER_visits_respiratory'];
        this.PMERVisitsAllRespiratory += item['PM_ER_visits_respiratory'];
        this.PMERVisitsAllRespiratoryValue +=
          item['C__PM_ER_visits_respiratory'];
        this.O3ERVisitsAllRespiratory += item['O3_ER_visits_respiratory'];
        this.O3ERVisitsAllRespiratoryValue +=
          item['C__O3_ER_visits_respiratory'];

        this.AsthmaSymptoms +=
          item['PM_Asthma_Symptoms_Albuterol_use'] +
          item['O3_Asthma_Symptoms_Chest_Tightness'] +
          item['O3_Asthma_Symptoms_Cough'] +
          item['O3_Asthma_Symptoms_Wheeze'] +
          item['O3_Asthma_Symptoms_Shortness_of_Breath'];
        this.AsthmaSymptomsValue +=
          item['C__PM_Asthma_Symptoms_Albuterol_use'] +
          item['C__O3_Asthma_Symptoms_Chest_Tightness'] +
          item['C__O3_Asthma_Symptoms_Cough'] +
          item['C__O3_Asthma_Symptoms_Wheeze'] +
          item['C__O3_Asthma_Symptoms_Shortness_of_Breath'];

        this.AlbuterolUse += item['PM_Asthma_Symptoms_Albuterol_use'];
        this.AlbuterolUseValue += item['C__PM_Asthma_Symptoms_Albuterol_use'];

        this.ChestTightness += item['O3_Asthma_Symptoms_Chest_Tightness'];
        this.ChestTightnessValue +=
          item['C__O3_Asthma_Symptoms_Chest_Tightness'];

        this.Cough += item['O3_Asthma_Symptoms_Cough'];
        this.CoughValue += item['C__O3_Asthma_Symptoms_Cough'];

        this.ShortnessOfBreath +=
          item['O3_Asthma_Symptoms_Shortness_of_Breath'];
        this.ShortnessOfBreathValue +=
          item['C__O3_Asthma_Symptoms_Shortness_of_Breath'];

        this.Wheeze += item['O3_Asthma_Symptoms_Wheeze'];
        this.WheezeValue += item['C__O3_Asthma_Symptoms_Wheeze'];

        this.SchoolLossDays += item['O3_School_Loss_Days'];
        this.SchoolLossDaysValue += item['C__O3_School_Loss_Days'];
      }
    });
  }
  // <-------------------------------------------- Filters data for state selection ------------------------------------------->
  // <----------------------------------Update Tribe FIlter ------------------>
  updateTribeFilterVal() {
    var county_dropdown = document.getElementById('county_dd');
    county_dropdown.setAttribute('disabled', 'true');

    var state_dropdown = document.getElementById(
      'state_dd'
    ) as HTMLSelectElement;
    state_dropdown.setAttribute('disabled', 'true');
  }
  // <----------------------------------Updates tribe options on user search input ------------------>
  filterTribes() {
    const filterValue = this.tribeSearchInput.toLowerCase();
    this.filteredTribes = this.allTribes.filter((tribe) =>
      tribe.toLowerCase().includes(filterValue)
    );
  }

  clearTribeSelection() {
    this.tribeSearchInput = '';
    this.selectedTribe = '';
    // this.showTribeDropdown = true; // Show dropdown again after clearing
    this.filterTribes();
    //get all results for all states
    this.updateCountyDropDownAndFilterVal('');
  }

  // selects a tribe and updates input
  selectTribe(tribe: string) {
    this.showTribeDropdown = false; // Hide dropdown after selection
    //make sure it's clear that user is searching all states and counties by setting to all states and all counties + disabling them
    this.selectedStateIndex = '';
    this.selectedCountyIndex = '';
    //this.updateCountyDropDownAndFilterVal('');

    this.tribeSearchInput = tribe;
    this.selectedTribe = tribe;
    this.resetTableVals();

    //now actually filter results based on selected Tribe
    if (!this.items) return;
    this.items.map((item, i) => {
      if (item.TRIBES && item.TRIBES[this.selectedTribe]) {
        console.log('passed tribe condition with item.TRIBES = ', item.TRIBES);
        this.TotalHealthBenefitsValue_high +=
          item['C__Total_Health_Benefits_High_Value'] *
          item.TRIBES[this.selectedTribe];
        this.TotalHealthBenefitsValue_low +=
          item['C__Total_Health_Benefits_Low_Value'] *
          item.TRIBES[this.selectedTribe];
        this.TotalPM_high +=
          item['C__Total_PM_High_Value'] * item.TRIBES[this.selectedTribe];
        this.TotalPM_low +=
          item['C__Total_PM_Low_Value'] * item.TRIBES[this.selectedTribe];
        this.TotalO3 +=
          item['C__Total_O3_Value'] * item.TRIBES[this.selectedTribe];
        this.Mortality_low +=
          (item['PM_Mortality_All_Cause__low_'] +
            item['O3_Mortality_Longterm_exposure'] +
            item['O3_Mortality_Shortterm_exposure']) *
          item.TRIBES[this.selectedTribe];
        this.Mortality_high +=
          (item['PM_Mortality_All_Cause__high_'] +
            item['O3_Mortality_Longterm_exposure'] +
            item['O3_Mortality_Shortterm_exposure']) *
          item.TRIBES[this.selectedTribe];
        this.PM_Mortality_high +=
          item['PM_Mortality_All_Cause__high_'] *
          item.TRIBES[this.selectedTribe];
        this.PM_MortalityValue_high +=
          item['C__PM_Mortality_All_Cause__high_'] *
          item.TRIBES[this.selectedTribe];
        this.PM_Mortality_low +=
          item['PM_Mortality_All_Cause__low_'] *
          item.TRIBES[this.selectedTribe];
        this.PM_MortalityValue_low +=
          item['C__PM_Mortality_All_Cause__low_'] *
          item.TRIBES[this.selectedTribe];
        this.O3_Mortality_long +=
          item['O3_Mortality_Longterm_exposure'] *
          item.TRIBES[this.selectedTribe];
        this.O3_MortalityValue_long +=
          item['C__O3_Mortality_Longterm_exposure'] *
          item.TRIBES[this.selectedTribe];
        this.O3_Mortality_short +=
          item['O3_Mortality_Shortterm_exposure'] *
          item.TRIBES[this.selectedTribe];
        this.O3_MortalityValue_short +=
          item['C__O3_Mortality_Shortterm_exposure'] *
          item.TRIBES[this.selectedTribe];

        this.NonfatalHeartAttacks +=
          item['PM_Acute_Myocardial_Infarction_Nonfatal'] *
          item.TRIBES[this.selectedTribe];

        this.InfantMortality +=
          item['PM_Infant_Mortality'] * item.TRIBES[this.selectedTribe];

        this.HospitalAdmitsAllRespiratory +=
          (item['PM_HA_All_Respiratory'] +
            item['PM_HA_Respiratory2'] +
            item['O3_HA_All_Respiratory']) *
          item.TRIBES[this.selectedTribe];
        this.PMHospitalAdmitsAllRespiratory +=
          (item['PM_HA_All_Respiratory'] + item['PM_HA_Respiratory2']) *
          item.TRIBES[this.selectedTribe];
        this.PMHospitalAdmitsAllRespiratoryValue +=
          (item['C__PM_Resp_Hosp_Adm'] + item['C__PM_HA_Respiratory2']) *
          item.TRIBES[this.selectedTribe];

        this.O3HospitalAdmitsAllRespiratory +=
          item['O3_HA_All_Respiratory'] * item.TRIBES[this.selectedTribe];
        this.O3HospitalAdmitsAllRespiratoryValue +=
          item['C__O3_HA_All_Respiratory'] * item.TRIBES[this.selectedTribe];

        this.EmergencyRoomVisitsAsthma +=
          item['O3_ER_Visits_Asthma'] * item.TRIBES[this.selectedTribe];
        this.MinorRestrictedActivityDays +=
          item['PM_Minor_Restricted_Activity_Days'] *
          item.TRIBES[this.selectedTribe];
        this.WorkLossDays +=
          item['PM_Work_Loss_Days'] * item.TRIBES[this.selectedTribe];
        this.MortalityValue_low +=
          (item['C__PM_Mortality_All_Cause__low_'] +
            item['C__O3_Mortality_Longterm_exposure'] +
            item['C__O3_Mortality_Shortterm_exposure']) *
          item.TRIBES[this.selectedTribe];
        this.MortalityValue_high +=
          (item['C__PM_Mortality_All_Cause__high_'] +
            item['C__O3_Mortality_Longterm_exposure'] +
            item['C__O3_Mortality_Shortterm_exposure']) *
          item.TRIBES[this.selectedTribe];
        this.NonfatalHeartAttacksValue +=
          item['C__PM_Acute_Myocardial_Infarction_Nonfatal'] *
          item.TRIBES[this.selectedTribe];

        this.InfantMortalityValue +=
          item['C__PM_Infant_Mortality'] * item.TRIBES[this.selectedTribe];
        this.HospitalAdmitsAllRespiratoryValue +=
          (item['C__PM_Resp_Hosp_Adm'] +
            item['C__O3_HA_All_Respiratory'] +
            item['C__PM_HA_Respiratory2']) *
          item.TRIBES[this.selectedTribe];
        this.EmergencyRoomVisitsAsthmaValue +=
          item['C__O3_ER_Visits_Asthma'] * item.TRIBES[this.selectedTribe];
        this.MinorRestrictedActivityDaysValue +=
          item['C__PM_Minor_Restricted_Activity_Days'] *
          item.TRIBES[this.selectedTribe];
        this.WorkLossDaysValue +=
          item['C__PM_Work_Loss_Days'] * item.TRIBES[this.selectedTribe];

        //new 2023 health endpoints
        this.IncidenceLungCancer +=
          item['PM_Incidence_Lung_Cancer'] * item.TRIBES[this.selectedTribe];
        this.IncidenceLungCancerValue +=
          item['C__PM_Incidence_Lung_Cancer'] * item.TRIBES[this.selectedTribe];

        this.HA_HCCPV_Disease +=
          item['PM_HA_Cardio_Cerebro_and_Peripheral_Vascular_Disease'] *
          item.TRIBES[this.selectedTribe];
        this.HA_HCCPV_DiseaseValue +=
          item['C__PM_HA_Cardio_Cerebro_and_Peripheral_Vascular_Disease'] *
          item.TRIBES[this.selectedTribe];

        this.HospitalAdmitsParkinsonsDisease +=
          item['PM_HA_Parkinsons_Disease'] * item.TRIBES[this.selectedTribe];
        this.HospitalAdmitsParkinsonsDiseaseValue +=
          item['C__PM_HA_Parkinsons_Disease'] * item.TRIBES[this.selectedTribe];

        this.HospitalAdmitsAlzheimersDisease +=
          item['PM_HA_Alzheimers_Disease'] * item.TRIBES[this.selectedTribe];
        this.HospitalAdmitsAlzheimersDiseaseValue +=
          item['C__PM_HA_Alzheimers_Disease'] * item.TRIBES[this.selectedTribe];

        this.IncidenceStroke +=
          item['PM_Incidence_Stroke'] * item.TRIBES[this.selectedTribe];
        this.IncidenceStrokeValue +=
          item['C__PM_Incidence_Stroke'] * item.TRIBES[this.selectedTribe];

        this.IncidenceOutOfHospitalCardiacArrest +=
          item['PM_Incidence_Out_of_Hospital_Cardiac_Arrest'] *
          item.TRIBES[this.selectedTribe];
        this.IncidenceOutOfHospitalCardiacArrestValue +=
          item['C__PM_Incidence_Out_of_Hospital_Cardiac_Arrest'] *
          item.TRIBES[this.selectedTribe];

        this.IncidenceAsthma +=
          (item['PM_Incidence_Asthma'] + item['O3_Incidence_Asthma']) *
          item.TRIBES[this.selectedTribe];
        this.IncidenceAsthmaValue =
          (item['C__PM_Incidence_Asthma'] + item['C__O3_Incidence_Asthma']) *
          item.TRIBES[this.selectedTribe];
        this.PMIncidenceAsthma +=
          item['PM_Incidence_Asthma'] * item.TRIBES[this.selectedTribe];
        this.PMIncidenceAsthmaValue +=
          item['C__PM_Incidence_Asthma'] * item.TRIBES[this.selectedTribe];
        this.O3IncidenceAsthma +=
          item['O3_Incidence_Asthma'] * item.TRIBES[this.selectedTribe];
        this.O3IncidenceAsthmaValue +=
          item['C__O3_Incidence_Asthma'] * item.TRIBES[this.selectedTribe];

        this.IncidenceHayFeverRhinitis +=
          (item['PM_Incidence_Hay_Fever_Rhinitis'] +
            item['O3_Incidence_Hay_Fever_Rhinitis']) *
          item.TRIBES[this.selectedTribe];
        this.IncidenceHayFeverRhinitisValue +=
          (item['C__PM_Incidence_Hay_Fever_Rhinitis'] +
            item['C__O3_Incidence_Hay_Fever_Rhinitis']) *
          item.TRIBES[this.selectedTribe];

        this.PMIncidenceHayFeverRhinitis +=
          item['PM_Incidence_Hay_Fever_Rhinitis'] *
          item.TRIBES[this.selectedTribe];

        this.PMIncidenceHayFeverRhinitisValue +=
          item['C__PM_Incidence_Hay_Fever_Rhinitis'] *
          item.TRIBES[this.selectedTribe];
        this.O3IncidenceHayFeverRhinitis +=
          item['O3_Incidence_Hay_Fever_Rhinitis'] *
          item.TRIBES[this.selectedTribe];

        this.O3IncidenceHayFeverRhinitisValue +=
          item['C__O3_Incidence_Hay_Fever_Rhinitis'] *
          item.TRIBES[this.selectedTribe];

        this.ERVisitsAllCardiacOutcomes +=
          item['PM_ER_visits_All_Cardiac_Outcomes'] *
          item.TRIBES[this.selectedTribe];
        this.ERVisitsAllCardiacOutcomesValue +=
          item['C__PM_ER_visits_All_Cardiac_Outcomes'] *
          item.TRIBES[this.selectedTribe];

        this.ERVisitsAllRespiratory +=
          (item['PM_ER_visits_respiratory'] +
            item['O3_ER_visits_respiratory']) *
          item.TRIBES[this.selectedTribe];
        this.ERVisitsAllRespiratoryValue +=
          (item['C__PM_ER_visits_respiratory'] +
            item['C__O3_ER_visits_respiratory']) *
          item.TRIBES[this.selectedTribe];
        this.PMERVisitsAllRespiratory +=
          item['PM_ER_visits_respiratory'] * item.TRIBES[this.selectedTribe];
        this.PMERVisitsAllRespiratoryValue +=
          item['C__PM_ER_visits_respiratory'] * item.TRIBES[this.selectedTribe];
        this.O3ERVisitsAllRespiratory +=
          item['O3_ER_visits_respiratory'] * item.TRIBES[this.selectedTribe];
        this.O3ERVisitsAllRespiratoryValue +=
          item['C__O3_ER_visits_respiratory'] * item.TRIBES[this.selectedTribe];

        this.AsthmaSymptoms +=
          (item['PM_Asthma_Symptoms_Albuterol_use'] +
            item['O3_Asthma_Symptoms_Chest_Tightness'] +
            item['O3_Asthma_Symptoms_Cough'] +
            item['O3_Asthma_Symptoms_Wheeze'] +
            item['O3_Asthma_Symptoms_Shortness_of_Breath']) *
          item.TRIBES[this.selectedTribe];
        this.AsthmaSymptomsValue +=
          (item['C__PM_Asthma_Symptoms_Albuterol_use'] +
            item['C__O3_Asthma_Symptoms_Chest_Tightness'] +
            item['C__O3_Asthma_Symptoms_Cough'] +
            item['C__O3_Asthma_Symptoms_Wheeze'] +
            item['C__O3_Asthma_Symptoms_Shortness_of_Breath']) *
          item.TRIBES[this.selectedTribe];

        this.AlbuterolUse +=
          item['PM_Asthma_Symptoms_Albuterol_use'] *
          item.TRIBES[this.selectedTribe];
        this.AlbuterolUseValue +=
          item['C__PM_Asthma_Symptoms_Albuterol_use'] *
          item.TRIBES[this.selectedTribe];

        this.ChestTightness +=
          item['O3_Asthma_Symptoms_Chest_Tightness'] *
          item.TRIBES[this.selectedTribe];
        this.ChestTightnessValue +=
          item['C__O3_Asthma_Symptoms_Chest_Tightness'] *
          item.TRIBES[this.selectedTribe];

        this.Cough +=
          item['O3_Asthma_Symptoms_Cough'] * item.TRIBES[this.selectedTribe];
        this.CoughValue +=
          item['C__O3_Asthma_Symptoms_Cough'] * item.TRIBES[this.selectedTribe];

        this.ShortnessOfBreath +=
          item['O3_Asthma_Symptoms_Shortness_of_Breath'] *
          item.TRIBES[this.selectedTribe];
        this.ShortnessOfBreathValue +=
          item['C__O3_Asthma_Symptoms_Shortness_of_Breath'] *
          item.TRIBES[this.selectedTribe];

        this.Wheeze +=
          item['O3_Asthma_Symptoms_Wheeze'] * item.TRIBES[this.selectedTribe];
        this.WheezeValue +=
          item['C__O3_Asthma_Symptoms_Wheeze'] *
          item.TRIBES[this.selectedTribe];

        this.SchoolLossDays +=
          item['O3_School_Loss_Days'] * item.TRIBES[this.selectedTribe];
        this.SchoolLossDaysValue +=
          item['C__O3_School_Loss_Days'] * item.TRIBES[this.selectedTribe];
      }
    });
  }
  // ✅ Delays hiding dropdown to allow click event to register
  hideDropdownWithDelay() {
    setTimeout(() => {
      this.showTribeDropdown = false;
    }, 200);
  }

  getResultsTitle() {
    if (this.selectedTribe) {
      return `${this.selectedTribe}`;
    }
    return this.tableStates[this.selectedTableState];
  }

  // <--------------------------- Updates county dropdown once changing selection in state dropdown --------------------------->
  updateCountyDropDownAndFilterVal(index: any) {
    this.tribeSearchInput = '';
    this.selectedTribe = '';

    var county_dropdown = document.getElementById('county_dd');
    var info_text_table = document.getElementById('info_text_table');
    if (index == '') {
      this.counties_for_state = [];
      county_dropdown.setAttribute('disabled', '');
      this.filtervalue = '00';
      this.selectedTableState = 'all state';
      this.showTableDataForAllStates();
      info_text_table.removeAttribute('hidden');
    } else {
      this.counties_for_state = this.state_clr_structure[index].counties;
      county_dropdown.removeAttribute('disabled');
      this.filtervalue = this.state_clr_structure[index].STFIPS;
      this.tableStates['selected state'] =
        this.state_clr_structure[index].STNAME;
      this.selectedTableState = 'selected state';
      this.filterDataForStateSelection();
      info_text_table.setAttribute('hidden', 'true');
    }
    var countyValue = '';
    this.showHideStateCountyNameAndUpdateFilterVal(index, countyValue);
  }
  // <------------------------- Updates county dropdown once changing selection in state dropdown/End ------------------------->

  // <------------------------------------------- Filters data for county selection ------------------------------------------->
  twoSigFigs(num: number | string, currency: boolean) {
    num = parseFloat(`${num}`);
    if (num === 0) return '0';

    // Adjust number to two significant digits
    let final = Number(num.toPrecision(2));

    // Handle small numbers prone to floating-point imprecision
    if (Math.abs(final) < 1 && Math.abs(final) > 0) {
      let strFinal = final.toString();
      // regex to capture any zeros and the first two significant digits after decimal
      const regexMatch = strFinal.match(/^0?\.(0*[1-9][0-9]?)/);
      if (regexMatch) {
        final = parseFloat('0.' + regexMatch[1]);
      }
    }

    // Formatting final output
    if (currency) {
      return new Intl.NumberFormat('en-US', {
        style: 'currency',
        currency: 'USD',
        maximumFractionDigits: 2,
        minimumFractionDigits: 0,
      }).format(final);
    } else {
      return final.toString();
    }
  }

  filterDataForCountySelection() {
    if (!this.items) return;
    console.log(
      'IN FILTER DATA FOR COUNTY SELECTION FILTERING ITEMS WITH FIPS ===',
      this.filtervalue
    );
    let foundMatch = false;
    this.items.map((item) => {
      if (item.FIPS == this.filtervalue && !foundMatch) {
        //first time we find a matching county - we want to reset the values
        foundMatch = true;
        this.TotalHealthBenefitsValue_high =
          item['C__Total_Health_Benefits_High_Value'];
        this.TotalHealthBenefitsValue_low =
          item['C__Total_Health_Benefits_Low_Value'];
        this.TotalPM_high = item['C__Total_PM_High_Value'];
        this.TotalPM_low = item['C__Total_PM_Low_Value'];
        this.TotalO3 = item['C__Total_O3_Value'];
        this.Mortality_low =
          item['PM_Mortality_All_Cause__low_'] +
          item['O3_Mortality_Longterm_exposure'] +
          item['O3_Mortality_Shortterm_exposure'];
        this.Mortality_high =
          item['PM_Mortality_All_Cause__high_'] +
          item['O3_Mortality_Longterm_exposure'] +
          item['O3_Mortality_Shortterm_exposure'];
        this.PM_Mortality_high = item['PM_Mortality_All_Cause__high_'];
        this.PM_MortalityValue_high = item['C__PM_Mortality_All_Cause__high_'];
        this.PM_Mortality_low = item['PM_Mortality_All_Cause__low_'];
        this.PM_MortalityValue_low = item['C__PM_Mortality_All_Cause__low_'];
        this.O3_Mortality_long = item['O3_Mortality_Longterm_exposure'];
        this.O3_MortalityValue_long = item['C__O3_Mortality_Longterm_exposure'];
        this.O3_Mortality_short = item['O3_Mortality_Shortterm_exposure'];
        this.O3_MortalityValue_short =
          item['C__O3_Mortality_Shortterm_exposure'];

        this.NonfatalHeartAttacks =
          item['PM_Acute_Myocardial_Infarction_Nonfatal'];

        this.InfantMortality = item['PM_Infant_Mortality'];

        this.HospitalAdmitsAllRespiratory =
          item['PM_HA_All_Respiratory'] +
          item['PM_HA_Respiratory2'] +
          item['O3_HA_All_Respiratory'];
        this.PMHospitalAdmitsAllRespiratory =
          item['PM_HA_All_Respiratory'] + item['PM_HA_Respiratory2'];
        this.PMHospitalAdmitsAllRespiratoryValue =
          item['C__PM_Resp_Hosp_Adm'] + item['C__PM_HA_Respiratory2'];

        this.O3HospitalAdmitsAllRespiratory = item['O3_HA_All_Respiratory'];
        this.O3HospitalAdmitsAllRespiratoryValue =
          item['C__O3_HA_All_Respiratory'];

        this.EmergencyRoomVisitsAsthma = item['O3_ER_Visits_Asthma'];
        this.MinorRestrictedActivityDays =
          item['PM_Minor_Restricted_Activity_Days'];
        this.WorkLossDays = item['PM_Work_Loss_Days'];
        this.MortalityValue_low =
          item['C__PM_Mortality_All_Cause__low_'] +
          item['C__O3_Mortality_Longterm_exposure'] +
          item['C__O3_Mortality_Shortterm_exposure'];
        this.MortalityValue_high =
          item['C__PM_Mortality_All_Cause__high_'] +
          item['C__O3_Mortality_Longterm_exposure'] +
          item['C__O3_Mortality_Shortterm_exposure'];
        this.NonfatalHeartAttacksValue =
          item['C__PM_Acute_Myocardial_Infarction_Nonfatal'];

        this.InfantMortalityValue = item['C__PM_Infant_Mortality'];
        this.HospitalAdmitsAllRespiratoryValue =
          item['C__PM_Resp_Hosp_Adm'] +
          item['C__O3_HA_All_Respiratory'] +
          item['C__PM_HA_Respiratory2'];
        this.EmergencyRoomVisitsAsthmaValue = item['C__O3_ER_Visits_Asthma'];
        this.MinorRestrictedActivityDaysValue =
          item['C__PM_Minor_Restricted_Activity_Days'];
        this.WorkLossDaysValue = item['C__PM_Work_Loss_Days'];

        //new 2023 health endpoints
        this.IncidenceLungCancer = item['PM_Incidence_Lung_Cancer'];
        this.IncidenceLungCancerValue = item['C__PM_Incidence_Lung_Cancer'];

        this.HA_HCCPV_Disease =
          item['PM_HA_Cardio_Cerebro_and_Peripheral_Vascular_Disease'];
        this.HA_HCCPV_DiseaseValue =
          item['C__PM_HA_Cardio_Cerebro_and_Peripheral_Vascular_Disease'];

        this.HospitalAdmitsParkinsonsDisease = item['PM_HA_Parkinsons_Disease'];
        this.HospitalAdmitsParkinsonsDiseaseValue =
          item['C__PM_HA_Parkinsons_Disease'];

        this.HospitalAdmitsAlzheimersDisease = item['PM_HA_Alzheimers_Disease'];
        this.HospitalAdmitsAlzheimersDiseaseValue =
          item['C__PM_HA_Alzheimers_Disease'];

        this.IncidenceStroke = item['PM_Incidence_Stroke'];
        this.IncidenceStrokeValue = item['C__PM_Incidence_Stroke'];

        this.IncidenceOutOfHospitalCardiacArrest =
          item['PM_Incidence_Out_of_Hospital_Cardiac_Arrest'];
        this.IncidenceOutOfHospitalCardiacArrestValue =
          item['C__PM_Incidence_Out_of_Hospital_Cardiac_Arrest'];

        this.IncidenceAsthma =
          item['PM_Incidence_Asthma'] + item['O3_Incidence_Asthma'];
        this.IncidenceAsthmaValue =
          item['C__PM_Incidence_Asthma'] + item['C__O3_Incidence_Asthma'];
        this.PMIncidenceAsthma = item['PM_Incidence_Asthma'];
        this.PMIncidenceAsthmaValue = item['C__PM_Incidence_Asthma'];
        this.O3IncidenceAsthma = item['O3_Incidence_Asthma'];
        this.O3IncidenceAsthmaValue = item['C__O3_Incidence_Asthma'];

        this.IncidenceHayFeverRhinitis =
          item['PM_Incidence_Hay_Fever_Rhinitis'] +
          item['O3_Incidence_Hay_Fever_Rhinitis'];
        this.IncidenceHayFeverRhinitisValue =
          item['C__PM_Incidence_Hay_Fever_Rhinitis'] +
          item['C__O3_Incidence_Hay_Fever_Rhinitis'];

        this.PMIncidenceHayFeverRhinitis =
          item['PM_Incidence_Hay_Fever_Rhinitis'];

        this.PMIncidenceHayFeverRhinitisValue =
          item['C__PM_Incidence_Hay_Fever_Rhinitis'];
        this.O3IncidenceHayFeverRhinitis =
          item['O3_Incidence_Hay_Fever_Rhinitis'];

        this.O3IncidenceHayFeverRhinitisValue =
          item['C__O3_Incidence_Hay_Fever_Rhinitis'];

        this.ERVisitsAllCardiacOutcomes =
          item['PM_ER_visits_All_Cardiac_Outcomes'];
        this.ERVisitsAllCardiacOutcomesValue =
          item['C__PM_ER_visits_All_Cardiac_Outcomes'];

        this.ERVisitsAllRespiratory =
          item['PM_ER_visits_respiratory'] + item['O3_ER_visits_respiratory'];
        this.ERVisitsAllRespiratoryValue =
          item['C__PM_ER_visits_respiratory'] +
          item['C__O3_ER_visits_respiratory'];
        this.PMERVisitsAllRespiratory = item['PM_ER_visits_respiratory'];
        this.PMERVisitsAllRespiratoryValue =
          item['C__PM_ER_visits_respiratory'];
        this.O3ERVisitsAllRespiratory = item['O3_ER_visits_respiratory'];
        this.O3ERVisitsAllRespiratoryValue =
          item['C__O3_ER_visits_respiratory'];

        this.AsthmaSymptoms =
          item['PM_Asthma_Symptoms_Albuterol_use'] +
          item['O3_Asthma_Symptoms_Chest_Tightness'] +
          item['O3_Asthma_Symptoms_Cough'] +
          item['O3_Asthma_Symptoms_Wheeze'] +
          item['O3_Asthma_Symptoms_Shortness_of_Breath'];
        this.AsthmaSymptomsValue =
          item['C__PM_Asthma_Symptoms_Albuterol_use'] +
          item['C__O3_Asthma_Symptoms_Chest_Tightness'] +
          item['C__O3_Asthma_Symptoms_Cough'] +
          item['C__O3_Asthma_Symptoms_Wheeze'] +
          item['C__O3_Asthma_Symptoms_Shortness_of_Breath'];

        this.AlbuterolUse = item['PM_Asthma_Symptoms_Albuterol_use'];
        this.AlbuterolUseValue = item['C__PM_Asthma_Symptoms_Albuterol_use'];

        this.ChestTightness = item['O3_Asthma_Symptoms_Chest_Tightness'];
        this.ChestTightnessValue =
          item['C__O3_Asthma_Symptoms_Chest_Tightness'];

        this.Cough = item['O3_Asthma_Symptoms_Cough'];
        this.CoughValue = item['C__O3_Asthma_Symptoms_Cough'];

        this.ShortnessOfBreath = item['O3_Asthma_Symptoms_Shortness_of_Breath'];
        this.ShortnessOfBreathValue =
          item['C__O3_Asthma_Symptoms_Shortness_of_Breath'];

        this.Wheeze = item['O3_Asthma_Symptoms_Wheeze'];
        this.WheezeValue = item['C__O3_Asthma_Symptoms_Wheeze'];

        this.SchoolLossDays = item['O3_School_Loss_Days'];
        this.SchoolLossDaysValue = item['C__O3_School_Loss_Days'];
      } else if (item.FIPS == this.filtervalue && foundMatch) {
        // once values have been reset start adding values of subsequent matching county items
        this.TotalHealthBenefitsValue_high +=
          item['C__Total_Health_Benefits_High_Value'];
        this.TotalHealthBenefitsValue_low +=
          item['C__Total_Health_Benefits_Low_Value'];

        this.TotalPM_high += item['C__Total_PM_High_Value'];
        this.TotalPM_low += item['C__Total_PM_Low_Value'];
        this.TotalO3 += item['C__Total_O3_Value'];
        this.Mortality_low +=
          item['PM_Mortality_All_Cause__low_'] +
          item['O3_Mortality_Longterm_exposure'] +
          item['O3_Mortality_Shortterm_exposure'];
        this.Mortality_high +=
          item['PM_Mortality_All_Cause__high_'] +
          item['O3_Mortality_Longterm_exposure'] +
          item['O3_Mortality_Shortterm_exposure'];
        this.PM_Mortality_high += item['PM_Mortality_All_Cause__high_'];
        this.PM_MortalityValue_high += item['C__PM_Mortality_All_Cause__high_'];
        this.PM_Mortality_low += item['PM_Mortality_All_Cause__low_'];
        this.PM_MortalityValue_low += item['C__PM_Mortality_All_Cause__low_'];
        this.O3_Mortality_long += item['O3_Mortality_Longterm_exposure'];
        this.O3_MortalityValue_long +=
          item['C__O3_Mortality_Longterm_exposure'];
        this.O3_Mortality_short += item['O3_Mortality_Shortterm_exposure'];
        this.O3_MortalityValue_short +=
          item['C__O3_Mortality_Shortterm_exposure'];

        this.NonfatalHeartAttacks +=
          item['PM_Acute_Myocardial_Infarction_Nonfatal'];

        this.InfantMortality += item['PM_Infant_Mortality'];

        this.HospitalAdmitsAllRespiratory +=
          item['PM_HA_All_Respiratory'] +
          item['PM_HA_Respiratory2'] +
          item['O3_HA_All_Respiratory'];
        this.PMHospitalAdmitsAllRespiratory +=
          item['PM_HA_All_Respiratory'] + item['PM_HA_Respiratory2'];
        this.PMHospitalAdmitsAllRespiratoryValue +=
          item['C__PM_Resp_Hosp_Adm'] + item['C__PM_HA_Respiratory2'];

        this.O3HospitalAdmitsAllRespiratory += item['O3_HA_All_Respiratory'];
        this.O3HospitalAdmitsAllRespiratoryValue +=
          item['C__O3_HA_All_Respiratory'];

        this.EmergencyRoomVisitsAsthma += item['O3_ER_Visits_Asthma'];
        this.MinorRestrictedActivityDays +=
          item['PM_Minor_Restricted_Activity_Days'];
        this.WorkLossDays += item['PM_Work_Loss_Days'];
        this.MortalityValue_low +=
          item['C__PM_Mortality_All_Cause__low_'] +
          item['C__O3_Mortality_Longterm_exposure'] +
          item['C__O3_Mortality_Shortterm_exposure'];
        this.MortalityValue_high +=
          item['C__PM_Mortality_All_Cause__high_'] +
          item['C__O3_Mortality_Longterm_exposure'] +
          item['C__O3_Mortality_Shortterm_exposure'];
        this.NonfatalHeartAttacksValue +=
          item['C__PM_Acute_Myocardial_Infarction_Nonfatal'];

        this.InfantMortalityValue += item['C__PM_Infant_Mortality'];
        this.HospitalAdmitsAllRespiratoryValue +=
          item['C__PM_Resp_Hosp_Adm'] +
          item['C__O3_HA_All_Respiratory'] +
          item['C__PM_HA_Respiratory2'];
        this.EmergencyRoomVisitsAsthmaValue += item['C__O3_ER_Visits_Asthma'];
        this.MinorRestrictedActivityDaysValue +=
          item['C__PM_Minor_Restricted_Activity_Days'];
        this.WorkLossDaysValue += item['C__PM_Work_Loss_Days'];

        //new 2023 health endpoints
        this.IncidenceLungCancer += item['PM_Incidence_Lung_Cancer'];
        this.IncidenceLungCancerValue += item['C__PM_Incidence_Lung_Cancer'];

        this.HA_HCCPV_Disease +=
          item['PM_HA_Cardio_Cerebro_and_Peripheral_Vascular_Disease'];
        this.HA_HCCPV_DiseaseValue +=
          item['C__PM_HA_Cardio_Cerebro_and_Peripheral_Vascular_Disease'];

        this.HospitalAdmitsParkinsonsDisease +=
          item['PM_HA_Parkinsons_Disease'];
        this.HospitalAdmitsParkinsonsDiseaseValue +=
          item['C__PM_HA_Parkinsons_Disease'];

        this.HospitalAdmitsAlzheimersDisease +=
          item['PM_HA_Alzheimers_Disease'];
        this.HospitalAdmitsAlzheimersDiseaseValue +=
          item['C__PM_HA_Alzheimers_Disease'];

        this.IncidenceStroke += item['PM_Incidence_Stroke'];
        this.IncidenceStrokeValue += item['C__PM_Incidence_Stroke'];

        this.IncidenceOutOfHospitalCardiacArrest +=
          item['PM_Incidence_Out_of_Hospital_Cardiac_Arrest'];
        this.IncidenceOutOfHospitalCardiacArrestValue +=
          item['C__PM_Incidence_Out_of_Hospital_Cardiac_Arrest'];

        this.IncidenceAsthma +=
          item['PM_Incidence_Asthma'] + item['O3_Incidence_Asthma'];
        this.IncidenceAsthmaValue +=
          item['C__PM_Incidence_Asthma'] + item['C__O3_Incidence_Asthma'];
        this.PMIncidenceAsthma += item['PM_Incidence_Asthma'];
        this.PMIncidenceAsthmaValue += item['C__PM_Incidence_Asthma'];
        this.O3IncidenceAsthma += item['O3_Incidence_Asthma'];
        this.O3IncidenceAsthmaValue += item['C__O3_Incidence_Asthma'];

        this.IncidenceHayFeverRhinitis +=
          item['PM_Incidence_Hay_Fever_Rhinitis'] +
          item['O3_Incidence_Hay_Fever_Rhinitis'];
        this.IncidenceHayFeverRhinitisValue +=
          item['C__PM_Incidence_Hay_Fever_Rhinitis'] +
          item['C__O3_Incidence_Hay_Fever_Rhinitis'];

        this.PMIncidenceHayFeverRhinitis +=
          item['PM_Incidence_Hay_Fever_Rhinitis'];

        this.PMIncidenceHayFeverRhinitisValue +=
          item['C__PM_Incidence_Hay_Fever_Rhinitis'];
        this.O3IncidenceHayFeverRhinitis +=
          item['O3_Incidence_Hay_Fever_Rhinitis'];

        this.O3IncidenceHayFeverRhinitisValue +=
          item['C__O3_Incidence_Hay_Fever_Rhinitis'];

        this.ERVisitsAllCardiacOutcomes +=
          item['PM_ER_visits_All_Cardiac_Outcomes'];
        this.ERVisitsAllCardiacOutcomesValue +=
          item['C__PM_ER_visits_All_Cardiac_Outcomes'];

        this.ERVisitsAllRespiratory +=
          item['PM_ER_visits_respiratory'] + item['O3_ER_visits_respiratory'];
        this.ERVisitsAllRespiratoryValue += item[
          'C__PM_ER_visits_respiratory'
        ] += item['C__O3_ER_visits_respiratory'];
        this.PMERVisitsAllRespiratory += item['PM_ER_visits_respiratory'];
        this.PMERVisitsAllRespiratoryValue +=
          item['C__PM_ER_visits_respiratory'];
        this.O3ERVisitsAllRespiratory += item['O3_ER_visits_respiratory'];
        this.O3ERVisitsAllRespiratoryValue +=
          item['C__O3_ER_visits_respiratory'];

        this.AsthmaSymptoms +=
          item['PM_Asthma_Symptoms_Albuterol_use'] +
          item['O3_Asthma_Symptoms_Chest_Tightness'] +
          item['O3_Asthma_Symptoms_Cough'] +
          item['O3_Asthma_Symptoms_Wheeze'] +
          item['O3_Asthma_Symptoms_Shortness_of_Breath'];
        this.AsthmaSymptomsValue +=
          item['C__PM_Asthma_Symptoms_Albuterol_use'] +
          item['C__O3_Asthma_Symptoms_Chest_Tightness'] +
          item['C__O3_Asthma_Symptoms_Cough'] +
          item['C__O3_Asthma_Symptoms_Wheeze'] +
          item['C__O3_Asthma_Symptoms_Shortness_of_Breath'];

        this.AlbuterolUse += item['PM_Asthma_Symptoms_Albuterol_use'];
        this.AlbuterolUseValue += item['C__PM_Asthma_Symptoms_Albuterol_use'];

        this.ChestTightness += item['O3_Asthma_Symptoms_Chest_Tightness'];
        this.ChestTightnessValue +=
          item['C__O3_Asthma_Symptoms_Chest_Tightness'];

        this.Cough += item['O3_Asthma_Symptoms_Cough'];
        this.CoughValue += item['C__O3_Asthma_Symptoms_Cough'];

        this.ShortnessOfBreath +=
          item['O3_Asthma_Symptoms_Shortness_of_Breath'];
        this.ShortnessOfBreathValue +=
          item['C__O3_Asthma_Symptoms_Shortness_of_Breath'];

        this.Wheeze += item['O3_Asthma_Symptoms_Wheeze'];
        this.WheezeValue += item['C__O3_Asthma_Symptoms_Wheeze'];

        this.SchoolLossDays += item['O3_School_Loss_Days'];
        this.SchoolLossDaysValue += item['C__O3_School_Loss_Days'];
      }
    }); //end of detailed items map
  }
  // <----------------------------------------- Filters data for county selection/End ----------------------------------------->

  // <----------------------------------------- Shows and hides state and county names ---------------------------------------->
  showHideStateCountyNameAndUpdateFilterVal(index: any, countyValue: any) {
    if (index != '' && countyValue == '') {
      this.selectedTableState = 'selected state';
      this.filtervalue = this.state_clr_structure[index].STFIPS;
      this.filterDataForStateSelection();
    }
    if (countyValue != '') {
      this.countyFIPS = countyValue.substr(countyValue.length - 5);
      this.countyName = countyValue.substr(0, countyValue.length - 5);
      this.tableStates['selected county'] =
        this.countyName + ', ' + this.tableStates['selected state'];
      this.selectedTableState = 'selected county';
      this.filtervalue = this.countyFIPS;
      this.filterDataForCountySelection();
    }
  }

  // <--------------------------------------- Shows and hides state and county names/End -------------------------------------->

  // <--------------------------------- Calls resultspanelToReviewpanelRetrievedResultsEmitter -------------------------------->
  emitFromResultspanelToReviewpanelRetrievedResults() {
    this.resultspanelToReviewpanelRetrievedResultsEmitter.emit(null);
  }
  // <------------------------------- Calls resultspanelToReviewpanelRetrievedResultsEmitter/End ------------------------------>

  // <-------------------------------------------------- expand(id) function which accesses id value of dict to toggle whether optional/detail rows are displayed ------------------------------------------------>
  expand(id: string): void {
    if (this.showBreakdown.hasOwnProperty(id)) {
      this.showBreakdown[id] = !this.showBreakdown[id];
    }
  }

  // <-------------------------------------------------- getResults() function ------------------------------------------------>
  getResults(): void {
    // var heartbeat = document.getElementById("heartbeat");
    // heartbeat.removeAttribute("hidden");
    var results_panel_content = document.getElementById(
      'results_panel_content'
    );
    results_panel_content.setAttribute('hidden', 'true');
    this.showHeartbeat = true;
    // this.showResultsPanelContent = false;

    this.items = [];
    this.summary = [];

    this.cobraDataService
      .getResults(this.filtervalue, this.discountRate)
      .subscribe(
        (data) => {
          console.log('API RESULTS ARE:', data);
          this.items = data['Impacts'];

          console.log(
            'ITEMS WITH TRIBES:',
            this.items.filter((i) => Object.keys(i.TRIBES).length > 0)
          );

          console.log('looping through items to get county summaries');
          //make life easier and add summaries to this.items for each county so that each county has summary values
          this.items = this.items.map((countyData) => {
            //then we also just want "sumaries" for just the O3 and PM specific health endpoints so just splice out either "PM_" or "O3_" from the endpoint name
            let specificEndpointSummaries = {};
            for (const key in countyData) {
              if (
                key !== 'PM_HA_All_Respiratory' &&
                key !== 'O3_HA_All_Respiratory' &&
                key !== 'PM_HA_Respiratory2' &&
                key !== 'C__PM_Resp_Hosp_Adm' &&
                key !== 'C__O3_HA_All_Respiratory' &&
                key !== 'C__PM_HA_Respiratory2' &&
                !key.toUpperCase().includes('MORTALITY_ALL') &&
                !key.toUpperCase().includes('EXPOSURE') &&
                !key.includes('Asthma_Symptoms') &&
                !key.includes('Rhinitis') &&
                !key.includes('Incidence_Asthma') &&
                !key.includes('ER_visits_respiratory') &&
                key !== 'C__Total_Health_Benefits_High_Value' &&
                key !== 'C__Total_Health_Benefits_Low_Value'
              ) {
                const newKey = key.replace(/(PM_|O3_)/g, '');
                specificEndpointSummaries[newKey] = countyData[key];
              }
            }

            return {
              ...countyData,
              ...specificEndpointSummaries,
              //aggregated values
              HA_All_Respiratory:
                countyData['PM_HA_All_Respiratory'] +
                countyData['O3_HA_All_Respiratory'] +
                countyData['PM_HA_Respiratory2'],
              C__HA_All_Respiratory:
                countyData['C__PM_Resp_Hosp_Adm'] +
                countyData['C__O3_HA_All_Respiratory'] +
                countyData['C__PM_HA_Respiratory2'],
              Mortality_All_Cause__low_:
                countyData['PM_Mortality_All_Cause__low_'] +
                countyData['O3_Mortality_Longterm_exposure'] +
                countyData['O3_Mortality_Shortterm_exposure'],
              C__Mortality_All_Cause__low_:
                countyData['C__PM_Mortality_All_Cause__low_'] +
                countyData['C__O3_Mortality_Longterm_exposure'] +
                countyData['C__O3_Mortality_Shortterm_exposure'],
              Mortality_All_Cause__high_:
                countyData['PM_Mortality_All_Cause__high_'] +
                countyData['O3_Mortality_Longterm_exposure'] +
                countyData['O3_Mortality_Shortterm_exposure'],
              C__Mortality_All_Cause__high_:
                countyData['C__PM_Mortality_All_Cause__high_'] +
                countyData['C__O3_Mortality_Longterm_exposure'] +
                countyData['C__O3_Mortality_Shortterm_exposure'],
              Asthma_Symptoms:
                countyData['PM_Asthma_Symptoms_Albuterol_use'] +
                countyData['O3_Asthma_Symptoms_Chest_Tightness'] +
                countyData['O3_Asthma_Symptoms_Shortness_of_Breath'] +
                countyData['O3_Asthma_Symptoms_Wheeze'] +
                countyData['O3_Asthma_Symptoms_Cough'],
              C__Asthma_Symptoms:
                countyData['C__PM_Asthma_Symptoms_Albuterol_use'] +
                countyData['C__O3_Asthma_Symptoms_Chest_Tightness'] +
                countyData['C__O3_Asthma_Symptoms_Shortness_of_Breath'] +
                countyData['C__O3_Asthma_Symptoms_Wheeze'] +
                countyData['C__O3_Asthma_Symptoms_Cough'],
              Incidence_Hay_Fever_Rhinitis:
                countyData['PM_Incidence_Hay_Fever_Rhinitis'] +
                countyData['O3_Incidence_Hay_Fever_Rhinitis'],
              C__Incidence_Hay_Fever_Rhinitis:
                countyData['C__PM_Incidence_Hay_Fever_Rhinitis'] +
                countyData['C__O3_Incidence_Hay_Fever_Rhinitis'],
              Incidence_Asthma:
                countyData['PM_Incidence_Asthma'] +
                countyData['O3_Incidence_Asthma'],
              C__Incidence_Asthma:
                countyData['C__PM_Incidence_Asthma'] +
                countyData['C__O3_Incidence_Asthma'],
              ER_visits_respiratory:
                countyData['PM_ER_visits_respiratory'] +
                countyData['O3_ER_visits_respiratory'],
              C__ER_visits_respiratory:
                countyData['C__PM_ER_visits_respiratory'] +
                countyData['C__O3_ER_visits_respiratory'],
            };
          });
          console.log('done loop');
          this.summary = data['Summary'];
          this.TotalHealthBenefitsValue_high =
            this.summary['TotalHealthBenefitsValue_high'];
          this.TotalHealthBenefitsValue_low =
            this.summary['TotalHealthBenefitsValue_low'];
          this.TotalPM_high = this.summary['TotalPMValue_high'];
          this.TotalPM_low = this.summary['TotalPMValue_low'];
          this.TotalO3 = this.summary['TotalO3Value'];
          this.Mortality_low = this.summary['Mortality_All_Cause__low_'];
          this.Mortality_high = this.summary['Mortality_All_Cause__high_'];
          this.PM_MortalityValue_high =
            this.summary['C__PM_Mortality_All_Cause__high_'];
          this.PM_MortalityValue_low =
            this.summary['C__PM_Mortality_All_Cause__low_'];
          this.PM_Mortality_high =
            this.summary['PM_Mortality_All_Cause__high_'];
          this.PM_Mortality_low = this.summary['PM_Mortality_All_Cause__low_'];
          this.O3_Mortality_long =
            this.summary['O3_Mortality_Longterm_exposure'];
          this.O3_Mortality_short =
            this.summary['O3_Mortality_Shortterm_exposure'];
          this.O3_MortalityValue_long =
            this.summary['C__O3_Mortality_Longterm_exposure'];
          this.O3_MortalityValue_short =
            this.summary['C__O3_Mortality_Shortterm_exposure'];
          this.NonfatalHeartAttacks =
            this.summary['Acute_Myocardial_Infarction_Nonfatal'];

          this.InfantMortality = this.summary['Infant_Mortality'];
          this.HospitalAdmitsAllRespiratory =
            this.summary['HA_All_Respiratory'];
          this.PMHospitalAdmitsAllRespiratory =
            this.summary['PM_HA_All_Respiratory'];
          this.PMHospitalAdmitsAllRespiratoryValue =
            this.summary['C__PM_HA_All_Respiratory'];
          this.O3HospitalAdmitsAllRespiratory =
            this.summary['O3_HA_All_Respiratory'];
          this.O3HospitalAdmitsAllRespiratoryValue =
            this.summary['C__O3_HA_All_Respiratory'];
          this.EmergencyRoomVisitsAsthma = this.summary['ER_Visits_Asthma'];
          this.MinorRestrictedActivityDays =
            this.summary['Minor_Restricted_Activity_Days'];
          this.WorkLossDays = this.summary['Work_Loss_Days'];
          this.MortalityValue_low =
            this.summary['C__Mortality_All_Cause__low_'];
          this.MortalityValue_high =
            this.summary['C__Mortality_All_Cause__high_'];
          this.NonfatalHeartAttacksValue =
            this.summary['C__Acute_Myocardial_Infarction_Nonfatal'];

          this.InfantMortalityValue = this.summary['C__Infant_Mortality'];
          this.HospitalAdmitsAllRespiratoryValue =
            this.summary['C__HA_All_Respiratory'];
          this.EmergencyRoomVisitsAsthmaValue =
            this.summary['C__ER_Visits_Asthma'];
          this.MinorRestrictedActivityDaysValue =
            this.summary['C__Minor_Restricted_Activity_Days'];

          this.IncidenceLungCancer = this.summary['Incidence_Lung_Cancer'];
          this.IncidenceLungCancerValue =
            this.summary['C__Incidence_Lung_Cancer'];

          this.HA_HCCPV_Disease =
            this.summary['HA_Cardio_Cerebro_and_Peripheral_Vascular_Disease'];
          this.HA_HCCPV_DiseaseValue =
            this.summary[
              'C__HA_Cardio_Cerebro_and_Peripheral_Vascular_Disease'
            ];

          this.HospitalAdmitsParkinsonsDisease =
            this.summary['HA_Parkinsons_Disease'];
          this.HospitalAdmitsParkinsonsDiseaseValue =
            this.summary['C__HA_Parkinsons_Disease'];

          this.HospitalAdmitsAlzheimersDisease =
            this.summary['HA_Alzheimers_Disease'];
          this.HospitalAdmitsAlzheimersDiseaseValue =
            this.summary['C__HA_Alzheimers_Disease'];

          this.IncidenceStroke = this.summary['Incidence_Stroke'];
          this.IncidenceStrokeValue = this.summary['C__Incidence_Stroke'];

          this.IncidenceOutOfHospitalCardiacArrest =
            this.summary['Incidence_Out_of_Hospital_Cardiac_Arrest'];
          this.IncidenceOutOfHospitalCardiacArrestValue =
            this.summary['C__Incidence_Out_of_Hospital_Cardiac_Arrest'];

          this.IncidenceAsthma = this.summary['Incidence_Asthma'];
          this.IncidenceAsthmaValue = this.summary['C__Incidence_Asthma'];
          this.PMIncidenceAsthma = this.summary['PM_Incidence_Asthma'];
          this.PMIncidenceAsthmaValue = this.summary['C__PM_Incidence_Asthma'];
          this.O3IncidenceAsthma = this.summary['O3_Incidence_Asthma'];
          this.O3IncidenceAsthmaValue = this.summary['C__O3_Incidence_Asthma'];

          this.IncidenceHayFeverRhinitis =
            this.summary['Incidence_Hay_Fever_Rhinitis'];
          this.IncidenceHayFeverRhinitisValue =
            this.summary['C__Incidence_Hay_Fever_Rhinitis'];
          this.PMIncidenceHayFeverRhinitis =
            this.summary['PM_Incidence_Hay_Fever_Rhinitis'];
          this.PMIncidenceHayFeverRhinitisValue =
            this.summary['C__PM_Incidence_Hay_Fever_Rhinitis'];
          this.O3IncidenceHayFeverRhinitis =
            this.summary['O3_Incidence_Hay_Fever_Rhinitis'];
          this.O3IncidenceHayFeverRhinitisValue =
            this.summary['C__O3_Incidence_Hay_Fever_Rhinitis'];

          this.ERVisitsAllCardiacOutcomes =
            this.summary['ER_visits_All_Cardiac_Outcomes'];
          this.ERVisitsAllCardiacOutcomesValue =
            this.summary['C__ER_visits_All_Cardiac_Outcomes'];

          this.ERVisitsAllRespiratory = this.summary['ER_visits_respiratory'];
          this.ERVisitsAllRespiratoryValue =
            this.summary['C__ER_visits_respiratory'];
          this.PMERVisitsAllRespiratory =
            this.summary['PM_ER_visits_respiratory'];
          this.PMERVisitsAllRespiratoryValue =
            this.summary['C__PM_ER_visits_respiratory'];
          this.O3ERVisitsAllRespiratory =
            this.summary['O3_ER_visits_respiratory'];
          this.O3ERVisitsAllRespiratoryValue =
            this.summary['C__O3_ER_visits_respiratory'];

          this.AsthmaSymptoms = this.summary['Asthma_Symptoms'];
          this.AsthmaSymptomsValue = this.summary['C__Asthma_Symptoms'];
          this.AlbuterolUse = this.summary['PM_Asthma_Symptoms_Albuterol_use'];
          this.AlbuterolUseValue =
            this.summary['C__PM_Asthma_Symptoms_Albuterol_use'];
          this.Cough = this.summary['O3_Asthma_Symptoms_Cough'];
          this.CoughValue = this.summary['C__O3_Asthma_Symptoms_Cough'];
          this.ChestTightness =
            this.summary['O3_Asthma_Symptoms_Chest_Tightness'];
          this.ChestTightnessValue =
            this.summary['C__O3_Asthma_Symptoms_Chest_Tightness'];
          this.ShortnessOfBreath =
            this.summary['O3_Asthma_Symptoms_Shortness_of_Breath'];
          this.ShortnessOfBreathValue =
            this.summary['C__O3_Asthma_Symptoms_Shortness_of_Breath'];
          this.Wheeze = this.summary['O3_Asthma_Symptoms_Wheeze'];
          this.WheezeValue = this.summary['C__O3_Asthma_Symptoms_Wheeze'];

          this.SchoolLossDays = this.summary['School_Loss_Days'];
          this.SchoolLossDaysValue = this.summary['C__School_Loss_Days'];

          this.WorkLossDaysValue = this.summary['C__Work_Loss_Days'];

          //push results data into geojson for map
          //let feature = county_data.features;

          //group census tract items by county
          // Group items by FIPS
          const groupedByFIPS = this.items.reduce((acc, item) => {
            const fips = item.FIPS;
            if (!acc[fips]) {
              acc[fips] = [];
            }
            acc[fips].push(item);
            return acc;
          }, {});

          this.groupedByFIPS = groupedByFIPS;

          console.log('done 2nd items loop....');
          // Excluded fields (not summed)
          const excludedFields = [
            'ID',
            'destindx',
            'tract_id',
            'FIPS',
            'STATE',
            'COUNTY',
            'IRA_fraction',
            'CJEST',
          ];

          console.log('items reduce');
          this.itemLookup = this.items?.reduce((acc, item) => {
            acc[item.tract_id] = item;
            return acc;
          }, {});
          this.showHeartbeat = false;
          this.emitFromResultspanelToReviewpanelRetrievedResults();
          results_panel_content.removeAttribute('hidden');

          console.log('configuring county features');
          // Process the features
          county_data.features.forEach((feature) => {
            const fips = feature.properties.GEOID;
            if (groupedByFIPS[fips]) {
              const summedData = this.sumProperties(
                groupedByFIPS[fips],
                excludedFields
              );
              feature.properties.DATA = summedData;
            } else {
              feature.properties.DATA = {}; // No matching data found
            }
          });

          console.log('adding county, tracts and states layers');

          /******add API data to full tract data for mapping*/
          this.full_tract_data.features = this.full_tract_data.features.map(
            (feature) => {
              let newFeature = feature;
              if (this.items) {
                const tract = this.itemLookup[feature.properties.GEOID] || null;
                if (tract) {
                  newFeature = {
                    ...feature,
                    properties: { ...feature.properties, DATA: tract },
                  };
                }
              }

              return newFeature;
            }
          );
          /******add API data to full tract data for mapping*/
          this.full_tract_data_complex.features =
            this.full_tract_data_complex.features.map((feature) => {
              let newFeature = feature;
              if (this.items) {
                const tract = this.itemLookup[feature.properties.GEOID] || null;
                if (tract) {
                  newFeature = {
                    ...feature,
                    properties: { ...feature.properties, DATA: tract },
                  };
                }
              }

              return newFeature;
            });
          //pre calculate limits
          this.precomputeChoroplethBreaks(this.full_tract_data, true);
          /***** */

          /*** add county layer to map */
          this.countyLayer = L.geoJSON(county_data);
          if (!this.map.hasLayer(this.countyLayer)) {
            this.map.addLayer(this.countyLayer);
            this.map.addLayer(this.statesLayer);
            console.log('precomputing choropleth breaks for county data');
            this.precomputeChoroplethBreaks(county_data);
            console.log(
              'styling map with limits:',
              this.globalChoroplethLimits
            );
            this.styleMap(this.selectedMapLayer);

            //add geojson data layer to the map

            if (
              this.map.hasLayer(this.tractsLayer) &&
              (this.full_tract_data || this.full_tract_data_complex)
            ) {
              console.log(
                'styling map with limits:',
                this.globalChoroplethLimits
              );
              const zoom = this.map.getZoom();
              if (zoom >= 9.6)
                this.styleMap(
                  this.selectedMapLayer,
                  this.full_tract_data_complex
                );
              else this.styleMap(this.selectedMapLayer, this.full_tract_data);
            }
            this.centerMap(this.map);
          }

          // makes sure the map is sized properly
          this.map.invalidateSize();
        },
        (err) => {
          console.error('An error occured retrieving results: ' + err);
          alert('An error occured retrieving results: ' + err);
        },
        () => {
          // console.log('setting showheartbeat to false')
          // heartbeat.setAttribute("hidden", "true");
          //this.showHeartbeat = false;
          //this.emitFromResultspanelToReviewpanelRetrievedResults();
          //results_panel_content.removeAttribute('hidden');
          // makes sure the map is sized properly
          //this.map.invalidateSize();
          // this.showResultsPanelContent = true;
        }
      );
  }

  // Function to sum numerical fields and include excluded fields without summing
  sumProperties(items, excludedFields) {
    const summed = {};

    // Include excluded fields from the first item
    const firstItem = items[0];
    excludedFields.forEach((field) => {
      if (field in firstItem) {
        summed[field] = firstItem[field];
      }
    });


    // Sum numerical fields
    items.forEach((item) => {
      Object.keys(item).forEach((key) => {
        if (!excludedFields.includes(key) && typeof item[key] === 'number') {
          if (!summed[key]) {
            summed[key] = 0;
          }
          summed[key] += item[key];
        }
      });
    });

    return summed;
  }

  // <------------------------------------------------ getResults() function/End ---------------------------------------------->

  // <-------------------------------------------------- updates results panel ------------------------------------------------>
  updateResultsPanelAfterAllComponentsRemoved() {
    // var no_results_screen = document.getElementById("no-results-screen");
    // var pending_results_screen = document.getElementById("pending-results-screen-id");
    // no_results_screen.removeAttribute("hidden");
    // pending_results_screen.setAttribute("hidden", "true");
    var results_screen = document.getElementById('results-screen');
    results_screen.setAttribute('hidden', 'true');
    if (this.map.hasLayer(this.countyLayer)) {
      this.map.removeLayer(this.countyLayer);
      this.centerMap(this.map);
    }
    this.showPendingResultsScreen = false;
    this.showNoResultsScreen = true;
    // this.showResultsScreen = false;
  }
  // <------------------------------------------------ updates results panel/End ---------------------------------------------->

  // <--------------------------------------------------- exportAll() function ------------------------------------------------>
  allResultsExcelExport(kind: any, data: any) {
    var filename = 'CobraDetailedResults.csv';
    if (kind == 'base') {
      filename = 'BaselineEmissions.xls';
    }
    if (kind == 'control') {
      filename = 'ControlEmissions.xls';
    }
    saveAs(data, filename);
  }

  exportAll() {
    this.showAllResultsBtn = false;
    this.cobraDataService
      .exportAllResults('results', this.discountRate)
      .subscribe((data) => {
        this.allResultsExcelExport('results', data);
        this.showAllResultsBtn = true;
      });
  }
  // <------------------------------------------------- exportAll() function/End ---------------------------------------------->

  // <------------------------------------------------ summaryExport() function ----------------------------------------------->
  summaryExcelExport(data: any) {

    var filename = `CobraFilteredSummaryReport_${this.filtervalue}.xlsx`;
    if (this.selectedTribe != '') {
      filename = `CobraFilteredSummaryReport_${this.selectedTribe}.xlsx`;
    }
    saveAs(data, filename);
  }

  summaryExport() {
    this.showCurrentViewBtn = false;
    if (this.selectedTribe != '') {
      //filtering on tribe
      this.cobraDataService
        .exportSummary(this.selectedTribe, this.discountRate)
        .subscribe((data) => {
          this.summaryExcelExport(data);
          this.showCurrentViewBtn = true;
        });

    } else {
      //filtering on state or county
        this.cobraDataService
      .exportSummary(this.filtervalue, this.discountRate)
      .subscribe((data) => {
        this.summaryExcelExport(data);
        this.showCurrentViewBtn = true;
      });

    }
  
  }
  // <---------------------------------------------- SummaryExport() function/End --------------------------------------------->

  // Helper function: Format number to two significant figures
  formatToTwoSigFigs(num: number | string, isCurrency: boolean): string {
    num = parseFloat(`${num}`);
    if (parseInt(`${num}`) === 0) return '0';

    const magnitude = Math.floor(Math.log10(Math.abs(num)));
    const divisor = Math.pow(10, magnitude - 1);
    const rounded = Math.round(num / divisor);
    const final = rounded * divisor;

    return isCurrency
      ? new Intl.NumberFormat('en-US', {
          style: 'currency',
          currency: 'USD',
          maximumFractionDigits: 0,
          minimumFractionDigits: 0,
        }).format(final)
      : final.toLocaleString();
  }

  // Helper function: Generate tooltip content
  generateTooltipContent(
    feature: any,
    dataValue: string,
    mapTitle: any,
    stateAbbrev: any,
    numberFormatter: Intl.NumberFormat,
    isCensusTract: boolean
  ): string {
    const data = feature.properties.DATA;
    if (!data) return '';

    const state = stateAbbrev.find((x: any) => x.name === data.STATE);
    const tractName = isCensusTract
      ? `<br/>${data.COUNTY}, ${
          state && state.abbrev ? state.abbrev : data.STATE
        }:<br/>(${feature.properties.NAMELSAD})<br/>`
      : `<br/>${data.COUNTY}, ${
          state && state.abbrev ? state.abbrev : data.STATE
        }<br/>`;
    let caseNumber = data[dataValue];
    let rateChange = 'avoided';

    if (caseNumber < 0) {
      rateChange = 'increased';
      caseNumber *= -1;
    }

    caseNumber =
      caseNumber < 0.1 && caseNumber !== 0
        ? caseNumber.toExponential(2)
        : numberFormatter.format(caseNumber);

    const popupText = {
      1: `The ${mapTitle.popupTextName} in ${tractName} are ${
        mapTitle.units1
      }${this.formatToTwoSigFigs(data[dataValue], false)} ${mapTitle.units2}.`,
      2: `The ${mapTitle.popupTextName} in ${tractName} are ${
        mapTitle.units1
      }${this.formatToTwoSigFigs(data[dataValue], false)}.`,
      3: `${tractName} ${rateChange} ${caseNumber} ${mapTitle.units2} ${mapTitle.popupTextName}.`,
      4: `The monetary value of the change in ${
        mapTitle.popupTextName
      } in ${tractName} is ${this.formatToTwoSigFigs(data[dataValue], true)}.`,
    };

    return popupText[mapTitle.popupStyle];
  }

  precomputeChoroplethBreaks(fullData, isCensus = false) {
    if (isCensus) {
      /******add API data to full tract data for mapping*/
      fullData.features = fullData.features.map((feature) => {
        let newFeature = feature;
        if (this.items) {
          const tract = this.itemLookup[feature.properties.GEOID] || null;
          if (tract) {
            newFeature = {
              ...feature,
              properties: { ...feature.properties, DATA: tract },
            };
          }
        }

        return newFeature;
      });
    } else {
      /*console.log("full data after data add is:", fullData);
      //get new county data by summing accross census tracts
      // Excluded fields (not summed)
      const excludedFields = [
        'ID',
        'destindx',
        'tract_id',
        'FIPS',
        'STATE',
        'COUNTY',
        'IRA_fraction',
        'CJEST',
      ];

      fullData.features.forEach((feature) => {
        const fips = feature.properties.GEOID;
        if (this.groupedByFIPS[fips]) {
          const summedData = this.sumProperties(
            this.groupedByFIPS[fips],
            excludedFields
          );
          feature.properties.DATA = summedData;
        } else {
          feature.properties.DATA = {}; // No matching data found
        }
      });

      console.log("full data after data add is:", fullData);

      console.log('adding county, tracts and states layers');*/
    }
    const values = fullData.features
      .map((feature) =>
        feature.properties.DATA
          ? feature.properties.DATA[this.selectedMapLayer]
          : 0
      )
      .filter((value) => !isNaN(value)); // Filter valid numbers

    if (values.length > 0) {
      if (isCensus) {
        this.globalChoroplethTractLimits = chroma.limits(values, 'q', 5); // Quantile breaks
      } else {
        this.globalChoroplethLimits = chroma.limits(values, 'q', 5); // Quantile breaks
      }
    } else {
      console.warn('No valid values found for precomputing breaks.');
    }
  }

  // Helper function: Configure choropleth layer
  configureChoroplethLayer(
    data: any,
    dataValue: string,
    mapTitle: any,
    stateAbbrev: any,
    numberFormatter: Intl.NumberFormat,
    isCensusTract: boolean
  ) {
    const choroplethLayer = L.choropleth(data, {
      valueProperty: (feature: any) =>
        feature.properties.DATA && !isNaN(feature.properties.DATA[dataValue])
          ? feature.properties.DATA[dataValue]
          : 0,
      scale: this.globalChoroplethColors,
      steps: 5, // Ensure this matches the number of colors and legend limits
      mode: 'q', // Quantile mode
      limits: isCensusTract
        ? this.globalChoroplethTractLimits
        : this.globalChoroplethLimits,
      style: {
        color: '#808080', // Border color
        weight: 1, // Border thickness
        opacity: 1, // Border opacity
        fillOpacity: 0.6, // Fill opacity
      },
    }).bindTooltip(
      (layer: any) =>
        this.generateTooltipContent(
          layer.feature,
          dataValue,
          mapTitle,
          stateAbbrev,
          numberFormatter,
          isCensusTract
        ),
      {
        sticky: true,
        direction: 'auto',
        className: 'custom-tooltip',
      }
    );
    return choroplethLayer;
  }

  //change map styling on dropdown selection
  styleMap(layerValue: string, combinedFeatures: any = undefined) {
    const isCensusTract = !!combinedFeatures;
    const numberFormatter = new Intl.NumberFormat('en-US', {
      style: 'decimal',
      maximumFractionDigits: 2,
      minimumFractionDigits: 2,
    });

    const dataValue = layerValue || 'C__Total_Health_Benefits_Low_Value';
    this.selectedMapLayer = layerValue || this.selectedMapLayer;

    const mapTitle = this.mapLayerDisplayName.find(
      (x) => x.value === dataValue
    );
    const stateAbbrev = this.stateAbbrev;

    document.getElementById(
      'mapTitle'
    ).innerHTML = `Displaying: ${mapTitle.name}`;

    let newTractLayer = this.tractsLayer;
    if (isCensusTract) {
      newTractLayer = this.configureChoroplethLayer(
        combinedFeatures,
        dataValue,
        mapTitle,
        stateAbbrev,
        numberFormatter,
        isCensusTract
      );
      if (newTractLayer) {
        // Ensure the new layer uses a Canvas renderer
        newTractLayer.options.renderer = L.canvas();
        if (this.tractsLayer && this.map.hasLayer(this.tractsLayer)) {
          this.map.removeLayer(this.tractsLayer);
        }

        this.map.addLayer(newTractLayer);

        this.tractsLayer = newTractLayer;
        this.tractsLayer.bringToFront();
      }
    } else {
      if (this.countyLayer) {
        this.map.removeLayer(this.countyLayer);
      }
      this.countyLayer = this.configureChoroplethLayer(
        county_data,
        dataValue,
        mapTitle,
        stateAbbrev,
        numberFormatter,
        isCensusTract
      );
      this.map.addLayer(this.countyLayer);
    }

    if (this.items) {
      this.configureLegend(isCensusTract, dataValue);
    }

    this.statesLayer.bringToFront();
    this.IRAlayer.bringToFront();
    //this.CJESTlayer.bringToFront();
  }

  configureLegend(isCensusTract: boolean, dataValue: string) {
    const numberFormatter = new Intl.NumberFormat('en-US', {
      style: 'decimal',
      maximumFractionDigits: 2,
      minimumFractionDigits: 2,
    });

    const mapTitle = this.mapLayerDisplayName.find(
      (x) => x.value === dataValue
    );

    if (this.legend) {
      this.map.removeControl(this.legend);
    }
    this.legend = L.control({ position: 'bottomleft' });
    this.legend.onAdd = function (map) {
      const div = L.DomUtil.create('div', 'legend');

      // Preserve reference to class-level properties
      let limits = isCensusTract
        ? this.globalChoroplethTractLimits
        : this.globalChoroplethLimits;
      let colors = this.globalChoroplethColors;

      const labels = [];
      div.innerHTML =
        '<div class="labels"><p id="legendTitle">' +
        mapTitle.legendTitle +
        `${isCensusTract ? '<br/> per Census Tract' : '<br/> per County'}` +
        '</p>';

      if (limits) {
        limits.forEach((limit, index) => {
          labels.push(
            '<li style="background-color: ' + colors[index] + '"></li>'
          );
        });
      }

      const formatLegendValue = (value: number) => {
        if (value > -0.01 && value < 0) {
          return value.toExponential(2);
        } else if (value <= -1e9) {
          return numberFormatter.format(value / 1e9) + 'B';
        } else if (value <= -1e6) {
          return numberFormatter.format(value / 1e6) + 'M';
        } else if (value >= 1e9) {
          return numberFormatter.format(value / 1e9) + 'B';
        } else if (value >= 1e6) {
          return numberFormatter.format(value / 1e6) + 'M';
        } else {
          return numberFormatter.format(value);
        }
      };

      const legendUnits1 = formatLegendValue(limits[0]);
      const legendUnits2 = formatLegendValue(limits[limits.length - 1]);

      div.innerHTML +=
        '<ul>' +
        labels.join('') +
        '</ul><div class="min">' +
        mapTitle.units1 +
        legendUnits1 +
        '</div><div class="max">' +
        mapTitle.units1 +
        legendUnits2 +
        '</div></div>';

      return div;
    }.bind(this); // Bind to the class instance

    this.legend.addTo(this.map);
  }
}
