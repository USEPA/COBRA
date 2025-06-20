import {
  Component,
  ViewEncapsulation,
  OnInit,
  Input,
  Output,
  EventEmitter,
} from '@angular/core';
import { NgForm } from '@angular/forms';
import { ClrSelectedState } from '@clr/angular';

import { Token } from '../../Token';
import { CobraDataService } from '../../cobra-data-service.service';
import { GlobalsService } from 'src/app/globals.service';

@Component({
  selector: 'app-emissionspanel',
  templateUrl: './emissionspanel.component.html',
  styleUrls: ['./emissionspanel.component.scss'],
  encapsulation: ViewEncapsulation.None,
})
export class EmissionspanelComponent implements OnInit {
  @Input() token: Token;
  @Output() emissionspanelToReviewpanelEmitter = new EventEmitter<any>();
  @Output() emissionspanelToReviewpanelDataSourceEmitter =
    new EventEmitter<any>();
  @Output() emissionspanelToResultspanelEmitter = new EventEmitter<any>();

  constructor(
    private cobraDataService: CobraDataService,
    private global: GlobalsService
  ) {}

  /* Error messages */
  public error_notnumber: string = 'Enter a positive numeric value.';
  public error_outofrange: string = 'Enter a value between 0 and 100%.';
  public error_largerthanbaseline: string =
    'Reductions entered exceed baseline emissions.';
  public error_cantreducebaseline: string =
    'Baseline is 0 tons and cannot be reduced.';

  /* Location variables */
  private statetree_items: any[] = null;
  public statetree_treeview: any[];
  public state_clr_structure: any[] = []; /* */
  public statetree_items_selected: any = null; /* */

  /* Tier variables */
  public tiertree_items: any[] = null;
  public tiertree_treeview: any[];
  public tiertree_items_selected: any[] = null; /* */
  public index_tier1 = '';
  public index_tier2 = '';
  public tier2_items: any[] = []; /* */
  public tier3_items: any[] = []; /* */

  /* Emissions variables */
  public last_returned_emissions: any = { baseline: [], control: [] }; /* */
  public pm25_baseline: number; /* */
  public so2_baseline: number; /* */
  public nox_baseline: number; /* */
  public nh3_baseline: number; /* */
  public voc_baseline: number; /* */
  public pm25_control: number = null; /* */
  public so2_control: number = null; /* */
  public nox_control: number = null; /* */
  public nh3_control: number = null; /* */
  public voc_control: number = null; /* */
  public pollutants = [
    {
      index: 0,
      name: 'PM25',
      name_sub: 'PM<sub>2.5</sub>',
      baseline_rounded_up: null,
      ri_switch_name: 'PM25ri',
      ri_switch_model: 'reduce',
      ri_switch_reduce_id: 'PM25r',
      ri_switch_increase_id: 'PM25i',
      input_name: 'changePM25',
      input_model: null,
      pt_switch_name: 'PM25pt',
      pt_switch_model: 'tons',
      pt_switch_tons_id: 'PM25t',
      pt_switch_percent_id: 'PM25p',
    },
    {
      index: 1,
      name: 'SO2',
      name_sub: 'SO<sub>2</sub>',
      baseline_rounded_up: null,
      ri_switch_name: 'SO2ri',
      ri_switch_model: 'reduce',
      ri_switch_reduce_id: 'SO2r',
      ri_switch_increase_id: 'SO2i',
      input_name: 'changeSO2',
      input_model: null,
      pt_switch_name: 'SO2pt',
      pt_switch_model: 'tons',
      pt_switch_tons_id: 'SO2t',
      pt_switch_percent_id: 'SO2p',
    },
    {
      index: 2,
      name: 'NOx',
      name_sub: 'NO<sub>x</sub>',
      baseline_rounded_up: null,
      ri_switch_name: 'NOXri',
      ri_switch_model: 'reduce',
      ri_switch_reduce_id: 'NOxr',
      ri_switch_increase_id: 'NOxi',
      input_name: 'changeNOx',
      input_model: null,
      pt_switch_name: 'NOXpt',
      pt_switch_model: 'tons',
      pt_switch_tons_id: 'NOxt',
      pt_switch_percent_id: 'NOxp',
    },
    {
      index: 3,
      name: 'VOC',
      name_sub: 'VOC',
      baseline_rounded_up: null,
      ri_switch_name: 'VOCri',
      ri_switch_model: 'reduce',
      ri_switch_reduce_id: 'VOCr',
      ri_switch_increase_id: 'VOCi',
      input_name: 'changeVOC',
      input_model: null,
      pt_switch_name: 'VOCpt',
      pt_switch_model: 'tons',
      pt_switch_tons_id: 'VOCt',
      pt_switch_percent_id: 'VOCp',
    },
    {
      index: 4,
      name: 'NH3',
      name_sub: 'NH<sub>3</sub>',
      baseline_rounded_up: null,
      ri_switch_name: 'NH3ri',
      ri_switch_model: 'reduce',
      ri_switch_reduce_id: 'NH3r',
      ri_switch_increase_id: 'NH3i',
      input_name: 'changeNH3',
      input_model: null,
      pt_switch_name: 'NH3pt',
      pt_switch_model: 'tons',
      pt_switch_tons_id: 'NH3t',
      pt_switch_percent_id: 'NH3p',
    },
  ];

  /* variables to show and hide baseline values */
  public locationPresent: boolean = false; /* */
  public tierPresent: boolean = false; /* */
  public showBaselines: boolean = false;

  /* variables to show and hide error messages */
  public showErrorNotNumber: any = [false, false, false, false, false]; /* */
  // the input should be in the range of 0<=x<=100
  public showErrorOutOfRange: any = [false, false, false, false, false]; /* */
  public showErrorLargerThanBaseline: any = [
    false,
    false,
    false,
    false,
    false,
  ]; /* */
  public showErrorCantReduceBaseline: any = [
    false,
    false,
    false,
    false,
    false,
  ]; /* */

  /* variable to show and hide conflict modal */
  public showConflictModal: boolean = false;

  /* Other variables */
  public endApiCall: boolean;
  public reviewPanelComponents: any[] = [];
  public foundConflict: boolean = false;
  public mode: string = 'COBRA';

  /* variables to pass to review panel */
  public stateCountyBadgesList = [];
  public tier1_selection_text = null;
  public tier2_selection_text = null;
  public tier3_selection_text = null;
  public component_data_for_reviewpanel = null;

  // <----------------------------------------- Requests state and tier data from API ----------------------------------------->
  ngOnInit() {
    this.cobraDataService.getDataDictionary_Tiers().subscribe(
      (data) => {
        this.tiertree_items = data;
        this.createStateAndTierTrees('tiers');
      },
      (err) => console.error('An error occured retrieving tier items: ' + err),
      () => {}
    );
    this.cobraDataService.getDataDictionary_State().subscribe(
      (data) => {
        this.statetree_items = data;
        this.createStateAndTierTrees('states');
      },
      (err) => console.error('An error occured retrieving state items: ' + err),
      () => {}
    );
  }
  // <--------------------------------------- Requests state and tier data from API/End --------------------------------------->

  // <---------------------------------------- Calls emissionspanelToReviewpanelDataSourceEmitter --------------------------------------->
  emitFromEmissionspanelToReviewpanelDataSource(dataIsAddedToForm: boolean) {
    this.emissionspanelToReviewpanelDataSourceEmitter.emit(dataIsAddedToForm);
  }
  // <-------------------------------------- Calls emissionspanelToReviewpanelDataSourceEmitter/End ------------------------------------->

  // <---------------------------------------- Calls emissionspanelToReviewpanelEmitter --------------------------------------->
  emitFromEmissionspanelToReviewpanel(data: any) {
    this.emissionspanelToReviewpanelEmitter.emit(data);
  }
  // <-------------------------------------- Calls emissionspanelToReviewpanelEmitter/End ------------------------------------->

  // <--------------------------------- Adds the new component based on emissions panel inputs -------------------------------->
  addComponentToScenario(
    modifyemissionsform: NgForm,
    queueData: any = null,
    dataIsAddedToForm: boolean = true
  ): void {
    this.mode = this.global.getMode();
    this.emitFromEmissionspanelToReviewpanelDataSource(dataIsAddedToForm);
    if (dataIsAddedToForm == true) {
      var chem_list = ['PM25', 'SO2', 'NOx' /*'NH3'*/, 'VOC'];
      var value = 0;
      var conversion = '';
      var adjustment = '';
      for (var i = 0; i < chem_list.length; i++) {
        value = parseFloat(modifyemissionsform.value['change' + chem_list[i]]);
        if (value === undefined || value == null || isNaN(value)) value = 0;
        conversion = modifyemissionsform.value[chem_list[i].toUpperCase() + 'pt'];
        adjustment = modifyemissionsform.value[chem_list[i].toUpperCase() + 'ri'];
        if (conversion == 'percent') {
          if (chem_list[i] == 'PM25')
            value = (this.pm25_baseline * value) / 100;
          if (chem_list[i] == 'SO2') value = (this.so2_baseline * value) / 100;
          if (chem_list[i].toUpperCase() === 'NOX') value = (this.nox_baseline * value) / 100;
          if (chem_list[i] == 'NH3') value = (this.nh3_baseline * value) / 100;
          if (chem_list[i] == 'VOC') value = (this.voc_baseline * value) / 100;
        }
        if (adjustment == 'reduce') {
          if (chem_list[i] == 'PM25')
            this.pm25_control = this.pm25_baseline - value;
          if (chem_list[i] == 'SO2')
            this.so2_control = this.so2_baseline - value;
          if (chem_list[i].toUpperCase() === 'NOX')
            this.nox_control = this.nox_baseline - value;
          if (chem_list[i] == 'NH3')
            this.nh3_control = this.nh3_baseline - value;
          if (chem_list[i] == 'VOC')
            this.voc_control = this.voc_baseline - value;
        } else if (adjustment == 'increase') {
          if (chem_list[i] == 'PM25')
            this.pm25_control = this.pm25_baseline + value;
          if (chem_list[i] == 'SO2')
            this.so2_control = this.so2_baseline + value;
          if (chem_list[i].toUpperCase() === 'NOX')
            this.nox_control = this.nox_baseline + value;
          if (chem_list[i] == 'NH3')
            this.nh3_control = this.nh3_baseline + value;
          if (chem_list[i] == 'VOC')
            this.voc_control = this.voc_baseline + value;
        }
      }

      /* This code snippet updates last_returned_emissions */
      let updatePacket = {};
      updatePacket['PM25'] = this.pm25_control;
      updatePacket['SO2'] = this.so2_control;
      updatePacket['NOx'] = this.nox_control;
      updatePacket['NH3'] = this.nh3_control;
      updatePacket['VOC'] = this.voc_control;
      updatePacket['fipscodes'] = this.statetree_items_selected;
      updatePacket['tierselection'] = this.tiertree_items_selected;

      // set control data to new inputs
      this.last_returned_emissions['control'][0] = updatePacket;

      /* This code snippet updates data that is going to be passed to reviewpanel */
      this.component_data_for_reviewpanel = {};
      this.component_data_for_reviewpanel['stateCountyBadgesList'] =
        this.stateCountyBadgesList;
      this.component_data_for_reviewpanel['tier1Text'] =
        this.tier1_selection_text;
      this.component_data_for_reviewpanel['tier2Text'] =
        this.tier2_selection_text;
      this.component_data_for_reviewpanel['tier3Text'] =
        this.tier3_selection_text;
      this.component_data_for_reviewpanel['PM25ri'] =
        this.pollutants[0].ri_switch_model;
      this.component_data_for_reviewpanel['SO2ri'] =
        this.pollutants[1].ri_switch_model;
      this.component_data_for_reviewpanel['NOXri'] =
        this.pollutants[2].ri_switch_model;
      this.component_data_for_reviewpanel['NH3ri'] =
        this.pollutants[4].ri_switch_model;
      this.component_data_for_reviewpanel['VOCri'] =
        this.pollutants[3].ri_switch_model;
      this.component_data_for_reviewpanel['cPM25'] =
        this.pollutants[0].input_model;
      this.component_data_for_reviewpanel['cSO2'] =
        this.pollutants[1].input_model;
      this.component_data_for_reviewpanel['cNOX'] =
        this.pollutants[2].input_model;
      this.component_data_for_reviewpanel['cNH3'] =
        this.pollutants[4].input_model;
      this.component_data_for_reviewpanel['cVOC'] =
        this.pollutants[3].input_model;
      this.component_data_for_reviewpanel['PM25pt'] =
        this.pollutants[0].pt_switch_model;
      this.component_data_for_reviewpanel['SO2pt'] =
        this.pollutants[1].pt_switch_model;
      this.component_data_for_reviewpanel['NOXpt'] =
        this.pollutants[2].pt_switch_model;
      this.component_data_for_reviewpanel['NH3pt'] =
        this.pollutants[4].pt_switch_model;
      this.component_data_for_reviewpanel['VOCpt'] =
        this.pollutants[3].pt_switch_model;
      // values used for the backend computations
      this.component_data_for_reviewpanel['statetree_items_selected'] =
        this.statetree_items_selected;
      this.component_data_for_reviewpanel['tiertree_items_selected'] =
        this.tiertree_items_selected;
      this.component_data_for_reviewpanel['updatePacket'] = updatePacket;

      this.checkForPossibleConflicts(
        this.component_data_for_reviewpanel,
        dataIsAddedToForm,
        queueData
      );

      if (!this.foundConflict) {
        /* All added components are saved in this array(backend computations are called later once clicking on Run Scenario button) */
        this.reviewPanelComponents.push(this.component_data_for_reviewpanel);
        /* After adding each component to scenario, all selections and inputs in the first panel are cleared to allow for creating a new component */
        this.clearStateSelections();
        this.clearTierSelections();
        this.clearEmissionChanges();
        this.showBaselines = false;
        document.getElementById('step2').style.visibility = 'visible';
        this.emitFromEmissionspanelToReviewpanel(
          this.component_data_for_reviewpanel
        );
      }
      // this.global.setMode("COBRA");
    }

    if (dataIsAddedToForm == false) {
      document
        .getElementById('statetree_spinner')
        .setAttribute('hidden', 'true');
      document.getElementById('statestree_and_btns').removeAttribute('hidden');
      queueData.queueElements.forEach((element) => {
        this.component_data_for_reviewpanel = element;
        this.checkForPossibleConflicts(
          this.component_data_for_reviewpanel,
          dataIsAddedToForm,
          element
        );

        if (!this.foundConflict) {
          /* All added components are saved in this array(backend computations are called later once clicking on Run Scenario button) */
          this.reviewPanelComponents.push(this.component_data_for_reviewpanel);
          /* After adding each component to scenario, all selections and inputs in the first panel are cleared to allow for creating a new component */
          this.clearStateSelections();
          this.clearTierSelections();
          this.clearEmissionChanges();
          this.showBaselines = false;
          document.getElementById('step2').style.visibility = 'visible';
          this.emitFromEmissionspanelToReviewpanel(
            this.component_data_for_reviewpanel
          );
        }
      });
    }
  }
  // <------------------------------- Adds the new component based on emissions panel inputs/END ------------------------------>

  // <--------------------------------------- Checks if baseline and control values exist ------------------------------------->
  /* This function sets baseline and control values to zero in case either one of location or tier selections are not made or if baseline and control arrays are empty. */
  checkIfAnyRecordsReturned() {
    if (
      this.tiertree_items_selected != null &&
      this.statetree_items_selected != null
    ) {
      if (
        (this.endApiCall &&
          (this.tiertree_items_selected.length == 0 ||
            this.statetree_items_selected.length == 0)) ||
        (this.endApiCall &&
          this.last_returned_emissions['baseline'].length == 0 &&
          this.last_returned_emissions['control'].length == 0)
      ) {
        this.pm25_baseline = 0;
        this.so2_baseline = 0;
        this.nox_baseline = 0;
        this.nh3_baseline = 0;
        this.voc_baseline = 0;
        this.pm25_control = 0;
        this.so2_control = 0;
        this.nox_control = 0;
        this.nh3_control = 0;
        this.voc_control = 0;
        this.last_returned_emissions['baseline'].push({
          PM25: this.pm25_baseline,
          SO2: this.so2_baseline,
          NOx: this.nox_baseline,
          NH3: this.nh3_baseline,
          VOC: this.voc_baseline,
          fipscodes: this.statetree_items_selected,
          tierselection: this.tiertree_items_selected,
        });
        this.last_returned_emissions['control'].push({
          PM25: this.pm25_control,
          SO2: this.so2_control,
          NOx: this.nox_control,
          NH3: this.nh3_control,
          VOC: this.voc_control,
          fipscodes: this.statetree_items_selected,
          tierselection: this.tiertree_items_selected,
        });
      } else {
        this.endApiCall = false;
      }
    }
    // The validation needs to be called at this point since in the cases that all baselines are zero, for the data that is received from API the baseline and control arrays are empty. So first we need to set all values to zero and then validate the inputs.
    for (var i = 0; i < this.pollutants.length; i++) {
      this.validateEmissionsPanelInput(i);
    }
  }
  // <------------------------------------- Checks if baseline and control values exist/End ----------------------------------->

  // <------------------------------------------- Sets baseline and control values -------------------------------------------->
  /* This function is called after requesting baseline and control data from API in order to set what is shown on the page to new received values. It is also called after clearing either location or tier selections to set all related values to zero. */
  setBaselineAndControl() {
    if (this.last_returned_emissions['baseline'].length == 0) {
      this.pm25_baseline = 0;
      this.so2_baseline = 0;
      this.nox_baseline = 0;
      this.nh3_baseline = 0;
      this.voc_baseline = 0;
      //set baseline rounded up to 0 for all
      this.pollutants = this.pollutants.map((p) => {
        return { ...p, baseline_rounded_up: 0 };
      });
    }
    if (this.last_returned_emissions['control'].length == 0) {
      this.pm25_control = 0;
      this.so2_control = 0;
      this.nox_control = 0;
      this.nh3_control = 0;
      this.voc_control = 0;
    }
    if (this.last_returned_emissions['control'].length != 0) {
      var control = this.last_returned_emissions['control'][0];
      this.pm25_control = control['PM25'];
      this.so2_control = control['SO2'];
      this.nox_control = control['NOx'];
      this.nh3_control = control['NH3'];
      this.voc_control = control['VOC'];
    }
    if (this.last_returned_emissions['baseline'].length != 0) {
      var baseline = this.last_returned_emissions['baseline'][0];
      this.pm25_baseline = baseline['PM25'];
      this.so2_baseline = baseline['SO2'];
      this.nox_baseline = baseline['NOx'];
      this.nh3_baseline = baseline['NH3'];
      this.voc_baseline = baseline['VOC'];
      this.pollutants = this.pollutants.map((pollutant) => {
        return {
          ...pollutant,
          baseline_rounded_up: Math.ceil(baseline[pollutant.name] * 100) / 100,
        };
      });
    }
    this.endApiCall = true;

    if (this.last_returned_emissions['baseline'].length != 0) {
      for (var i = 0; i < this.pollutants.length; i++) {
        this.validateEmissionsPanelInput(i);
      }
    }
    this.checkIfAnyRecordsReturned();
  }
  // <----------------------------------------- Sets baseline and control values/End ------------------------------------------>

  // <-------------------- Requests baseline and control data from API based on location and tier selections ------------------>
  public executedatarequest(): void {
    if (
      this.tiertree_items_selected != null &&
      this.statetree_items_selected != null
    ) {
      if (
        this.tiertree_items_selected.length != 0 &&
        this.statetree_items_selected.length != 0
      ) {
        this.cobraDataService
          .getEmissionsData(
            this.statetree_items_selected,
            this.tiertree_items_selected
          )
          .subscribe(
            (data) => {
              this.last_returned_emissions['baseline'] = data['baseline'];
              this.last_returned_emissions['control'] = data['control'];
              this.setBaselineAndControl();
            },
            (err) =>
              console.error('An error occured getting tier items: ' + err),
            () => {}
          );
      } else {
        this.endApiCall = true;
        this.checkIfAnyRecordsReturned();
      }
    }
  }
  // <------------------- Requests baseline and control data from API based on state and tier selections/End ------------------>

  // <--------------------------------- findWithAttr() is called in createStateAndTierTrees() --------------------------------->
  findWithAttr(array: any, attr: any, value: any) {
    for (var i = 0; i < array.length; i += 1) {
      if (array[i][attr] === value) {
        return i;
      }
    }
    return -1;
  }
  // <------------------------------- findWithAttr() is called in createStateAndTierTrees()/End ------------------------------->

  // <---------------------------------------- Calls emissionspanelToResultspanelEmitter -------------------------------------->
  emitFromEmissionspanelToResultspanel(data: any) {
    this.emissionspanelToResultspanelEmitter.emit(data);
  }
  // <-------------------------------------- Calls emissionspanelToResultspanelEmitter/End ------------------------------------>

  // <-------------------------------------------- Creates state tree and tier tree ------------------------------------------->
  /* Function used to index objects in JS */
  public createStateAndTierTrees(treelevel: any): void {
    /* Builds tier checkbox tree */
    if (treelevel == 'tiers') {
      var tier = [];
      for (var i = 0; i < this.tiertree_items.length; i++) {
        tier.push(
          new Object({
            TIER3NAME: this.tiertree_items[i].TIER3NAME,
            TIER2NAME: this.tiertree_items[i].TIER2NAME,
            TIER1NAME: this.tiertree_items[i].TIER1NAME,
            TIER1: this.tiertree_items[i].TIER1,
            TIER2: this.tiertree_items[i].TIER2,
            TIER3: this.tiertree_items[i].TIER3,
          })
        );
      }
      var uniqueNamestier1 = [];
      var namechecktier1 = [];
      for (var j = 0; j < tier.length; j++) {
        if (namechecktier1.indexOf(tier[j].TIER1NAME) === -1) {
          namechecktier1.push(tier[j].TIER1NAME);
          uniqueNamestier1.push({
            text: tier[j].TIER1NAME,
            TIER1: tier[j].TIER1,
            uniqueid: tier[j].TIER1.toString(),
            items: [],
          });
        }
      }

      var count2 = 0;
      var test = [];
      for (var k = 0; k < tier.length; k++) {
        if (namechecktier1.indexOf(tier[k].TIER1NAME) == count2 + 1) {
          count2++;
          test = [];
        }
        if (test.indexOf(tier[k].TIER2NAME) === -1) {
          var tier2index = this.findWithAttr(
            uniqueNamestier1,
            'text',
            tier[k].TIER1NAME
          );
          test.push(tier[k].TIER2NAME);
          uniqueNamestier1[tier2index]['items'].push({
            text: tier[k].TIER2NAME,
            TIER1: tier[k].TIER1,
            TIER2: tier[k].TIER2,
            uniqueid: tier[k].TIER1.toString() + ',' + tier[k].TIER2.toString(),
            items: [],
          });
        }
      }

      for (var k = 0; k < tier.length; k++) {
        var tier1index = this.findWithAttr(
          uniqueNamestier1,
          'text',
          tier[k].TIER1NAME
        );
        var tier2index = this.findWithAttr(
          uniqueNamestier1[tier1index]['items'],
          'text',
          tier[k].TIER2NAME
        );
        uniqueNamestier1[tier1index]['items'][tier2index]['items'].push({
          text: tier[k].TIER3NAME,
          TIER1: tier[k].TIER1,
          TIER2: tier[k].TIER2,
          TIER3: tier[k].TIER3,
          uniqueid:
            tier[k].TIER1.toString() +
            ',' +
            tier[k].TIER2.toString() +
            ',' +
            tier[k].TIER3.toString(),
        });
      }

      /* Sorts uniqueNamestier1 and its nested arrays */
      uniqueNamestier1.sort(function (a, b) {
        var textA = a.text.toUpperCase();
        var textB = b.text.toUpperCase();
        return textA < textB ? -1 : textA > textB ? 1 : 0;
      });
      for (var i = 0; i < uniqueNamestier1.length; i++) {
        uniqueNamestier1[i].tier1_dd_order = i;
        uniqueNamestier1[i].items.sort(function (a, b) {
          var textA = a.text.toUpperCase();
          var textB = b.text.toUpperCase();
          return textA < textB ? -1 : textA > textB ? 1 : 0;
        });
        for (var j = 0; j < uniqueNamestier1[i].items.length; j++) {
          uniqueNamestier1[i].items[j].tier2_dd_order = j;
          uniqueNamestier1[i].items[j].items.sort(function (a, b) {
            var textA = a.text.toUpperCase();
            var textB = b.text.toUpperCase();
            return textA < textB ? -1 : textA > textB ? 1 : 0;
          });
          for (var k = 0; k < uniqueNamestier1[i].items[j].items.length; k++) {
            uniqueNamestier1[i].items[j].items[k].tier3_dd_order = k;
          }
        }
      }
      this.tiertree_treeview = uniqueNamestier1;
    } else if (treelevel == 'states') {
      /* Builds state checkbox tree */
      var state = [];
      var county = [];
      for (var i = 0; i < this.statetree_items.length; i++) {
        state.push(
          new Object({
            STNAME: this.statetree_items[i].STNAME,
            STFIPS: this.statetree_items[i].STFIPS,
            SOURCEINDX: this.statetree_items[i].SOURCEINDX,
          })
        );
        county.push(
          new Object({
            STNAME: this.statetree_items[i].STNAME,
            CYNAME: this.statetree_items[i].CYNAME,
            CNTYFIPS: this.statetree_items[i].CNTYFIPS,
            FIPS:
              this.statetree_items[i].STFIPS + this.statetree_items[i].CNTYFIPS,
            SOURCEINDX: this.statetree_items[i].SOURCEINDX,
          })
        );
      }

      // <............................................. Creates state_clr_structure ............................................>
      /* state_clr_structure is an array including state and county data with a structure that is needed for creating state tree based on Clarity format. */
      var state_current = null;
      var state_next = null;
      for (var i = 0; i < county.length; i++) {
        state_current = county[i].STNAME;
        if (i === 0) {
          var j = 0;
          this.state_clr_structure.push(
            new Object({
              STNAME: county[i].STNAME,
              STFIPS: county[i].FIPS.substring(0, 2),
              index: i,
              selected: ClrSelectedState.UNSELECTED,
              expanded: false,
              counties: [
                {
                  county: county[i].CYNAME,
                  FIPS: county[i].FIPS,
                  selected: ClrSelectedState.UNSELECTED,
                },
              ],
            })
          );
        }
        if (i + 1 !== county.length) {
          state_next = county[i + 1].STNAME;
          if (state_current === state_next) {
            this.state_clr_structure[j].counties.push({
              county: county[i + 1].CYNAME,
              FIPS: county[i + 1].FIPS,
              selected: ClrSelectedState.UNSELECTED,
            });
          } else {
            j++;
            this.state_clr_structure.push({
              STNAME: county[i + 1].STNAME,
              STFIPS: county[i + 1].FIPS.substring(0, 2),
              index: j,
              selected: ClrSelectedState.UNSELECTED,
              expanded: false,
              counties: [
                {
                  county: county[i + 1].CYNAME,
                  FIPS: county[i + 1].FIPS,
                  selected: ClrSelectedState.UNSELECTED,
                },
              ],
            });
          }
        }
      }

      this.emitFromEmissionspanelToResultspanel(this.state_clr_structure);
      // <............................................. Creates state_clr_structure/End ........................................>

      var uniqueNames = [];
      var namecheck = [];
      for (var j = 0; j < state.length; j++) {
        if (namecheck.indexOf(state[j].STNAME) === -1) {
          namecheck.push(state[j].STNAME);
          uniqueNames.push({
            text: state[j].STNAME,
            STFIPS: state[j].STFIPS,
            SOURCEINDX: -1,
            uniqueid: state[j].STFIPS,
            items: [{}],
          });
        }
      }

      var count = 0;
      var count2 = 0;
      for (var k = 0; k < county.length; k++) {
        if (namecheck.indexOf(county[k].STNAME) == count2 + 1) {
          count = 0;
          count2++;
        }
        uniqueNames[namecheck.indexOf(county[k].STNAME)]['items'][count] = {
          text: county[k].CYNAME,
          STFIPS: state[k].STFIPS,
          CNTYFIPS: county[k].CNTYFIPS,
          SOURCEINDX: county[k].SOURCEINDX,
          uniqueid: state[k].STFIPS + county[k].CNTYFIPS,
        };
        count++;
      }
      this.statetree_treeview = uniqueNames;
    }
    this.cobraDataService.changeSCData(this.statetree_treeview);
  }
  // <-------------------------------------------- Creates state tree and tier tree ------------------------------------------->

  // <----------------------------------------- Creates statetree_items_selected array ---------------------------------------->
  updateStatetreeItemsSelected() {
    this.statetree_items_selected = [];
    this.stateCountyBadgesList = [];
    for (var i = 0; i < this.state_clr_structure.length; i++) {
      if (this.state_clr_structure[i].selected === 1) {
        for (var j = 0; j < this.state_clr_structure[i].counties.length; j++) {
          this.state_clr_structure[i].counties[j].selected = 1;
        }
        this.statetree_items_selected.push(this.state_clr_structure[i].STFIPS);
        if (this.state_clr_structure[i].STFIPS != 11) {
          this.stateCountyBadgesList.push(
            this.state_clr_structure[i].STNAME + ' - All Counties'
          );
        } else {
          this.stateCountyBadgesList.push('District of Columbia - DC');
        }
      } else if (this.state_clr_structure[i].selected === 2) {
        for (var j = 0; j < this.state_clr_structure[i].counties.length; j++) {
          if (this.state_clr_structure[i].counties[j].selected === 1) {
            this.statetree_items_selected.push(
              this.state_clr_structure[i].counties[j].FIPS
            );
            this.stateCountyBadgesList.push(
              this.state_clr_structure[i].counties[j].county +
                ', ' +
                this.state_clr_structure[i].STNAME
            );
          }
        }
      } else {
        for (var j = 0; j < this.state_clr_structure[i].counties.length; j++) {
          this.state_clr_structure[i].counties[j].selected = 0;
        }
      }
    }
    if (this.statetree_items_selected.length == 0) {
      this.last_returned_emissions = { baseline: [], control: [] };
      for (var i = 0; i < this.pollutants.length; i++) {
        this.showErrorLargerThanBaseline[i] = false;
        this.showErrorCantReduceBaseline[i] = false;
      }
    }
    this.activateDeactivateAddToScenarioButton();
    this.executedatarequest();
    if (this.statetree_items_selected.length != 0) {
      this.locationPresent = true;
    } else {
      this.locationPresent = false;
    }
  }
  // <-------------------------------------- Creates statetree_items_selected array/End --------------------------------------->

  // <-------------------------------------------- Clears state and county selections ----------------------------------------->
  public clearStateSelections() {
    var add_to_scenario_btn = document.getElementById('add_to_scenario_btn');
    for (var i = 0; i < this.state_clr_structure.length; i++) {
      this.state_clr_structure[i].selected = ClrSelectedState.UNSELECTED;
      this.state_clr_structure[i].expanded = false;
      for (var j = 0; j < this.state_clr_structure[i].counties.length; j++) {
        this.state_clr_structure[i].counties[j].selected =
          ClrSelectedState.UNSELECTED;
      }
    }
    this.statetree_items_selected = null;
    this.last_returned_emissions = { baseline: [], control: [] };
    for (var i = 0; i < this.pollutants.length; i++) {
      this.showErrorLargerThanBaseline[i] = false;
      this.showErrorCantReduceBaseline[i] = false;
    }
    add_to_scenario_btn.setAttribute('disabled', '');
    this.setBaselineAndControl();
    this.locationPresent = false;
  }
  // <------------------------------------------ Clears state and county selections/End --------------------------------------->

  // <------------------------------------------------ Selects all states entirely -------------------------------------------->
  public selectAllStates() {
    for (var i = 0; i < this.state_clr_structure.length; i++) {
      this.state_clr_structure[i].selected = ClrSelectedState.SELECTED;
      for (var j = 0; j < this.state_clr_structure[i].counties.length; j++) {
        this.state_clr_structure[i].counties[j].selected =
          ClrSelectedState.SELECTED;
      }
    }
    this.updateStatetreeItemsSelected();
  }
  // <------------------------------------------------ Selects all states entirely -------------------------------------------->

  // <--------------------- Updates Tier2 Dropdown and updates tiertree_items_selected once changing Tier1 -------------------->
  /* index_tier1 is the value of the selected option in the dropdown and is the same as tier1_dd_order. */
  /* In the case that no option is selected, index1 equals -1 */
  public updateTier2Dropdown(index: any) {
    var tier1_current_item = this.tiertree_treeview[index];
    this.index_tier1 = index;
    this.tier2_items = tier1_current_item.items;
    var target_dd2 = document.getElementById('tier2');
    target_dd2.removeAttribute('disabled');
    this.tier3_items = [];
    var target_dd3 = document.getElementById('tier3');
    target_dd3.setAttribute('disabled', '');
    this.tiertree_items_selected = [tier1_current_item.TIER1.toString()];
    this.activateDeactivateAddToScenarioButton();
    this.executedatarequest();
    this.tierPresent = true;
    this.tier1_selection_text = tier1_current_item.text;
    this.tier2_selection_text = null;
    this.tier3_selection_text = null;
  }
  // <------------------- Updates Tier2 Dropdown and updates tiertree_items_selected once changing Tier1/End ------------------>

  // <--------------------- Updates Tier3 Dropdown and updates tiertree_items_selected once changing Tier2 -------------------->
  public updateTier3Dropdown(index: any) {
    var tier2_current_item =
      this.tiertree_treeview[this.index_tier1].items[index];
    this.index_tier2 = index;
    if (index !== 'all') {
      this.tier3_items = tier2_current_item.items;
      var target_dd = document.getElementById('tier3');
      target_dd.removeAttribute('disabled');
      this.tiertree_items_selected = [
        tier2_current_item.TIER1 + ',' + tier2_current_item.TIER2,
      ];
      this.tier2_selection_text = tier2_current_item.text;
    } else {
      this.tier3_items = [];
      var target_dd3 = document.getElementById('tier3');
      target_dd3.setAttribute('disabled', '');
      this.tiertree_items_selected = [this.index_tier1.toString()];
      this.tier2_selection_text = null;
    }
    this.executedatarequest();
    this.tier3_selection_text = null;
  }
  // <------------------- Updates Tier3 Dropdown and updates tiertree_items_selected once changing Tier2/End ------------------>

  // <---------------------------------- Updates tiertree_items_selected once changing Tier3 ---------------------------------->
  public updateItemsSelectedTier3(index: any) {
    var tier3_current_item =
      this.tiertree_treeview[this.index_tier1].items[this.index_tier2].items[
        index
      ];
    if (index !== 'all') {
      this.tiertree_items_selected = [
        tier3_current_item.TIER1 +
          ',' +
          tier3_current_item.TIER2 +
          ',' +
          tier3_current_item.TIER3,
      ];
      if (tier3_current_item.TIER3 == 99) {
        this.tier3_selection_text = 'OTHER';
      } else {
        this.tier3_selection_text = tier3_current_item.text;
      }
    } else if (this.index_tier2 !== 'all') {
      this.tiertree_items_selected = [
        tier3_current_item.TIER1 + ',' + tier3_current_item.TIER2,
      ];
    } else {
      this.tiertree_items_selected = [tier3_current_item.TIER1.toString()];
    }
    if (index == 'all') {
      this.tier3_selection_text = null;
    }
    this.executedatarequest();
  }
  // <-------------------------------- Updates tiertree_items_selected once changing Tier3/End -------------------------------->

  // <-------------------------------------------------- Clears tier selections ----------------------------------------------->
  public clearTierSelections() {
    var add_to_scenario_btn = document.getElementById('add_to_scenario_btn');
    var tier1_dropdown = document.getElementById('tier1') as HTMLSelectElement;
    tier1_dropdown.selectedIndex = 0;
    this.tier2_items = [];
    var target_dd2 = document.getElementById('tier2');
    target_dd2.setAttribute('disabled', '');
    this.tier3_items = [];
    var target_dd3 = document.getElementById('tier3');
    target_dd3.setAttribute('disabled', '');
    this.tiertree_items_selected = null;
    this.last_returned_emissions = { baseline: [], control: [] };
    for (var i = 0; i < 5; i++) {
      this.showErrorLargerThanBaseline[i] = false;
      this.showErrorCantReduceBaseline[i] = false;
    }
    add_to_scenario_btn.setAttribute('disabled', '');
    this.setBaselineAndControl();
    this.tierPresent = false;
  }
  // <------------------------------------------------ Clears tier selections/End --------------------------------------------->

  // <----------------------------------- Hides and shows baseline values in Emissions subpanel ------------------------------->
  public checkIfBaselinesShow() {
    if (this.locationPresent && this.tierPresent) {
      this.showBaselines = true;
    } else {
      this.showBaselines = false;
    }
  }
  // <--------------------------------- Hides and shows baseline values in Emissions subpanel/End ----------------------------->

  // <--------------------------------------------- Clears emissions subpanel inputs ------------------------------------------>
  clearEmissionChanges() {
    var add_to_scenario_btn = document.getElementById('add_to_scenario_btn');
    //set ri_switch_model: 'reduce', pt_switch_model: 'tons', and input_model: null for all pollutants;
    this.pollutants = this.pollutants.map((p) => {
      return {
        ...p,
        ri_switch_model: 'reduce',
        pt_switch_model: 'tons',
        input_model: null,
      };
    });

    if (this.last_returned_emissions['baseline'].length == 0) {
      this.pm25_baseline = 0;
      this.so2_baseline = 0;
      this.nox_baseline = 0;
      this.nh3_baseline = 0;
      this.voc_baseline = 0;
      //set all pollutants baseline_rounded_up to 0
      //set baseline rounded up to 0 for all pollutants
      this.pollutants = this.pollutants.map((p) => {
        return { ...p, baseline_rounded_up: 0 };
      });
    }
    if (this.last_returned_emissions['control'].length == 0) {
      this.pm25_control = 0;
      this.so2_control = 0;
      this.nox_control = 0;
      this.nh3_control = 0;
      this.voc_control = 0;
    }
    if (this.last_returned_emissions['control'].length != 0) {
      var control = this.last_returned_emissions['control'][0];
      control['PM25'] = this.pm25_baseline;
      control['SO2'] = this.so2_baseline;
      control['NOx'] = this.nox_baseline;
      control['NH3'] = this.nh3_baseline;
      control['VOC'] = this.voc_baseline;
      this.pm25_control = this.pm25_baseline;
      this.so2_control = this.so2_baseline;
      this.nox_control = this.nox_baseline;
      this.nh3_control = this.nh3_baseline;
      this.voc_control = this.voc_baseline;
    }
    this.showErrorNotNumber = [false, false, false, false, false];
    this.showErrorOutOfRange = [false, false, false, false, false];
    this.showErrorLargerThanBaseline = [false, false, false, false, false];
    this.showErrorCantReduceBaseline = [false, false, false, false, false];
    add_to_scenario_btn.setAttribute('disabled', '');
  }
  // <------------------------------------------- Clears emissions subpanel inputs/End ---------------------------------------->

  // <-------------------------------------------- Validates inputs in Emissions panel ---------------------------------------->
  public validateEmissionsPanelInput(i: number) {
    var inputValues = this.pollutants.map((p) => p.input_model);

    var inputValue = inputValues[i];
    var baselines = this.pollutants.map((p) => p.baseline_rounded_up);
    var baselineValue = baselines[i];
    var reduceIncreaseToggles = this.pollutants.map((p) => p.ri_switch_model);
    var reduceIncreaseToggle = reduceIncreaseToggles[i];
    var tonsPercentToggles = this.pollutants.map((p) => p.pt_switch_model);

    var tonsPercentToggle = tonsPercentToggles[i];
    // remove all error messages for current input to validate again
    this.showErrorNotNumber[i] = false;
    this.showErrorOutOfRange[i] = false;
    this.showErrorLargerThanBaseline[i] = false;
    this.showErrorCantReduceBaseline[i] = false;
    // validate the inputs
    var validNumber =
      /^[+]?([0-9]+(?:[\.][0-9]*)?|\.[0-9]+)$/.test(inputValue) ||
      inputValue == undefined ||
      inputValue == '';
    if (!validNumber) {
      this.showErrorNotNumber[i] = true;
    } else {
      if (reduceIncreaseToggle == 'reduce' && tonsPercentToggle == 'percent') {
        if ((inputValue < 0 || inputValue > 100) && baselineValue != 0) {
          this.showErrorOutOfRange[i] = true;
        }
      }
      if (
        this.statetree_items_selected != null &&
        this.tiertree_items_selected != null &&
        this.statetree_items_selected.length != 0 &&
        this.last_returned_emissions['baseline'].length != 0 &&
        baselineValue != undefined
      ) {
        if (reduceIncreaseToggle == 'reduce' && tonsPercentToggle == 'tons') {
          if (baselineValue != 0 && inputValue > baselineValue) {
            this.showErrorLargerThanBaseline[i] = true;
          }
          if (baselineValue == 0 && inputValue > baselineValue) {
            this.showErrorCantReduceBaseline[i] = true;
          }
        }
        if (
          reduceIncreaseToggle == 'reduce' &&
          tonsPercentToggle == 'percent'
        ) {
          if (baselineValue == 0 && inputValue > 0) {
            this.showErrorCantReduceBaseline[i] = true;
          }
        }
      }
    }
    this.activateDeactivateAddToScenarioButton();
  }
  // <------------------------------------------ Validates inputs in Emissions panel/End -------------------------------------->

  // <------------------------------------- Activates and deactivates Add to Scenario button ---------------------------------->
  public activateDeactivateAddToScenarioButton() {
    var inputValues = this.pollutants.map((p) => p.input_model);
    var add_to_scenario_btn = document.getElementById('add_to_scenario_btn');
    if (
      this.statetree_items_selected == null ||
      this.tiertree_items_selected == null
    ) {
      add_to_scenario_btn.setAttribute('disabled', '');
    } else if (
      this.statetree_items_selected.length == 0 ||
      this.tiertree_items_selected.length == 0
    ) {
      add_to_scenario_btn.setAttribute('disabled', '');
    } else if (
      inputValues.filter((val) => val !== undefined && val !== '').length === 0
    ) {
      add_to_scenario_btn.setAttribute('disabled', '');
    } else if (
      this.showErrorNotNumber.indexOf(true) != -1 ||
      this.showErrorOutOfRange.indexOf(true) != -1 ||
      this.showErrorLargerThanBaseline.indexOf(true) != -1 ||
      this.showErrorCantReduceBaseline.indexOf(true) != -1
    ) {
      add_to_scenario_btn.setAttribute('disabled', '');
    } else {
      add_to_scenario_btn.removeAttribute('disabled');
    }
  }
  // <----------------------------------- Activates and deactivates Add to Scenario button/End -------------------------------->

  // <----------------------------------- Checks if there is any conflicts between components --------------------------------->
  /* This function checks if the new component that we are trying to create is in conflict with any other component that is already added to scenario. If any conflict is detected; 'foundConflict' is set to true, an alert modal appears on the page and the new component is not added to scenario. */
  public checkForPossibleConflicts(
    newComponentData: any,
    dataIsAddedToForm: boolean,
    queueElement: any
  ) {
    this.foundConflict = false;

    var presentCounties = [];

    if (dataIsAddedToForm == true) {
      /* Create an array listing FIPS codes of all selected counties in the new component. */
      for (var i = 0; i < this.state_clr_structure.length; i++) {
        if (
          this.state_clr_structure[i].selected == 1 ||
          this.state_clr_structure[i].selected == 2
        ) {
          for (
            var j = 0;
            j < this.state_clr_structure[i].counties.length;
            j++
          ) {
            if (this.state_clr_structure[i].counties[j].selected === 1) {
              presentCounties.push(
                this.state_clr_structure[i].counties[j].FIPS
              );
            }
          }
        }
      }

      /* Create an array listing all pollutants that have an input value in the new component. */
      var presentPollutants = [];
      if (newComponentData['cPM25']) presentPollutants.push('PM25');
      if (newComponentData['cSO2']) presentPollutants.push('SO2');
      if (newComponentData['cNOX']) presentPollutants.push('NOx');
      if (newComponentData['cNH3']) presentPollutants.push('NH3');
      if (newComponentData['cVOC']) presentPollutants.push('VOC');
    }

    if (dataIsAddedToForm == false) {
      /* Create an array listing FIPS codes of all selected counties in the component added for AVERT case. */
      var statetree_items_selected = queueElement.statetree_items_selected;
      for (var i = 0; i < statetree_items_selected.length; i++) {
        if (statetree_items_selected[i].length == 2) {
          for (var j = 0; j < this.state_clr_structure.length; j++) {
            if (
              this.state_clr_structure[j].STFIPS == statetree_items_selected[i]
            ) {
              for (
                var k = 0;
                k < this.state_clr_structure[j].counties.length;
                k++
              ) {
                presentCounties.push(
                  this.state_clr_structure[j].counties[k].FIPS
                );
              }
              break;
            }
          }
        } else {
          presentCounties.push(statetree_items_selected[i]);
        }
      }

      /* Create an array listing all pollutants that are present in the component added for AVERT case. */
      var presentPollutants = [];
      if (queueElement['cPM25']) presentPollutants.push('PM25');
      if (queueElement['cSO2']) presentPollutants.push('SO2');
      if (queueElement['cNOX']) presentPollutants.push('NOx');
      if (queueElement['cNH3']) presentPollutants.push('NH3');
      if (queueElement['cVOC']) presentPollutants.push('VOC');
    }

    /* Create an array listing all the combinations that exist in the new component based on the selections made in this component. */
    if (dataIsAddedToForm == true)
      var presentTiersCode = newComponentData['tiertree_items_selected'];
    if (dataIsAddedToForm == false)
      var presentTiersCode = queueElement['tiertree_items_selected'];
    var presentTiersCode = newComponentData['tiertree_items_selected'];
    var currentCombination = '';
    var newComponentExistingCombinations = [];
    for (var i = 0; i < presentCounties.length; i++) {
      for (var j = 0; j < presentPollutants.length; j++) {
        currentCombination =
          presentCounties[i] +
          '-' +
          presentPollutants[j] +
          '-' +
          presentTiersCode;
        newComponentExistingCombinations.push(currentCombination);
      }
    }

    /* Create an array listing all possible combinations that can be considered as parent for the combinations that exist in the new component. Parent is referring to tier selections that implicitly include 'presentTiersCode'.) */
    var possibleParentCombinationsForNewComponent = [];
    var tierCodeSplits = presentTiersCode[0].split(',');
    if (tierCodeSplits.length == 2) {
      for (var i = 0; i < presentCounties.length; i++) {
        for (var j = 0; j < presentPollutants.length; j++) {
          currentCombination =
            presentCounties[i] +
            '-' +
            presentPollutants[j] +
            '-' +
            tierCodeSplits[0];
          possibleParentCombinationsForNewComponent.push(currentCombination);
        }
      }
    }
    if (tierCodeSplits.length == 3) {
      for (var i = 0; i < presentCounties.length; i++) {
        for (var j = 0; j < presentPollutants.length; j++) {
          currentCombination =
            presentCounties[i] +
            '-' +
            presentPollutants[j] +
            '-' +
            tierCodeSplits[0];
          possibleParentCombinationsForNewComponent.push(currentCombination);
        }
      }
      for (var i = 0; i < presentCounties.length; i++) {
        for (var j = 0; j < presentPollutants.length; j++) {
          currentCombination =
            presentCounties[i] +
            '-' +
            presentPollutants[j] +
            '-' +
            tierCodeSplits[0] +
            ',' +
            tierCodeSplits[1];
          possibleParentCombinationsForNewComponent.push(currentCombination);
        }
      }
    }

    /* There are three types of conflicts:
      type1: Explicit conflicts which happen when at least one combination that exists in review panel is recreated in the new component.
      type2: At least one combination in the new component is child for review panel.
      type3: At least one combination in the new component is parent for review panel.
    */
    /* Check for type1 and type2 conflicts. */
    for (var i = 0; i < this.reviewPanelComponents.length; i++) {
      var componentFromListExistingCombinations =
        this.reviewPanelComponents[i]['listOfExistingCombinations'];
      for (var j = 0; j < newComponentExistingCombinations.length; j++) {
        // Check for explicit conflicts(type1).
        if (
          componentFromListExistingCombinations.indexOf(
            newComponentExistingCombinations[j]
          ) > -1
        ) {
          this.foundConflict = true;
          this.showConflictModal = true;
          return;
        }
      }
      for (
        var j = 0;
        j < possibleParentCombinationsForNewComponent.length;
        j++
      ) {
        // Check if any of the possible parent combinations of the new component already exists in review panel(type2).
        if (
          componentFromListExistingCombinations.indexOf(
            possibleParentCombinationsForNewComponent[j]
          ) > -1
        ) {
          this.foundConflict = true;
          this.showConflictModal = true;
          return;
        }
      }
    }

    // For every component in the review panel, check if any of its possible parent combinations is created in the new component. It would mean to check if any combination in the review panel can be considered as a child for what is created in the new component(type3).
    for (var i = 0; i < this.reviewPanelComponents.length; i++) {
      var componentFromListPossibleParentCombinations =
        this.reviewPanelComponents[i]['listOfPossibleParentCombinations'];
      for (
        var j = 0;
        j < componentFromListPossibleParentCombinations.length;
        j++
      ) {
        if (
          newComponentExistingCombinations.indexOf(
            componentFromListPossibleParentCombinations[j]
          ) > -1
        ) {
          this.foundConflict = true;
          this.showConflictModal = true;
          return;
        }
      }
    }

    if (!this.foundConflict) {
      this.component_data_for_reviewpanel['listOfExistingCombinations'] =
        newComponentExistingCombinations;
      this.component_data_for_reviewpanel['listOfPossibleParentCombinations'] =
        possibleParentCombinationsForNewComponent;
    }
  }
  // <--------------------------------- Checks if there is any conflicts between components/End ------------------------------->

  // <----------------------------------- Removes component from reviewPanelComponents array ---------------------------------->
  /* This function is called when a component is removed from review panel to also remove that component from reviewPanelComponents array which is defiened in emissions panel and is needed when looking for conflicts. */
  removeComponentFromReviewPanelComponentsArray(index: number) {
    this.reviewPanelComponents.splice(
      this.reviewPanelComponents.length - (index + 1),
      1
    );
  }
  // <----------------------------------- Removes component from reviewPanelComponents array ---------------------------------->

  // <----------------------------- Resets emissions panel when Build New Scenario button is clicked -------------------------->
  resetEmissionsPanel() {
    this.clearStateSelections();
    this.clearTierSelections();
    this.clearEmissionChanges();
    this.showBaselines = false;
    this.index_tier1 = '';
    this.index_tier2 = '';
    this.tier2_items = [];
    this.tier3_items = [];
    this.last_returned_emissions = { baseline: [], control: [] };
    this.foundConflict = false;
    this.reviewPanelComponents = [];
    this.stateCountyBadgesList = [];
    this.tier1_selection_text = null;
    this.tier2_selection_text = null;
    this.tier3_selection_text = null;
    this.component_data_for_reviewpanel = null;
  }
  // <--------------------------- Resets emissions panel when Build New Scenario button is clicked/End ------------------------>

  // <------------------------------------ Clears are selections and inputs on emissions panel -------------------------------->
  clearEmissionsPanel() {
    this.clearStateSelections();
    this.clearTierSelections();
    this.clearEmissionChanges();
  }
  // <---------------------------------- Clears are selections and inputs on emissions panel/End ------------------------------>
}
