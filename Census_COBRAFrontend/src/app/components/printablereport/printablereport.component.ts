import { Component, OnInit, Input, ChangeDetectorRef } from '@angular/core';
import { CobraDataService } from '../../cobra-data-service.service';
import html2canvas from 'html2canvas';
import { trigger, style, transition, animate } from '@angular/animations';
import JSZip, { filter } from 'jszip';
import { saveAs } from 'file-saver';

interface AdvancedFilter {
  category: string;
  operator: string;
  value: number;
  threshold: string; //corresponds to the column name ENERGYBURDEN_6pct vs ENERGYBURDEN_10pct
  mode: string; //percentile or threshold
}
interface PopulationInfo {
  [dest_tract: string]: {
    population: number;
    FIPS: string;
    CJEST: number;
    IRA_fraction: number;
    LIFEEXPPCT: number;
    LOWINCOME: number;
    P_OZONE: number;
    P_PM25: number;
    "ENERGYBURDEN<6PCT": number;
    "ENERGYBURDEN>=6PCT": number;
    "ENERGYBURDEN>=10PCT": number;

  };
}

@Component({
  selector: 'app-printablereport',
  templateUrl: './printablereport.component.html',
  styleUrls: ['./printablereport.component.scss'],
  animations: [
    trigger('slideInOut', [
      // When element is added (enter)
      transition(':enter', [
        style({ height: 0, opacity: 0 }),
        animate('300ms ease-out', style({ height: '*', opacity: 1 })),
      ]),
      // When element is removed (leave)
      transition(':leave', [
        animate('300ms ease-in', style({ height: 0, opacity: 0 })),
      ]),
    ]),
  ],
})
export class PrintablereportComponent implements OnInit {
  @Input() selectedFilter: string;
  @Input() inputComponents: any[];
  @Input() state_clr_structure: any[];
  @Input() discountRate: number;
  @Input() currentResults: any;
  @Input() populationInfo: PopulationInfo = {};
  @Input() filterState: any;
  @Input() mode: string;
  @Input() avertData: { avertInputs: any; avertRegions: any };
  windowOrigin = window.location.origin;

  /*
   filterState = {
      tableStates: this.resultspanelComponent?.tableStates || {
        'all state': 'All Contiguous U.S. States',
        'selected state': '',
        'selected county': '',
      },
      countyName: this.resultspanelComponent?.countyName,
      countyFIPS: this.resultspanelComponent?.countyFIPS,
      selectedTableState: this.resultspanelComponent?.selectedTableState || 'all state',
      filtervalue: this.resultspanelComponent.filtervalue || '00',
    }*/

  public selectedStateIndex: any = '';
  public selectedCountyIndex: any = '';
  public title = 'Nationwide';

  public showResetReport = false;
  public toggleSections = {
    summary: true,
    cobraInputs: true,
    betaNote: true,
    avertInputs: false,
    benefitsTable: true,
    impactsTable: true,
    dacChart: true,
  };

  public impactData = {
    dacLowImpacts: 0,
    dacHighImpacts: 0,
    nonDacLowImpacts: 0,
    nonDacHighImpacts: 0,
    totalPopLowImpacts: 0,
    totalPopHighImpacts: 0,
    //dac vs nondac benefits and disbenefits
    dacLowBenefits: 0,
    nonDacLowBenefits: 0,
    dacLowDisbenefits: 0,
    nonDacLowDisbenefits: 0,
  };

  public nationalImpactData = {
    dacLowImpacts: 0,
    dacHighImpacts: 0,
    nonDacLowImpacts: 0,
    nonDacHighImpacts: 0,
    totalPopLowImpacts: 0,
    totalPopHighImpacts: 0,
    //dac vs nondac benefits and disbenefits
    dacLowBenefits: 0,
    nonDacLowBenefits: 0,
    dacLowDisbenefits: 0,
    nonDacLowDisbenefits: 0,
    totalBenefitsLowValue: 0,
    totalDisbenefitsLowValue: 0,
  };

  public populationData = {
    //percent of total pop receiving benefits
    totalPopulationBenefits: 0,

    //percent of Dac pop receiving benefits
    percentDACBenefits: 0,

    //percent of non-dac pop receiving benefits
    percentNONDACBenefits: 0,
    // precent of population receiving disbenfits
    totalPopulationHurt: 0,

    // percent of disadvantaged communities receiving disbenifits
    percentDACHurt: 0,
    // percent of nondisadvantaged communities receiving disbenifits
    percentNONDACHurt: 0,
    // percent of population that is disadvantaged vs disadvantaged
    dacPercentPop: 0,
    nonDacPercentPop: 0,

    benefitsGoingtoDAC: 0,
    benefitsGoingtoNonDAC: 0,
    disbenefitsGoingtoDAC: 0,
    disbenefitsGoingtoNonDAC: 0,
    disbenefits: false,
    benefits: false,

    //percent of dac vs non dac pop that are unimpacted
    percentDACUnimpacted: 0,
    percentNonDACUnimpacted: 0,

    //determimed to receive no benefits or disbenefits based on threshold
    unimpactedPop: 0,
    unimpactedTotalPercent: 0,
    unimpactedDACPercent: 0,
    unimpactedNonDacPercent: 0,
    totalPopulation: 0,

    //dollar value of total benefits and disbenefits in the filtered scenario
    totalBenefitsValue: 0,
    totalDisbenefitsValue: 0,
  };

  public thresholdInputs = {
    percent: 0,
    dollars: 0,
    percentBenefitsWithThreshold: 0,
    percentDisbenefitsWithThreshold: 0,
  };

  //public lidcDefinition: string = 'IRA_fraction';

  public filterItems: any[] = [];
  public filterSummary: any = {};

  public nationalItems: any[] = [];
  public nationalSummary: any = {};

  /* filter vars */
  public counties_for_state: any[] = [];
  public groupedComponents: any[] = [];
  /*public countyFIPS: any;
  public countyName: any;
  public selectedTableState: string = 'all state';
  public tableStates: any = {
    'all state': 'All Contiguous U.S. States',
    'selected state': '',
    'selected county': '',
  };
  public filtervalue = '00';*/
  /************************************* */

  public selectedSectors = [];

  public avertRows: { projectType: string; changes: string[] }[] = [];

  public avertDisplay = {
    annualGwhReduction: {
      displayName: 'Annual GWh Reduction',
      projectType: 'Energy Efficiency',
    },
    hourlyMwReduction: {
      displayName: 'Hourly MW Reduction',
      projectType: 'Energy Efficiency',
    },
    /* unsure of these */
    broadProgramReduction: {
      displayName: 'Broad Program Reduction',
      projectType: 'Energy Efficiency',
      units: '%',
    },
    targetedProgramReduction: {
      displayName: 'Targeted Program Reduction',
      projectType: 'Energy Efficiency',
      units: '%',
    },
    topHours: {
      displayName: 'Top Hours',
      projectType: 'Energy Efficiency',
      units: '%',
    },
    maxAnnualDischargeCycles: {
      displayName: 'Max Annual Discharge Cycles',
      projectType: 'Energy Storage',
    },
    /****/
    onshoreWind: {
      displayName: 'Onshore wind total capacity (MW)',
      projectType: 'Renewable Energy',
    },
    offshoreWind: {
      displayName: 'Offshore wind total capacity (MW)',
      projectType: 'Renewable Energy',
    },
    utilitySolar: {
      displayName: 'Utility solar total capacity (MW)',
      projectType: 'Renewable Energy',
    },
    rooftopSolar: {
      displayName: 'Rooftop solar total capacity (MW)',
      projectType: 'Renewable Energy',
    },
    utilityStorage: {
      displayName: 'Utility storage total capacity (MW)',
      projectType: 'Energy Storage',
    },
    rooftopStorage: {
      displayName: 'Rooftop storage total capacity (MW)',
      projectType: 'Energy Storage',
    },
    batteryEVs: {
      displayName: 'Light-duty battery EVs',
      projectType: 'Electric Vehicles',
    },
    hybridEVs: {
      displayName: 'Plug-in hybrid EVs',
      projectType: 'Electric Vehicles',
    },
    transitBuses: {
      displayName: 'Electric transit buses',
      projectType: 'Electric Vehicles',
    },
    schoolBuses: {
      displayName: 'Electric school buses',
      projectType: 'Electric Vehicles',
    },
    evDeploymentLocation: {
      displayName: 'EV deployment location',
      projectType: 'Electric Vehicles',
    },
    evModelYear: {
      displayName: 'EV model year',
      projectType: 'Electric Vehicles',
    },
    iceReplacementVehicle: {
      displayName: 'ICE vehicles being replaced',
      projectType: 'Electric Vehicles',
    },
  };

  public regionsMap = {
    AL: 'Alabama',
    AK: 'Alaska',
    AZ: 'Arizona',
    AR: 'Arkansas',
    CA: 'California',
    CO: 'Colorado',
    CT: 'Connecticut',
    DE: 'Delaware',
    FL: 'Florida',
    GA: 'Georgia',
    HI: 'Hawaii',
    ID: 'Idaho',
    IL: 'Illinois',
    IN: 'Indiana',
    IA: 'Iowa',
    KS: 'Kansas',
    KY: 'Kentucky',
    LA: 'Louisiana',
    ME: 'Maine',
    MD: 'Maryland',
    MA: 'Massachusetts',
    MI: 'Michigan',
    MN: 'Minnesota',
    MS: 'Mississippi',
    MO: 'Missouri',
    MT: 'Montana',
    NE: 'New England',
    NV: 'Nevada',
    NH: 'New Hampshire',
    NJ: 'New Jersey',
    NM: 'New Mexico',
    NY: 'New York',
    NC: 'North Carolina',
    ND: 'North Dakota',
    OH: 'Ohio',
    OK: 'Oklahoma',
    OR: 'Oregon',
    PA: 'Pennsylvania',
    RI: 'Rhode Island',
    SC: 'South Carolina',
    SD: 'South Dakota',
    TN: 'Tennessee',
    TX: 'Texas',
    UT: 'Utah',
    VT: 'Vermont',
    VA: 'Virginia',
    WA: 'Washington',
    WV: 'West Virginia',
    WI: 'Wisconsin',
    WY: 'Wyoming',
    SW: 'Southwest',
    NW: 'Northwest',
    SE: 'Southeast',
    MIDW: 'Midwest',
    MIDA: 'Mid-Atlantic',
    DC: 'District of Columbia',
    CENT: 'Central',
    RM: 'Rocky Mountains',
    NCSC: 'Carolinas',
  };

  /****** COMMUNITIES OF INTEREST FILTERS  */
  // Controls whether the advanced filters panel is visible
  showAdvancedFilters = true;

  // Stores the list of advanced filters
  advancedFilters: AdvancedFilter[] = [];

  // Determines how filters are combined ("AND" or "OR")
  filterLogic = 'AND';

  constructor(
    private cobraDataService: CobraDataService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.groupedComponents = this.groupComponentsBySector(this.inputComponents);

    console.log('AVERT DATA IS:', this.avertData);
    if (this.mode === 'AVERT') {
      //make sure avert is actually sending the right data before creating an avert section in the report
      if (this.avertData && this.avertData.avertInputs) {
        this.toggleSections.avertInputs = true;
        console.log('populating avert rows');
        this.avertRows = this.populateAvertRows(
          this.avertData.avertInputs,
          this.avertDisplay
        );
      } else {
        console.error('Avert is not sending over avert inputs');
      }
    } else {
      console.log('no avert data to display');
    }
    if (this.getTitle() === 'Nationwide') {
      this.selectedStateIndex = '';
      this.selectedCountyIndex = '';
    }

    //currentresults will always be nationwide items
    if (this.currentResults && this.currentResults.Items) {
      console.log('CURRENT RESULTS ARE:', this.currentResults);
      this.filterItems = this.currentResults.Items;
      this.filterSummary = this.currentResults.Summary;
      this.nationalItems = this.filterItems;
      this.nationalSummary = this.filterSummary;
      this.getNationWideImpacts(this.nationalItems);
    } else {
      this.filterItems = [];
      this.filterSummary = {};
    }

    if (this.filterState.selectedTableState === 'all state') {
      this.selectedStateIndex = '';
      this.selectedCountyIndex = '';
      //perform filtering and select appropriate dropdown items if state or county is selected
      this.getPopulationBenefitsAndImpacts(this.filterItems);
    } else {
      //get first two chards of filtervalue to get state fips
      this.selectedStateIndex = this.state_clr_structure.findIndex(
        (item) => item.STFIPS === this.filterState.filterValue.substr(0, 2)
      );
      this.counties_for_state =
        this.state_clr_structure[this.selectedStateIndex].counties;
      var county_dropdown = document.getElementById('county_dd_report');
      county_dropdown?.removeAttribute('disabled');
      if (this.filterState.selectedTableState === 'selected state') {
        this.filterItems = this.filterItems.filter(
          (item, i) => item.FIPS.substr(0, 2) == this.filterState.filterValue
        );
        //recalculate
        this.getPopulationBenefitsAndImpacts(this.filterItems);
      } else if (this.filterState.selectedTableState === 'selected county') {
        var selectedCountyIndex = this.counties_for_state.findIndex(
          (county) => county.FIPS === this.filterState.countyFIPS
        );
        console.log('selected county index is: ', selectedCountyIndex);
        this.selectedCountyIndex =
          selectedCountyIndex > -1
            ? `${this.counties_for_state[selectedCountyIndex]?.county}${this.counties_for_state[selectedCountyIndex].FIPS}`
            : '';
        console.log('SET SELECTEDCOUNTYINDEX TO:', this.selectedCountyIndex);
        this.filterState.tableStates['selected county'] =
          this.filterState.countyName +
          ', ' +
          this.filterState.tableStates['selected state'];
        this.filterState.filterValue = this.filterState.countyFIPS;
        this.filterItems = this.filterItems.filter(
          (item, i) => item.FIPS == this.filterState.filterValue
        );
        //recalculate
        this.getPopulationBenefitsAndImpacts(this.filterItems);
      }
    }
    //add a single default filter
    this.addAdvancedFilter();
  }

  /*------- COMMUNITIES OF INTEREST filter logic ------------------------------ */
  // Toggles the advanced filters panel
  toggleAdvancedFilters(): void {
    this.showAdvancedFilters = !this.showAdvancedFilters;
    if (this.advancedFilters.length === 0) {
      this.addAdvancedFilter(); // Add a default filter if none exist
    }
  }

  // Adds a new filter with default values (note: category is empty for placeholder)
  addAdvancedFilter(): void {
    this.advancedFilters.push({
      category: '',
      operator: '>',
      value: 75,
      threshold: 'ENERGYBURDEN<6PCT',
      mode: 'percentile', // default mode is percentile
    });
    //since they haven't selected a category yet, we don't have to call onFilterChange();
  }

  // Removes a filter at the specified index
  removeAdvancedFilter(index: number): void {
    this.advancedFilters.splice(index, 1);
    this.onFilterChange();
  }

  getFilterDescription(filter: any): string {
    const direction = filter.operator === '>' ? 'higher' : 'lower';
  
    switch (filter.category) {
      case 'LIFEEXPPCT':
        return `Have ${direction} life expectancies than ${filter.value}% of communities.`;
      case 'LOWINCOME':
        return `Have ${direction === 'higher' ? 'more' : 'fewer'} low-income households than ${filter.value}% of communities.`;
      case 'ENERGYBURDEN':
          const pct = filter.threshold === 'ENERGYBURDEN<6PCT' ? '< 6%' : filter.threshold === 'ENERGYBURDEN>=6PCT' ? '≥  6%' : ' ≥ 10%';
          return `Pay ${pct} of their household income to energy costs.`;
      case 'P_PM25':
        return `Experience ${direction} PM2.5 concentrations than ${filter.value}% of communities.`;
      case 'P_OZONE':
        return `Experience ${direction} ozone concentrations than ${filter.value}% of communities.`;
      default:
        return '';
    }
  }

  toggleOperator(filter: AdvancedFilter): void {
    filter.operator = filter.operator === '>' ? '<' : '>';
    //recompute impacts
    this.onFilterChange(filter);
  }

  onFilterChange(filter: AdvancedFilter | undefined = undefined): void {
    console.log('detected filter change'); 
    if (filter && filter.category === "ENERGYBURDEN") filter.mode = 'threshold';
    else if (filter) filter.mode = 'percentile';
    //recompute impacts
    this.getNationWideImpacts(this.nationalItems);
    this.getPopulationBenefitsAndImpacts(this.filterItems);
  }

  onModeToggle(filter: any, isChecked: boolean): void {
    filter.mode = isChecked ? 'percentile' :  'threshold';
    this.onFilterChange(filter);
  }

  getSliderBackground(value: number): string {
    // Choose your colors: filled portion (e.g., blue) and remaining (e.g., light gray)
    const fillColor = '#007bff'; // Change to desired color
    const trackColor = '#ccc'; // Change to desired color
    // value is already a percentage from 0 to 100
    return `linear-gradient(to right, ${fillColor} 0%, ${fillColor} ${value}%, ${trackColor} ${value}%, ${trackColor} 100%)`;
  }

  updateThresholdInputs(key: string, event: any) {
    // Disallow negative values
    let value = parseFloat(event.target.value);
    if (value < 0) {
      this.thresholdInputs[key] = 0; // Reset to 0 if negative
    } else {
      this.thresholdInputs[key] = value;
    }

    // Update percent and dollars based on user input
    if (key === 'percent') {
      this.thresholdInputs.dollars = Math.abs(
        this.nationalImpactData.totalPopLowImpacts *
          (this.thresholdInputs.percent / 100)
      );
      this.getPercentBenefitsEcompassedWithThreshold();
    } else if (key === 'dollars') {
      this.thresholdInputs.percent = Math.abs(
        (this.thresholdInputs.dollars /
          this.nationalImpactData.totalPopLowImpacts) *
          100
      );
      this.getPercentBenefitsEcompassedWithThreshold();
    } else if (key === 'percentBenefitsWithThreshold') {
      if (this.nationalImpactData.totalBenefitsLowValue !== 0) {
        let targetSum =
          (this.thresholdInputs.percentBenefitsWithThreshold / 100) *
          this.nationalImpactData.totalBenefitsLowValue;

        // Sort health benefit values in descending order
        let sortedValues = this.nationalItems
          .map((item) => item.C__Total_Health_Benefits_Low_Value)
          .filter((value) => value > 0) // Only consider positive benefit values
          .sort((a, b) => b - a); // Sort in descending order

        let cumulativeSum = 0;
        let threshold = 0;

        for (let value of sortedValues) {
          cumulativeSum += value;
          if (cumulativeSum >= targetSum) {
            threshold = value; // This is the dollar threshold we are looking for
            break;
          }
        }

        this.thresholdInputs.dollars = threshold;

        this.thresholdInputs.percent = Math.abs(
          (this.thresholdInputs.dollars /
            this.nationalImpactData.totalPopLowImpacts) *
            100
        );
      } else {
        this.thresholdInputs.dollars = 0;
        this.thresholdInputs.percent = 0;
      }
      //recalculate disbenefits encompassed by threshold
      this.getPercentBenefitsEcompassedWithThreshold('disbenefits');
    } else if (key === 'percentDisbenefitsWithThreshold') {
      if (this.nationalImpactData.totalDisbenefitsLowValue !== 0) {
        let targetSum =
          (this.thresholdInputs.percentDisbenefitsWithThreshold / 100) *
          this.nationalImpactData.totalDisbenefitsLowValue;

        // Sort disbenefit values in **ascending** order (smallest to largest negative values)
        let sortedValues = this.nationalItems
          .map((item) => item.C__Total_Health_Benefits_Low_Value)
          .filter((value) => value < 0) // Only consider negative disbenefit values
          .sort((a, b) => a - b); // Sort in ascending order (smallest/more negative first)

        let cumulativeSum = 0;
        let threshold = 0;

        for (let value of sortedValues) {
          cumulativeSum += value;
          if (Math.abs(cumulativeSum) >= Math.abs(targetSum)) {
            threshold = value; // This is the dollar threshold we are looking for
            break;
          }
        }

        this.thresholdInputs.dollars = threshold;
        this.thresholdInputs.percent = Math.abs(
          (this.thresholdInputs.dollars /
            this.nationalImpactData.totalPopLowImpacts) *
            100
        );
      } else {
        this.thresholdInputs.dollars = 0;
        this.thresholdInputs.percent = 0;
      }
      //recalculate benefits encompassed by threshold
      //this.getPercentBenefitsEcompassedWithThreshold('benefits');
    }

    //round all keys in thresholdInputs to 2 decimal places
    for (let key in this.thresholdInputs) {
      this.thresholdInputs[key] = this.roundWithDecimals(
        this.thresholdInputs[key]
      );
    }

    // Update population percentages based off updated threshold
    this.getPopulationBenefitsAndImpacts(this.filterItems);
  }

  getPercentBenefitsEcompassedWithThreshold(benefitsOrDisbenefits?: string) {
    /*let benefitsValueMeetingThreshold = 0;
    let disbenefitsValueMeetingThreshold = 0;

    const nationalItemsMeetingThreshold = this.nationalItems.filter(
      (item) =>
        Math.abs(item.C__Total_Health_Benefits_Low_Value) >=
        this.thresholdInputs.dollars
    );

    for (let item of nationalItemsMeetingThreshold) {
      if (item.C__Total_Health_Benefits_Low_Value >= 0) {
        benefitsValueMeetingThreshold +=
          item.C__Total_Health_Benefits_Low_Value;
      } else {
        disbenefitsValueMeetingThreshold +=
          item.C__Total_Health_Benefits_Low_Value;
      }
    }


    if (benefitsOrDisbenefits === 'benefits' || !benefitsOrDisbenefits) {
      
      // Update benefit and disbenefit percentages
      if (this.nationalImpactData.totalBenefitsLowValue !== 0) {
        this.thresholdInputs.percentBenefitsWithThreshold = Math.abs(
          (benefitsValueMeetingThreshold /
            this.nationalImpactData.totalBenefitsLowValue) *
            100
        );
      } else {
        this.thresholdInputs.percentBenefitsWithThreshold = 0;
      }
    } if (
      benefitsOrDisbenefits === 'disbenefits' ||
      !benefitsOrDisbenefits
    ) {
      if (this.nationalImpactData.totalDisbenefitsLowValue !== 0) {

        this.thresholdInputs.percentDisbenefitsWithThreshold = Math.abs(
          disbenefitsValueMeetingThreshold) /
            Math.abs(this.nationalImpactData.totalDisbenefitsLowValue) *
            100;
      } else {
        this.thresholdInputs.percentDisbenefitsWithThreshold = 0;
      }
    }*/
  }

  hasVisibleChanges(component: any): boolean {
    return ['PM2.5', 'SO2', 'NOx', 'VOC'].some(
      (pollutant) =>
        component.changes[pollutant] && component.changes[pollutant] !== 0
    );
  }

  calculateRowspan(component: any): number {
    return (
      Object.keys(component.changes).filter(
        (pollutant) =>
          ['PM2.5', 'SO2', 'NOx', 'VOC'].includes(pollutant) &&
          component.changes[pollutant] &&
          component.changes[pollutant] !== 0
      ).length + 1
    );
  }

  downloadToPng(id: string) {
    const element = document.getElementById(id);

    if (element) {
      // Add 'screenshot-mode' class to hide icons not wanted in screenshot
      element.classList.add('screenshot-mode');

      html2canvas(element)
        .then((canvas) => {
          // Remove 'screenshot-mode' class after screenshot
          element.classList.remove('screenshot-mode');

          const link = document.createElement('a');
          link.download = `${id}.png`;
          link.href = canvas.toDataURL();
          link.click();
        })
        .catch((error) => {
          element.classList.remove('screenshot-mode');
          console.error('Error capturing screenshot:', error);
        });
    }
  }

  getAvertRegions(): string {
    if (this.avertData.avertRegions) {
      const avertRegions = this.avertData.avertRegions;
      if (avertRegions.length === 0) return '';
      if (avertRegions.length === 1) return `${avertRegions[0]} Region`;
      if (avertRegions.length === 2)
        return `${avertRegions.join(' and ')} Regions`;

      // For more than 2 elements
      const allButLastTwo = avertRegions.slice(0, -2).join(', ');
      const lastTwo = avertRegions.slice(-2).join(' and ');

      const combined = allButLastTwo ? `${allButLastTwo}, ${lastTwo}` : lastTwo;

      return `${combined} Regions`;
    }
    return '';
  }

  populateAvertRows(
    inputData: Record<string, string>,
    dictionary: any
  ): { projectType: string; changes: string[] }[] {
    // Step 1: Filter out keys with empty values
    const filteredData = Object.entries(inputData).filter(
      ([key, value]) => value !== ''
    );

    // Step 2: Group changes by projectType
    const projectTypeMap: Record<string, string[]> = {};

    filteredData.forEach(([key, value]) => {
      if (dictionary[key]) {
        //only add certain records conditionally

        //ev deployment location should only show if evs are modeled:
        const EVkeys = [
          'batteryEVs',
          'hybridEVs',
          'transitBuses',
          'schoolBuses',
        ];
        //check if evs were modeled and skip adding these fields if not
        if (
          [
            'evDeploymentLocation',
            'evModelYear',
            'iceReplacementVehicle',
          ].includes(key) &&
          !EVkeys.some((evKey) => inputData[evKey] && inputData[evKey] !== '')
        ) {
          return; // Skip if no EVs are modeled
        }

        //get correct region displayname
        if (key === 'evDeploymentLocation' && value) {
          const split = value.split('-');
          if (split.length < 2 || !this.regionsMap[split[1]]) {
            return value;
          } else {
            value = `${this.regionsMap[split[1]]}`;
          }
        }

        //max annual discharge cycles should only show if storage is modeled
        if (
          key === 'maxAnnualDischargeCycles' &&
          (!inputData['utilityStorage'] ||
            inputData['utilityStorage'] === '') &&
          (!inputData['rooftopStorage'] || inputData['rooftopStorage'] === '')
        ) {
          return;
        }

        const { displayName, projectType } = dictionary[key];
        if (!projectTypeMap[projectType]) {
          projectTypeMap[projectType] = [];
        }
        //add units to specific fields:
        if (dictionary[key].units) {
          const units = dictionary[key].units;
          projectTypeMap[projectType].push(`${displayName}: ${value}${units}`);
        } else {
          projectTypeMap[projectType].push(`${displayName}: ${value}`);
        }
      }
    });

    // Step 3: Transform grouped data into the required structure
    const avertRows = Object.entries(projectTypeMap).map(
      ([projectType, changes]) => ({
        projectType,
        changes,
      })
    );
    console.log('returning avertRows:', avertRows);

    return avertRows;
  }

  resetReport() {
    this.showResetReport = false;
    //restore / show all sections
    this.toggleSections = {
      summary: true,
      cobraInputs: true,
      betaNote: true,
      avertInputs: this.mode === 'AVERT' ? true : false, // Only show AVERT inputs if in AVERT mode
      benefitsTable: true,
      impactsTable: true,
      dacChart: true,
    };
  }

  round(value: number): number {
    return Math.round(value);
  }

  roundWithDecimals(value: number): number {
    if (Number.isInteger(value)) {
      return value; // Preserve whole numbers
    }

    // If value is >= 0.01, round to at most 2 decimal places
    if (Math.abs(value) >= 0.01) {
      return parseFloat(value.toFixed(2));
    }

    // If value is very small (e.g., 0.0000023), keep significant digits
    return parseFloat(value.toPrecision(2));
  }

  groupComponentsBySector(inputComponents: any[]): any[] {
    // Create a map to group components by their tierSelections
    const groupedComponents = new Map<string, any>();

    inputComponents.forEach((component) => {
      const tierKey = JSON.stringify(component.tierSelections);

      if (!groupedComponents.has(tierKey)) {
        // Initialize a new group if it doesn't exist
        groupedComponents.set(tierKey, {
          tierSelections: component.tierSelections,
          changes: { ...component.changes }, // Deep copy the changes object
        });
      } else {
        // Merge changes into the existing group
        const existingGroup = groupedComponents.get(tierKey);
        for (const key in component.changes) {
          if (component.changes.hasOwnProperty(key)) {
            existingGroup.changes[key] += component.changes[key];
          }
        }
      }
    });

    // Convert the grouped components back into an array
    return Array.from(groupedComponents.values());
  }

  getSector(component: any): string {
    //filter out any null tiers and join
    return component.tierSelections
      .filter((tier) => tier !== null && tier !== '' && tier !== undefined)
      .join(', ');
  }

  getChanges(pollutant: string, component: any): string {
    const value = component.changes[pollutant];

    // Format the number with two decimal places and commas
    return `${value > 0 ? '+' : ''}${new Intl.NumberFormat('en-US', {
      minimumFractionDigits: 2,
      maximumFractionDigits: 2,
    }).format(value)}`;
  }

  format(value: number): string {
    // Format the number with two decimal places and commas
    return new Intl.NumberFormat('en-US', {
      minimumFractionDigits: 2,
      maximumFractionDigits: 2,
    }).format(value);
  }

  print(divID: string) {
    window.print();
  }

  exportReportData() {
    console.log('Exporting data...');

    const zip = new JSZip(); // Create a new ZIP archive

    // 1. Create Filtering Details CSV
    let filterDetailsText = `
COBRA Report Details:
---------------------
The following report was generated on: ${new Date().toLocaleString()}`;
    if (this.title === 'Nationwide') {
      filterDetailsText += ` and includes all contiguous U.S. states.\n\n`;
    } else {
      filterDetailsText += ` and the results have been filtered to include census tracts from: ${this.title}.\n\n`;
    }

    filterDetailsText += `COBRA Scenario Inputs
---------------------\n`;

    this.groupedComponents.forEach((component) => {
      if (this.hasVisibleChanges(component)) {
        // Add sector name
        filterDetailsText += `Changes to emissions in sector: ${this.getSector(
          component
        )}\n`;

        // Add pollutant rows under the sector
        if (component.changes['PM2.5']) {
          filterDetailsText += `        - PM2.5: ${this.getChanges(
            'PM2.5',
            component
          )} short tons\n`;
        }
        if (component.changes['SO2']) {
          filterDetailsText += `        - SO2: ${this.getChanges(
            'SO2',
            component
          )} short tons\n`;
        }
        if (component.changes['NOx']) {
          filterDetailsText += `        - NOx: ${this.getChanges(
            'NOx',
            component
          )} short tons\n`;
        }
        if (component.changes['VOC']) {
          filterDetailsText += `        - VOC: ${this.getChanges(
            'VOC',
            component
          )} short tons\n`;
        }

        filterDetailsText += '\n'; // Add space between sectors
      }
    });

    if (this.getValidSliderFilters().length > 0) {
      filterDetailsText += `Communities of Interest (COI) Filters
-------------------------------------
When compared to other census tracts nationwide, you have defined "Communities of Interest" as census tracts that meet ${
        this.filterLogic === 'AND' ? 'all' : 'any'
      } of the following:\n`;
      this.getValidSliderFilters().forEach((component) => {
        filterDetailsText += `
                    - ${this.getFilterDescription(component)}
        \n`;
      });
      filterDetailsText += `\nCensus tracts meeting these criteria are denoted by the "Within COI" column in the accompanying data.\n`;
    } else {
      filterDetailsText += `Communities of Interest (COI) Filters
-------------------------------------
No communities of interest were defined as a part of this report.\n`;
    }

    // 2. Create Filtered Data CSV
    const csvHeaders = [
      'Census Tract ID',
      'State',
      'County',
      'Population',
      '$ Total Health Benefits (Low)',
      '$ Total Health Benefits (High)',
      'Within COI',
      'Income %ile',
      'Life Expectancy %ile',
      'Ambient PM2.5 %ile',
      'Ambient Ozone %ile',
      'Energy Burden < 6% of Income',
      'Energy Burden >= 6% of Income',
      'Energy Burden >= 10% of Income'
    ];

    let csvData: string[] = [csvHeaders.join(',')]; // Start with headers

    for (let item of this.filterItems.sort((a,b) => a.tract_id - b.tract_id)) {
      const popInfo = this.populationInfo[item.tract_id];
      const row = [item.tract_id, item.STATE, item.COUNTY];

      row.push(popInfo.population);

      row.push(item.C__Total_Health_Benefits_Low_Value);
      row.push(item.C__Total_Health_Benefits_High_Value);

      row.push(this.isInFilter(popInfo) ? 'Yes' : 'No');
      row.push(popInfo.LOWINCOME);
      row.push(popInfo.LIFEEXPPCT);

      row.push(popInfo.P_PM25);
      row.push(popInfo.P_OZONE);
      row.push(popInfo["ENERGYBURDEN<6PCT"]);
      row.push(popInfo["ENERGYBURDEN>=6PCT"]);
      row.push(popInfo["ENERGYBURDEN>=10PCT"]);

      csvData.push(row.join(','));
    }

    const filteredDataCSV = csvData.join('\n');

    // 3. Add files to the ZIP archive
    zip.file('COBRA_Report_Details.txt', filterDetailsText.trim()); // Save as .txt
    zip.file('COBRA_Report_Data.csv', filteredDataCSV);

    // 4. Generate the ZIP file and trigger download
    zip.generateAsync({ type: 'blob' }).then((zipBlob) => {
      const date = new Date().toISOString().split('T')[0]; // Format: YYYY-MM-DD
      saveAs(zipBlob, `COBRAReport_${date}.zip`);
    });
  }

  getTitle(): string {
    if (this.filterState.selectedTableState === 'all state') {
      this.title = 'Nationwide';
      return 'Nationwide';
    } else if (this.filterState.selectedTableState === 'selected county') {
      this.title = this.filterState.tableStates['selected county'];
      return this.filterState.tableStates['selected county'];
    } else if (this.filterState.selectedTableState === 'selected state') {
      this.title = this.filterState.tableStates['selected state'];
      return this.filterState.tableStates['selected state'];
    }
    return this.filterState.tableStates['selected state'];
  }

  getFilterResults(): any {
    this.cobraDataService
      .getResults(this.filterState.filterValue, this.discountRate)
      .subscribe((data) => {
        console.log('API RESULTS ARE:', data);
        this.filterItems = data['Impacts'];
        this.filterSummary = data['Summary'];
      });
  }

  getValidSliderFilters(): any[] {
    //only return filters that have categories set / valid values
    return this.advancedFilters.filter(
      (f) =>
        f.category && f.operator && f.value !== null && f.value !== undefined
    );
  }

  getFilterForCategory(filter: AdvancedFilter, item: any): boolean {
    let category = filter.category
    if (filter.category === "ENERGYBURDEN") {
      if (filter.mode === 'percentile') category = "ENERGYBURDENPCT";
      else category = filter.threshold;
    }
    //now that category is set properly continue with filtering logic
    if (filter.mode === 'threshold') {
      return item[category] === 1;
    }
    if (item[category] === -1) {
      return false; // If the category value is -1, it means no data is available for that category.
    }
    return filter.operator === '>'
          ? item[category] > filter.value
          : item[category] < filter.value;
  }

  isInFilter(item: any | null): boolean {
    if (!item) return false;
    const filters = this.getValidSliderFilters();
    if (filters.length === 0) return false;

    if (this.filterLogic === 'AND') {
      // All conditions must be true.
      return filters.every((filter) => {
        return this.getFilterForCategory(filter, item);
      });
    } else {
      // At least one condition must be true.
      return filters.some((filter) => {
        return this.getFilterForCategory(filter, item);
      });
    }
  }

  getNationWideImpacts(items: any[]) {
    this.nationalImpactData.dacLowImpacts = 0;
    this.nationalImpactData.dacHighImpacts = 0;
    this.nationalImpactData.nonDacLowImpacts = 0;
    this.nationalImpactData.nonDacHighImpacts = 0;
    this.nationalImpactData.totalPopLowImpacts = 0;
    this.nationalImpactData.totalPopHighImpacts = 0;

    this.nationalImpactData.dacLowBenefits = 0;
    this.nationalImpactData.nonDacLowBenefits = 0;
    this.nationalImpactData.dacLowDisbenefits = 0;
    this.nationalImpactData.nonDacLowDisbenefits = 0;

    (this.nationalImpactData.totalBenefitsLowValue = 0),
      (this.nationalImpactData.totalDisbenefitsLowValue = 0);

    // Loop through items and use populationInfo dictionary for efficient lookup
    for (let item of items) {
      const destTract = item.tract_id;
      const populationInfo = this.populationInfo[destTract] || null; // Look up directly in the dictionary

      if (!populationInfo) {
        console.warn(`No population info found for dest_tract: ${destTract}`);
        continue;
      }

      if (populationInfo) {
        // Impact calculations
        this.nationalImpactData.totalPopLowImpacts +=
          item.C__Total_Health_Benefits_Low_Value;
        this.nationalImpactData.totalPopHighImpacts +=
          item.C__Total_Health_Benefits_High_Value;

        //COI
        if (this.isInFilter(populationInfo)) {
          this.nationalImpactData.dacHighImpacts +=
            item.C__Total_Health_Benefits_High_Value;
          this.nationalImpactData.dacLowImpacts +=
            item.C__Total_Health_Benefits_Low_Value;
        }
        // non COI
        else {
          this.nationalImpactData.nonDacLowImpacts +=
            item.C__Total_Health_Benefits_Low_Value;
          this.nationalImpactData.nonDacHighImpacts +=
            item.C__Total_Health_Benefits_High_Value;
        }

        //get benefits
        if (item.C__Total_Health_Benefits_Low_Value >= 0) {
          //total
          this.nationalImpactData.totalBenefitsLowValue +=
            item.C__Total_Health_Benefits_Low_Value;

          //filtered pop vs non filtered pop
          if (this.isInFilter(populationInfo)) {
            this.nationalImpactData.dacLowBenefits +=
              item.C__Total_Health_Benefits_Low_Value;
          } else {
            this.nationalImpactData.nonDacLowBenefits +=
              item.C__Total_Health_Benefits_Low_Value;
          }
        } else {
          //disbenefits
          //total
          this.nationalImpactData.totalDisbenefitsLowValue +=
            item.C__Total_Health_Benefits_Low_Value;

          //filtered pop vs non filtered pop
          if (this.isInFilter(populationInfo)) {
            this.nationalImpactData.dacLowDisbenefits +=
              item.C__Total_Health_Benefits_Low_Value;
          } else {
            this.nationalImpactData.nonDacLowDisbenefits +=
              item.C__Total_Health_Benefits_Low_Value;
          }
        }
      }
    } //done items loop
    //now that we have total national impacts (low) we can calculate the threshold dollar amount
    //thresholdInputs.percent is already set by default to 0.001%
   /* this.thresholdInputs.dollars = Math.abs(
      this.nationalImpactData.totalPopLowImpacts *
        (this.thresholdInputs.percent / 100)
    );

    //this.getPercentBenefitsEcompassedWithThreshold();
    //round all keys in thresholdInputs to 2 decimal places
    for (let key in this.thresholdInputs) {
      this.thresholdInputs[key] = this.roundWithDecimals(
        this.thresholdInputs[key]
      );
    }*/
  }

  getPopulationBenefitsAndImpacts(items: any[]) {
    // Reset population and impact data
    this.populationData.percentDACBenefits = 0;
    this.populationData.percentNONDACBenefits = 0;
    let DACPop = 0;
    let nonDACPop = 0;
    //DAC population recieving benefits
    let DACBenefitsPop = 0;

    let DACUnimpactedPop = 0;
    let nonDACUnimpactedPop = 0;

    //actual of value of benefits going to DAC
    let DACBenefitsValue = 0;
    let nonDACBenefitsValue = 0;
    let totalBenefitsValue = 0;

    let DACHurt = 0;
    let nonDACHurt = 0;

    let unimpactedDACPop = 0;
    let unimpactedNonDACPop = 0;

    this.populationData.totalPopulationBenefits = 0;
    this.populationData.totalPopulationHurt = 0;
    let nonDACBenefits = 0;

    //overallimapcts
    this.impactData.dacLowImpacts = 0;
    this.impactData.dacHighImpacts = 0;
    this.impactData.nonDacLowImpacts = 0;
    this.impactData.nonDacHighImpacts = 0;
    this.impactData.totalPopLowImpacts = 0;
    this.impactData.totalPopHighImpacts = 0;
    //dac vs nondac benefits and disbenefits
    this.impactData.dacLowBenefits = 0;
    this.impactData.nonDacLowBenefits = 0;
    this.impactData.dacLowDisbenefits = 0;
    this.impactData.nonDacLowDisbenefits = 0;

    let totalPopulation = 0;
    let totalBenefits = 0;
    let totalDisbenefits = 0;
    this.populationData.disbenefits = false;
    this.populationData.benefits = false;

    let totalBenefitsWithoutThreshold = 0;
    let totalDisBenefitsWithoutThreshold = 0;

    //determimed to receive no benefits or disbenefits based on threshold
    this.populationData.unimpactedPop = 0;
    this.populationData.unimpactedTotalPercent = 0;
    this.populationData.unimpactedDACPercent = 0;
    this.populationData.unimpactedNonDacPercent = 0;

    // Loop through items and use populationInfo dictionary for efficient lookup
    for (let item of items) {
      const destTract = item.tract_id;
      const populationInfo = this.populationInfo[destTract] || null; // Look up directly in the dictionary

      if (!populationInfo) {
        console.warn(`No population info found for dest_tract: ${destTract}`);
        continue;
      }

      if (populationInfo) {
        const population = populationInfo.population;

        // DAC population calculations with threshold
        if (this.isInFilter(populationInfo)) {
          DACPop += population;
          if (
            item.C__Total_Health_Benefits_Low_Value >=
            this.thresholdInputs.dollars
          ) {
            DACBenefitsPop += population;
            DACBenefitsValue += item.C__Total_Health_Benefits_Low_Value;
          } else if (
            item.C__Total_Health_Benefits_Low_Value <=
            this.thresholdInputs.dollars * -1
          ) {
            DACHurt += population;
          } else {
            //unimpacted population
            DACUnimpactedPop += population;
            unimpactedDACPop += population;
          }
        } else {
          //get the total nonDAC population
          nonDACPop += population;
          //non COI population
          //benefits condition
          if (
            item.C__Total_Health_Benefits_Low_Value >
            this.thresholdInputs.dollars
          ) {
            //get nonDAC population recieving benefits
            nonDACBenefits += population;
          }

          //getnonDAC pop receiving disbenefits
          else if (
            item.C__Total_Health_Benefits_Low_Value <=
            this.thresholdInputs.dollars * -1
          ) {
            //get nonDAC population receiving disbenefits
            nonDACHurt += population;
          }

          //unimpacted condition
          if (
            item.C__Total_Health_Benefits_Low_Value <=
              this.thresholdInputs.dollars &&
            item.C__Total_Health_Benefits_Low_Value >=
              this.thresholdInputs.dollars * -1
          ) {
            nonDACUnimpactedPop += population;
            unimpactedNonDACPop += population;
          }
        }

        // benefits and disbenefits without caring about threshold
        if (item.C__Total_Health_Benefits_Low_Value >= 0) {
          //$ value of benefits going to DAC
          if (this.isInFilter(populationInfo)) {
            this.impactData.dacLowBenefits +=
              item.C__Total_Health_Benefits_Low_Value;
          } else {
            //$ value of benefits going to nonDAC
            this.impactData.nonDacLowBenefits +=
              item.C__Total_Health_Benefits_Low_Value;
          }

          //total benefits
          totalBenefitsWithoutThreshold +=
            item.C__Total_Health_Benefits_Low_Value;
        } else if (item.C__Total_Health_Benefits_Low_Value < 0) {
          //get the value of dac disbenefits going to dac without caring for threshold
          if (this.isInFilter(populationInfo)) {
            this.impactData.dacLowDisbenefits +=
              item.C__Total_Health_Benefits_Low_Value;
          } else {
            //value of disbenefits going to nonDAC
            this.impactData.nonDacLowDisbenefits +=
              item.C__Total_Health_Benefits_Low_Value;
          }

          totalDisBenefitsWithoutThreshold +=
            item.C__Total_Health_Benefits_Low_Value;
        }

        // Total pop benefits and disbenefits with threshold
        if (
          item.C__Total_Health_Benefits_Low_Value >= this.thresholdInputs.dollars
        ) {
          // recieving (positive) benefits
          //console.log("FOUND ITEM RECIEVING BENEFITS:", item);

          //total benefits value
          totalBenefitsValue += item.C__Total_Health_Benefits_Low_Value;
          //total population recieving benefits
          this.populationData.totalPopulationBenefits += population;

          // add total benefits value
          totalBenefits += item.C__Total_Health_Benefits_Low_Value;
          this.populationData.benefits = true;
        } else if (
          item.C__Total_Health_Benefits_Low_Value <=
          this.thresholdInputs.dollars * -1
        ) {
          // GET DISBENEFITS
          this.populationData.disbenefits = true;

          //get total population receiving disbenifts
          this.populationData.totalPopulationHurt += population;
          // get the total value of benefits
          totalDisbenefits += item.C__Total_Health_Benefits_Low_Value;
        } else {
          //unimpacted population
          this.populationData.unimpactedPop += population;
        }

        //impact calculations for COI vs non COI
        if (this.isInFilter(populationInfo)) {
          this.impactData.dacLowImpacts +=
            item.C__Total_Health_Benefits_Low_Value;
          this.impactData.dacHighImpacts +=
            item.C__Total_Health_Benefits_High_Value;
        } else {
          //non COI impacts
          this.impactData.nonDacLowImpacts +=
            item.C__Total_Health_Benefits_Low_Value;
          this.impactData.nonDacHighImpacts +=
            item.C__Total_Health_Benefits_High_Value;
        }

        // Impact calculations for impact table
        this.impactData.totalPopLowImpacts +=
          item.C__Total_Health_Benefits_Low_Value;
        this.impactData.totalPopHighImpacts +=
          item.C__Total_Health_Benefits_High_Value;

        totalPopulation += population;
      }
    } //end of items/census tracts loop

    //set total population of filtere dpop
    this.populationData.totalPopulation = totalPopulation;

    // Calculate percentages for table
    if (totalPopulation !== 0) {
      this.populationData.totalPopulationBenefits =
        (this.populationData.totalPopulationBenefits / totalPopulation) * 100;
      this.populationData.totalPopulationHurt =
        (this.populationData.totalPopulationHurt / totalPopulation) * 100;

      this.populationData.dacPercentPop = (DACPop / totalPopulation) * 100;
      this.populationData.nonDacPercentPop =
        100 - this.populationData.dacPercentPop;

      //get unimpacted population
      this.populationData.unimpactedTotalPercent =
        (this.populationData.unimpactedPop / totalPopulation) * 100;
      if (this.populationData.unimpactedPop > 0) {
        this.populationData.unimpactedDACPercent =
          (unimpactedDACPop / this.populationData.unimpactedPop) * 100;
        this.populationData.unimpactedNonDacPercent =
          (unimpactedNonDACPop / this.populationData.unimpactedPop) * 100;
      } else {
        this.populationData.unimpactedDACPercent = 0;
        this.populationData.unimpactedNonDacPercent = 0;
      }
    } else {
      this.populationData.totalPopulationBenefits = 0;
      this.populationData.totalPopulationHurt = 0;
      this.populationData.dacPercentPop = 0;
      this.populationData.nonDacPercentPop = 0;
    }
    // Population of DAC vs non-DAC receiving benefits
    if (DACPop !== 0) {
      this.populationData.percentDACBenefits = (DACBenefitsPop / DACPop) * 100;
      this.populationData.percentDACHurt = (DACHurt / DACPop) * 100;
      this.populationData.percentDACUnimpacted =
        (DACUnimpactedPop / DACPop) * 100;
    } else {
      this.populationData.percentDACBenefits = 0;
      this.populationData.percentDACHurt = 0;
      this.populationData.percentDACUnimpacted = 0;
    }

    if (nonDACPop !== 0) {
      this.populationData.percentNONDACBenefits =
        (nonDACBenefits / nonDACPop) * 100;
      this.populationData.percentNONDACHurt = (nonDACHurt / nonDACPop) * 100;
      this.populationData.percentNonDACUnimpacted =
        (nonDACUnimpactedPop / nonDACPop) * 100;
    } else {
      this.populationData.percentNONDACBenefits = 0;
      this.populationData.percentNONDACHurt = 0;
      this.populationData.percentNonDACUnimpacted = 0;
    }

    /***** begun  */
    if (totalBenefitsWithoutThreshold !== 0) {
      this.populationData.benefits = true;

      //dac and non dac benefits
      this.populationData.benefitsGoingtoDAC =
        (this.impactData.dacLowBenefits / totalBenefitsWithoutThreshold) * 100;
      this.populationData.benefitsGoingtoNonDAC =
        (this.impactData.nonDacLowBenefits / totalBenefitsWithoutThreshold) *
        100;
    } else {
      this.populationData.benefits = false;
      this.populationData.benefitsGoingtoDAC = 0;
      this.populationData.benefitsGoingtoNonDAC = 0;
    }

    //calculate percentages for bar graph
    if (totalDisBenefitsWithoutThreshold !== 0) {
      this.populationData.disbenefits = true;
      //dac and non dac disbenefits
      this.populationData.disbenefitsGoingtoDAC =
        (this.impactData.dacLowDisbenefits / totalDisBenefitsWithoutThreshold) *
        100;
      this.populationData.disbenefitsGoingtoNonDAC =
        (this.impactData.nonDacLowDisbenefits /
          totalDisBenefitsWithoutThreshold) *
        100;
    } else {
      this.populationData.disbenefits = false;
      this.populationData.disbenefitsGoingtoDAC = 0;
      this.populationData.disbenefitsGoingtoNonDAC = 0;
    }

    //set total benefits and disbenefits for filter to be used in bar graph tooltips
    this.populationData.totalBenefitsValue = totalBenefitsWithoutThreshold;
    this.populationData.totalDisbenefitsValue =
      totalDisBenefitsWithoutThreshold;

    //ensure angular detects changes
    this.populationData = { ...this.populationData };
    this.impactData = { ...this.impactData };

    this.cdr.detectChanges();
  }

  /* filtering logic ---------------------------------------------------- */
  showHideStateCountyNameAndUpdateFilterVal(index: any, countyValue: any) {
    console.log('IN FILTER BY COUNTY FUNC WITH COUNTYVALUE:', countyValue);
    if (index !== '' && countyValue === '') {
      this.filterState.selectedTableState = 'selected state';
      this.filterState.filterValue = this.state_clr_structure[index].STFIPS;
      this.filterDataForStateSelection();
      this.selectedCountyIndex = '';
    } else if (index === '') {
      this.filterItems = this.nationalItems;
      this.filterSummary = this.nationalSummary;
      this.selectedStateIndex = '';
      this.filterState.selectedTableState = 'all state';
      this.selectedCountyIndex = '';
    }
    if (countyValue !== '') {
      this.filterState.countyFIPS = countyValue.substr(countyValue.length - 5);
      this.filterState.countyName = countyValue.substr(
        0,
        countyValue.length - 5
      );
      this.filterState.tableStates['selected county'] =
        this.filterState.countyName +
        ', ' +
        this.filterState.tableStates['selected state'];
      this.filterState.selectedTableState = 'selected county';
      this.filterState.filterValue = this.filterState.countyFIPS;
      this.filterDataForCountySelection();
    }
    //update report title
    this.getTitle();
  }

  filterDataForCountySelection() {
    this.filterItems = this.nationalItems;
    this.filterItems = this.filterItems.filter(
      (item, i) => item.FIPS == this.filterState.filterValue
    );
    //recalculate
    this.getPopulationBenefitsAndImpacts(this.filterItems);
  }

  filterDataForStateSelection() {
    console.log(
      'FILTER VALUE STATE SELECTION IS:',
      this.filterState.filterValue
    );
    this.filterItems = this.nationalItems;
    this.filterItems = this.filterItems.filter(
      (item, i) => item.FIPS.substr(0, 2) == this.filterState.filterValue
    );
    //recalculate
    console.log(
      'FILTERING ON STATE: GETTING BENEFITS AND IMPACTS WITH ITEMS:',
      this.filterItems
    );
    this.getPopulationBenefitsAndImpacts(this.filterItems);
  }

  updateCountyDropDownAndFilterVal(index: any) {
    this.selectedStateIndex = index;

    var county_dropdown = document.getElementById('county_dd_report');
    if (index === '') {
      this.counties_for_state = [];
      county_dropdown?.setAttribute('disabled', '');
      this.filterState.filterValue = '00';
      this.filterState.selectedTableState = 'all state';
      this.selectedCountyIndex = '';
      this.filterItems = this.nationalItems;
      this.filterSummary = this.nationalSummary;
      this.getPopulationBenefitsAndImpacts(this.filterItems);
      //this.showTableDataForAllStates();
    } else {
      this.counties_for_state = this.state_clr_structure[index].counties;
      county_dropdown?.removeAttribute('disabled');
      this.filterState.filterValue = this.state_clr_structure[index].STFIPS;
      this.filterState.tableStates['selected state'] =
        this.state_clr_structure[index].STNAME;
      this.filterState.selectedTableState = 'selected state';
      this.filterDataForStateSelection();
      this.getTitle();
    }
    var selectedCountyIndex = this.counties_for_state.findIndex(
      (county) => county.FIPS === this.filterState.countyFIPS
    );
    console.log('selected county index is: ', selectedCountyIndex);
    this.selectedCountyIndex =
      selectedCountyIndex > -1
        ? `${this.counties_for_state[selectedCountyIndex]?.county}${this.counties_for_state[selectedCountyIndex].FIPS}`
        : '';
    this.showHideStateCountyNameAndUpdateFilterVal(
      index,
      this.selectedCountyIndex
    );
    this.cdr.detectChanges(); // Force change detection
  }

  /*onDACToggle(value: string) {
    this.lidcDefinition = value;
    console.log('LIDC Definition changed to:', this.lidcDefinition);
    // Additional logic based on the selected value
    this.getPopulationBenefitsAndImpacts(this.filterItems);
  }*/

  public toggleSectionVisibility(key: string): void {
    this.toggleSections[key] = false;

    if (Object.values(this.toggleSections).some((val) => val === false)) {
      this.showResetReport = true;
    }
  }

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
      return final.toLocaleString();
    }
  }
}
