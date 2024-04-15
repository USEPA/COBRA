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
import 'leaflet-choropleth';
import { featureLayer } from 'esri-leaflet';

import { saveAs } from 'file-saver';

import { Token } from '../../Token';
import { CobraDataService } from '../../cobra-data-service.service';

import county_data from '../resultspanel/map_data/county_map.json';
import state_data from '../resultspanel/map_data/state_data.json';

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

  constructor(private cobraDataService: CobraDataService) {}

  /* variables used to show and hide different results screens */
  public showNoResultsScreen = true;
  public showPendingResultsScreen = false;
  // public showResultsScreen = true;
  public showHeartbeat = false;
  // public showResultsPanelContent = false;

  /* variable to show and hide build new scenario confirmation modal */
  public showBuildNewConfirmationModal: boolean = false;

  /* variables related to state and county dropdowns for filtering */
  public state_clr_structure: any[] = [];
  public counties_for_state: any[] = [];
  public countyFIPS: any;
  public countyName: any;
  public selectedTableState: string = 'all state';
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

    this.statesLayer = L.geoJSON(state_data, {
      style: {
        color: '#000',
        weight: 1,
        fill: false,
      },
    });

    var tribalLands = featureLayer({
      url: 'https://geopub.epa.gov/arcgis/rest/services/EMEF/tribal/MapServer/4',
      style: {
        color: '#404040',
        weight: 1,
        fill: false,
      },
    });

    var overlayLayers = {
      'Tribal Lands': tribalLands,
    };

    L.control.layers(null, overlayLayers).addTo(this.map);

    this.map.addLayer(OpenStreetMap_Mapnik);
    this.map.addLayer(this.statesLayer);
  }

  ngOnInit(): void {}

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
    this.styleMap(this.selectedMapLayer);
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
  filterDataForStateSelection() {
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

  // <--------------------------- Updates county dropdown once changing selection in state dropdown --------------------------->
  updateCountyDropDownAndFilterVal(index: any) {
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

    //getting 2 sig figs
    const magnitude = Math.floor(Math.log10(Math.abs(num)));
    let divisor = Math.pow(10, magnitude - 1);
    const adjustedNum = num + 5e-15 * num;
    let final = Math.round(adjustedNum / divisor) * divisor;

    if (Math.floor(Math.log10(Math.abs(final))) < magnitude) {
      divisor /= 10;
      final = Math.round(adjustedNum / divisor) * divisor;
    }

    // Handle javascript floating-point impresicion using string manipulation (without this it will return float numbers with more than 2 sig figs for floats under 0 in some cases)
    if (final < 1 && final > 0) {
      let strFinal = final.toString();

      // regex to capture any zeros between the dot and the first non-zero digit.
      const regexMatch = strFinal.match(/^0?\.0*([1-9]{1}[0-9]{1})/);

      if (regexMatch) {
        const leadingZeros = strFinal.slice(0, strFinal.indexOf(regexMatch[1]));
        strFinal = leadingZeros + regexMatch[1]; // Construct string keeping zeros and the two non-zero decimals
        final = parseFloat(strFinal);
      }
    }

    if (currency) {
      return new Intl.NumberFormat('en-US', {
        style: 'currency',
        currency: 'USD',
        maximumFractionDigits: 2,
        minimumFractionDigits: 0,
      }).format(final);
    } else {
      if (final < 1) {
        return final.toString();
      }
      return final.toLocaleString();
    }
  }

  filterDataForCountySelection() {
    this.items.map((item) => {
      if (item.FIPS == this.filtervalue) {
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
          for (let index = 0; index < county_data.features.length; index++) {
            let county = this.items.find(
              (x) => x.FIPS === county_data.features[index].properties.GEOID
            );
            if (county != undefined) {
              county_data.features[index].properties.DATA = county;
            }
          }
          //add geojson data layer to the map
          this.countyLayer = L.geoJSON(county_data);
          this.styleMap(this.selectedMapLayer);
          this.centerMap(this.map);
          if (!this.map.hasLayer(this.countyLayer)) {
            this.map.addLayer(this.countyLayer);
            this.map.addLayer(this.statesLayer);
          }
        },
        (err) => {
          console.error('An error occured retrieving results: ' + err);
          alert('An error occured retrieving results: ' + err);
        },
        () => {
          // heartbeat.setAttribute("hidden", "true");
          this.showHeartbeat = false;
          this.emitFromResultspanelToReviewpanelRetrievedResults();
          results_panel_content.removeAttribute('hidden');
          // makes sure the map is sized properly
          this.map.invalidateSize();
          // this.showResultsPanelContent = true;
        }
      );
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
    var filename = 'ExcelResultReport.xlsx';
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
    var filename = 'SummaryExcelReport.xlsx';
    saveAs(data, filename);
  }

  summaryExport() {
    this.showCurrentViewBtn = false;
    this.cobraDataService
      .exportSummary(this.filtervalue, this.discountRate)
      .subscribe((data) => {
        this.summaryExcelExport(data);
        this.showCurrentViewBtn = true;
      });
  }
  // <---------------------------------------------- SummaryExport() function/End --------------------------------------------->

  //change map styling on dropdown selection
  styleMap(layerValue) {
    const twoSigFigs = (num: number | string, currency: boolean) => {
      num = parseFloat(`${num}`);
      if (parseInt(`${num}`) === 0) return 0;

      const magnitude = Math.floor(Math.log10(Math.abs(num)));
      const divisor = Math.pow(10, magnitude - 1);
      const rounded = Math.round(num / divisor);

      const final = rounded * divisor;
      if (currency) {
        return new Intl.NumberFormat('en-US', {
          style: 'currency',
          currency: 'USD',
          maximumFractionDigits: 0,
          minimumFractionDigits: 0,
        }).format(final);
      } else {
        return final.toLocaleString();
      }
    };
    let number = new Intl.NumberFormat('en-US', {
      style: 'decimal',
      maximumFractionDigits: 2,
      minimumFractionDigits: 2,
    });

    let dataValue = 'C__Total_Health_Benefits_Low_Value';
    if (layerValue != undefined) {
      dataValue = layerValue;
      this.selectedMapLayer = layerValue;
    }

    let mapTitle = this.mapLayerDisplayName.find((x) => x.value == dataValue);
    let stateAbbrev = this.stateAbbrev;
    document.getElementById('mapTitle').innerHTML =
      'Displaying: ' + mapTitle.name;
    this.map.removeLayer(this.countyLayer);
    this.countyLayer = L.choropleth(county_data, {
      valueProperty: function (feature) {
        return feature.properties.DATA[dataValue];
      },
      scale: ['#f6eff7', '#bdc9e1', '#67a9cf', '#1c9099', '#016c59'],
      steps: 5,
      mode: 'q', //q for quantile, e for equidistant, k for k-means, l for logarithmic
      style: {
        color: '#808080',
        weight: 1,
        opacity: 1,
        fillOpacity: 0.7,
      },
    }).bindPopup(function (layer) {
      let state = stateAbbrev.find(
        (x) => x.name == layer.feature.properties.DATA.STATE
      );
      let rateChange = 'avoided';
      let caseNumber = layer.feature.properties.DATA[dataValue];
      if (caseNumber < 0) {
        rateChange = 'increased';
        caseNumber = caseNumber * -1;
      }
      if (caseNumber < 0.1 && caseNumber != 0) {
        caseNumber = caseNumber.toExponential(2);
      } else {
        caseNumber = number.format(caseNumber);
      }
      switch (mapTitle.popupStyle) {
        case 1:
          return (
            'The ' +
            mapTitle.popupTextName +
            ' in ' +
            layer.feature.properties.NAME +
            ', ' +
            state.abbrev +
            ' are ' +
            mapTitle.units1 +
            twoSigFigs(layer.feature.properties.DATA[dataValue], false) +
            ' ' +
            mapTitle.units2 +
            '.'
          );
        case 2:
          return (
            'The ' +
            mapTitle.popupTextName +
            ' in ' +
            layer.feature.properties.NAME +
            ', ' +
            state.abbrev +
            ' are ' +
            mapTitle.units1 +
            twoSigFigs(layer.feature.properties.DATA[dataValue], false) +
            '.'
          );
        case 3:
          return (
            layer.feature.properties.NAME +
            ', ' +
            state.abbrev +
            ' ' +
            rateChange +
            ' ' +
            caseNumber +
            ' ' +
            mapTitle.units2 +
            ' ' +
            mapTitle.popupTextName +
            '.'
          );
        case 4:
          return (
            'The monetary value of the change in ' +
            mapTitle.popupTextName +
            ' in ' +
            layer.feature.properties.NAME +
            ', ' +
            state.abbrev +
            ' is ' +
            twoSigFigs(layer.feature.properties.DATA[dataValue], true) +
            '.'
          );
      }
    });
    if (this.legend != undefined) {
      this.map.removeControl(this.legend);
    }
    this.legend = L.control({ position: 'bottomleft' });
    let countyLayer = this.countyLayer;
    this.legend.onAdd = function (map) {
      var div = L.DomUtil.create('div', 'legend');
      var limits = countyLayer.options.limits;
      var colors = countyLayer.options.colors;
      var labels = [];

      // Add min & max
      div.innerHTML =
        '<div class="labels"><p id="legendTitle">' +
        mapTitle.legendTitle +
        '</p>';

      limits.forEach(function (limit, index) {
        labels.push(
          '<li style="background-color: ' + colors[index] + '"></li>'
        );
      });

      let legendUnits1, legendUnits2;
      if (limits[0] > -0.01 && limits[0] < 0) {
        legendUnits1 = limits[0].toExponential(2);
      } else if (limits[0] <= -1000000000) {
        legendUnits1 = number.format(limits[0] / 1000000000) + 'B';
      } else if (limits[0] <= -1000000) {
        legendUnits1 = number.format(limits[0] / 1000000) + 'M';
      } else {
        legendUnits1 = number.format(limits[0]);
      }

      if (limits[limits.length - 1] < 0.01 && limits[limits.length - 1] > 0) {
        legendUnits2 = limits[limits.length - 1].toExponential(2);
      } else if (limits[limits.length - 1] >= 1000000000) {
        legendUnits2 =
          number.format(limits[limits.length - 1] / 1000000000) + 'B';
      } else if (limits[limits.length - 1] >= 1000000) {
        legendUnits2 = number.format(limits[limits.length - 1] / 1000000) + 'M';
      } else {
        legendUnits2 = number.format(limits[limits.length - 1]);
      }

      div.innerHTML +=
        '<ul>' +
        labels.join('') +
        '</ul><div class="min">' +
        mapTitle.units1 +
        legendUnits1 +
        '</div> \
        <div class="max">' +
        mapTitle.units1 +
        legendUnits2 +
        '</div></div>';
      return div;
    };
    this.legend.addTo(this.map);
    this.map.addLayer(this.countyLayer);
    this.statesLayer.bringToFront();
  }
}
