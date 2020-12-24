import { Component, ViewEncapsulation, OnInit, Input, Output, EventEmitter } from '@angular/core';
import { Token } from '../../Token';
import { CobraDataService } from '../../cobra-data-service.service';
import { NgForm } from '@angular/forms';
import { ClrSelectedState } from '@clr/angular';
@Component({
  selector: 'app-emissionspanel',
  templateUrl: './emissionspanel.component.html',
  styleUrls: ['../../../../node_modules/@clr/ui/clr-ui.css','../../../theme/styles.scss','./emissionspanel.component.scss'],
  encapsulation: ViewEncapsulation.None
})

export class EmissionspanelComponent implements OnInit {
  @Input() token: Token;
  @Output() emissionspanelToReviewpanelEmitter = new EventEmitter<any>();
  @Output() emissionspanelToResultspanelEmitter = new EventEmitter<any>();
  
  constructor(private cobraDataService: CobraDataService) { }
  
  /* Location variables */
  private statetree_items: any[] = null;
  public statetree_treeview: any[];
  public state_clr_structure: any[] = [];
  public statetree_items_selected: any = null;

  /* Tier variables */
  public tiertree_items: any[] = null;
  public tiertree_treeview: any[];
  public tiertree_items_selected: any[] = null;
  public index_tier1 = '';
  public index_tier2 = '';
  public tier2_items: any[] = [];
  public tier3_items: any[] = [];
  
  /* Emissions variables */
  public last_returned_emissions: any = {baseline: [], control: []};
  public pm25_baseline: number;
  public so2_baseline: number;
  public nox_baseline: number;
  public nh3_baseline: number;
  public voc_baseline: number;
  public pm25_baseline_rounded_up: number;
  public so2_baseline_rounded_up: number;
  public nox_baseline_rounded_up: number;
  public nh3_baseline_rounded_up: number;
  public voc_baseline_rounded_up: number;
  public pm25_control: number;
  public so2_control: number;
  public nox_control: number;
  public nh3_control: number;
  public voc_control: number;
  PM25ri = 'reduce';
  PM25pt = 'tons';
  SO2ri = 'reduce';
  SO2pt = 'tons';
  NOXri = 'reduce';
  NOXpt = 'tons';
  NH3ri = 'reduce';
  NH3pt = 'tons';
  VOCri = 'reduce';
  VOCpt = 'tons';
  public cPM25: any;
  public cSO2: any;
  public cNOX: any;
  public cNH3: any;
  public cVOC: any;

  /* variables to show and hide baseline values */
  public locationPresent: boolean = false;
  public tierPresent: boolean = false;
  public showBaselines: boolean = false;

  /* variables to show and hide error messages */
  public showErrorNotNumber: any = [false, false, false, false, false];
  // the input should be in the range of 0<=x<=100
  public showErrorOutOfRange: any = [false, false, false, false, false];
  public showErrorLargerThanBaseline: any = [false, false, false, false, false];
  public showErrorCantReduceBaseline: any = [false, false, false, false, false];

  /* Other variables */
  public endApiCall: boolean;
  public reviewPanelComponents: any[] = [];
  public foundConflict: boolean = false;

  /* variables to pass to review panel */
  public stateCountyBadgesList = [];
  public tier1_selection_text = null;
  public tier2_selection_text = null;
  public tier3_selection_text = null;
  public component_data_for_reviewpanel = null;

  // <----------------------------------------- Requests state and tier data from API ----------------------------------------->
  ngOnInit() {
    this.cobraDataService.getDataDictionary_Tiers().subscribe(
      data => { this.tiertree_items = data;
                this.createStateAndTierTrees('tiers');
              },
      err => console.error('An error occured retrieving tier items: ' + err),
      () => console.log('Retrieved tier items')
    );
    this.cobraDataService.getDataDictionary_State().subscribe(
      data => { this.statetree_items = data;
                this.createStateAndTierTrees('states');
              },
      err => console.error('An error occured retrieving state items: ' + err),
      () => console.log('Retrieved state items')
    );
  }
  // <--------------------------------------- Requests state and tier data from API/End --------------------------------------->

  // <---------------------------------------- Calls emissionspanelToReviewpanelEmitter --------------------------------------->
  emitFromEmissionspanelToReviewpanel(data: any) {
    this.emissionspanelToReviewpanelEmitter.emit(data);
  }
  // <-------------------------------------- Calls emissionspanelToReviewpanelEmitter/End ------------------------------------->

  // <--------------------------------------- Applies changes based on subpanel C inputs -------------------------------------->
  addComponentToScenario(modifyemissionsform: NgForm): void {
    var chem_list = ['PM25', 'SO2', 'NOX', 'NH3', 'VOC'];
    var value = 0;
    var conversion = "";
    var adjustment = "";
    for (var i = 0; i < chem_list.length; i++) {
      value = parseFloat(modifyemissionsform.value["change" + chem_list[i]]);
      if (value === undefined || value== null || isNaN(value)) value = 0;
      conversion = modifyemissionsform.value[chem_list[i] + "pt"];
      adjustment = modifyemissionsform.value[chem_list[i] + "ri"];
      if (conversion == "percent") {
          if (chem_list[i] == 'PM25') value = this.pm25_baseline * value / 100;
          if (chem_list[i] == 'SO2') value = this.so2_baseline * value / 100;
          if (chem_list[i] == 'NOX') value = this.nox_baseline * value / 100;
          if (chem_list[i] == 'NH3') value = this.nh3_baseline * value / 100;
          if (chem_list[i] == 'VOC') value = this.voc_baseline * value / 100;
      }
      if (adjustment == "reduce") {
          if (chem_list[i] == 'PM25') this.pm25_control = this.pm25_baseline - value;
          if (chem_list[i] == 'SO2') this.so2_control = this.so2_baseline - value;
          if (chem_list[i] == 'NOX') this.nox_control = this.nox_baseline - value;
          if (chem_list[i] == 'NH3') this.nh3_control = this.nh3_baseline - value;
          if (chem_list[i] == 'VOC') this.voc_control = this.voc_baseline - value;
      } else if (adjustment == "increase") {
          if (chem_list[i] == 'PM25') this.pm25_control = this.pm25_baseline + value;
          if (chem_list[i] == 'SO2') this.so2_control = this.so2_baseline + value;
          if (chem_list[i] == 'NOX') this.nox_control = this.nox_baseline + value;
          if (chem_list[i] == 'NH3') this.nh3_control = this.nh3_baseline + value;
          if (chem_list[i] == 'VOC') this.voc_control = this.voc_baseline + value;
        }
    }

    /* This code snippet updates last_returned_emissions */
    let updatePacket = {};
      updatePacket["PM25"] = this.pm25_control;
      updatePacket["SO2"] = this.so2_control;
      updatePacket["NO2"] = this.nox_control;
      updatePacket["NH3"] = this.nh3_control;
      updatePacket["VOC"] = this.voc_control;
      updatePacket["fipscodes"] = this.statetree_items_selected;
      updatePacket["tierselection"] = this.tiertree_items_selected;

    // set control data to new inputs
    this.last_returned_emissions["control"][0] = updatePacket;

    /* This code snippet updates data that is going to be passed to reviewpanel */
    this.component_data_for_reviewpanel = {};
    this.component_data_for_reviewpanel["stateCountyBadgesList"] = this.stateCountyBadgesList;
    this.component_data_for_reviewpanel["tier1Text"] = this.tier1_selection_text;
    this.component_data_for_reviewpanel["tier2Text"] = this.tier2_selection_text;
    this.component_data_for_reviewpanel["tier3Text"] = this.tier3_selection_text;
    this.component_data_for_reviewpanel["PM25ri"] = this.PM25ri;
    this.component_data_for_reviewpanel["SO2ri"] = this.SO2ri;
    this.component_data_for_reviewpanel["NOXri"] = this.NOXri;
    this.component_data_for_reviewpanel["NH3ri"] = this.NH3ri;
    this.component_data_for_reviewpanel["VOCri"] = this.VOCri;
    this.component_data_for_reviewpanel["cPM25"] = this.cPM25;
    this.component_data_for_reviewpanel["cSO2"] = this.cSO2;
    this.component_data_for_reviewpanel["cNOX"] = this.cNOX;
    this.component_data_for_reviewpanel["cNH3"] = this.cNH3;
    this.component_data_for_reviewpanel["cVOC"] = this.cVOC;
    this.component_data_for_reviewpanel["PM25pt"] = this.PM25pt;
    this.component_data_for_reviewpanel["SO2pt"] = this.SO2pt;
    this.component_data_for_reviewpanel["NOXpt"] = this.NOXpt;
    this.component_data_for_reviewpanel["NH3pt"] = this.NH3pt;
    this.component_data_for_reviewpanel["VOCpt"] = this.VOCpt;
    // values used for the backend computations
    this.component_data_for_reviewpanel["statetree_items_selected"] = this.statetree_items_selected;
    this.component_data_for_reviewpanel["tiertree_items_selected"] = this.tiertree_items_selected;
    this.component_data_for_reviewpanel["updatePacket"] = updatePacket;

    this.checkForPossibleConflicts(this.component_data_for_reviewpanel);

    if (!this.foundConflict) {
      /* All added components are saved in this array(backend computations are called later once clicking on Run Scenario button) */
      this.reviewPanelComponents.push(this.component_data_for_reviewpanel);
      /* After adding each component to scenario, all selections and inputs in the first panel are cleared to allow for creating a new component */
      this.clearStateSelections();
      this.clearTierSelections();
      this.clearEmissionChanges();
      this.showBaselines = false;
      this.emitFromEmissionspanelToReviewpanel(this.component_data_for_reviewpanel);
    }
  }
  // <------------------------------------- Applies changes based on subpanel C inputs/End ------------------------------------>
  
  // <--------------------------------------- Checks if baseline and control values exist ------------------------------------->
  /* This function sets baseline and control values to zero in case either one of location or tier selections are not made or if baseline and control arrays are empty. */
  checkIfAnyRecordsReturned() {
    if (this.tiertree_items_selected != null && this.statetree_items_selected != null) {
      if ((this.endApiCall && (this.tiertree_items_selected.length == 0 || this.statetree_items_selected.length == 0)) || (this.endApiCall && (this.last_returned_emissions["baseline"].length == 0 && this.last_returned_emissions["control"].length == 0))) {
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
        this.last_returned_emissions["baseline"].push({ "PM25": this.pm25_baseline, "SO2": this.so2_baseline, "NO2": this.nox_baseline, "NH3": this.nh3_baseline, "VOC": this.voc_baseline, "fipscodes": this.statetree_items_selected, "tierselection": this.tiertree_items_selected });
        this.last_returned_emissions["control"].push({ "PM25": this.pm25_control, "SO2": this.so2_control, "NO2": this.nox_control, "NH3": this.nh3_control, "VOC": this.voc_control, "fipscodes": this.statetree_items_selected, "tierselection": this.tiertree_items_selected });
      } else {
        this.endApiCall = false;
      }
    }
    // The validation needs to be called at this point since in the cases that all baselines are zero, for the data that is received from API the baseline and control arrays are empty. So first we need to set all values to zero and then validate the inputs.
    for (var i = 0; i < 5; i++) {
      this.validateEmissionsPanelInput(i);
    }
  }
  // <------------------------------------- Checks if baseline and control values exist/End ----------------------------------->

  // <------------------------------------------- Sets baseline and control values -------------------------------------------->
  /* This function is called after requesting baseline and control data from API in order to set what is shown on the page to new received values. It is also called after clearing either location or tier selections to set all related values to zero. */
  setBaselineAndControl() {
    if (this.last_returned_emissions["baseline"].length == 0) {
      this.pm25_baseline = 0;
      this.so2_baseline = 0;
      this.nox_baseline = 0;
      this.nh3_baseline = 0;
      this.voc_baseline = 0;
    }
    if (this.last_returned_emissions["control"].length == 0) {
      this.pm25_control = 0;
      this.so2_control = 0;
      this.nox_control = 0;
      this.nh3_control = 0;
      this.voc_control = 0;
    }
    if (this.last_returned_emissions["control"].length != 0) {
      var control = this.last_returned_emissions["control"][0];
      this.pm25_control = control["PM25"];
      this.so2_control = control["SO2"];
      this.nox_control = control["NO2"];
      this.nh3_control = control["NH3"];
      this.voc_control = control["VOC"];
    }
    if (this.last_returned_emissions["baseline"].length != 0) { 
      var baseline = this.last_returned_emissions["baseline"][0];
      this.pm25_baseline = baseline["PM25"];
      this.so2_baseline = baseline["SO2"];
      this.nox_baseline = baseline["NO2"];
      this.nh3_baseline = baseline["NH3"];
      this.voc_baseline = baseline["VOC"];
      this.pm25_baseline_rounded_up = Math.ceil(baseline["PM25"]*100)/100;
      this.so2_baseline_rounded_up = Math.ceil(baseline["SO2"]*100)/100;
      this.nox_baseline_rounded_up = Math.ceil(baseline["NO2"]*100)/100;
      this.nh3_baseline_rounded_up = Math.ceil(baseline["NH3"]*100)/100;
      this.voc_baseline_rounded_up = Math.ceil(baseline["VOC"]*100)/100;
    }
    this.endApiCall = true;

    if (this.last_returned_emissions["baseline"].length != 0) {
      for (var i = 0; i < 5; i++) {
        this.validateEmissionsPanelInput(i);
      }
    }
    this.checkIfAnyRecordsReturned();
  }
  // <----------------------------------------- Sets baseline and control values/End ------------------------------------------>

  // <-------------------- Requests baseline and control data from API based on location and tier selections ------------------>
  public executedatarequest(): void {
    if (this.tiertree_items_selected != null && this.statetree_items_selected != null) {
      if (this.tiertree_items_selected.length != 0 && this.statetree_items_selected.length != 0) {
        this.cobraDataService.getEmissionsData(this.statetree_items_selected, this.tiertree_items_selected).subscribe(
          data => {
            this.last_returned_emissions["baseline"] = data["baseline"];
            this.last_returned_emissions["control"] = data["control"];
            this.setBaselineAndControl();
          },
          err => console.error('An error occured getting tier items: ' + err),
          () => {
            console.log('Retrieved tier items.');
          }
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
        tier.push(new Object({ 'TIER3NAME': this.tiertree_items[i].TIER3NAME, 'TIER2NAME': this.tiertree_items[i].TIER2NAME, 'TIER1NAME': this.tiertree_items[i].TIER1NAME, 'TIER1': this.tiertree_items[i].TIER1, 'TIER2': this.tiertree_items[i].TIER2, 'TIER3': this.tiertree_items[i].TIER3 }));
      }
      var uniqueNamestier1 = [];
      var namechecktier1 = [];
      for (var j = 0; j < tier.length; j++) {
        if (namechecktier1.indexOf(tier[j].TIER1NAME) === -1) {
          namechecktier1.push(tier[j].TIER1NAME);
          uniqueNamestier1.push({ 'text': tier[j].TIER1NAME, 'TIER1': tier[j].TIER1, 'uniqueid': tier[j].TIER1.toString(), 'items': [] });
        }
      }

      var count2 = 0;
      var test = [];
      for (var k = 0; k < tier.length; k++) {
        if (namechecktier1.indexOf(tier[k].TIER1NAME) == (count2 + 1)) {
          count2++;
          test = [];
        }
        if (test.indexOf(tier[k].TIER2NAME) === -1) {
          var tier2index = this.findWithAttr(uniqueNamestier1, 'text', tier[k].TIER1NAME);
          test.push(tier[k].TIER2NAME);
          uniqueNamestier1[tier2index]['items'].push({ 'text': tier[k].TIER2NAME, 'TIER1': tier[k].TIER1, 'TIER2': tier[k].TIER2, 'uniqueid': tier[k].TIER1.toString() + ',' + tier[k].TIER2.toString(), 'items': [] });
        }
      }

      for (var k = 0; k < tier.length; k++) {
        var tier1index = this.findWithAttr(uniqueNamestier1, 'text', tier[k].TIER1NAME);
        var tier2index = this.findWithAttr(uniqueNamestier1[tier1index]['items'], 'text', tier[k].TIER2NAME);
        uniqueNamestier1[tier1index]['items'][tier2index]['items'].push({ 'text': tier[k].TIER3NAME, 'TIER1': tier[k].TIER1, 'TIER2': tier[k].TIER2, 'TIER3': tier[k].TIER3, 'uniqueid': tier[k].TIER1.toString() + ',' + tier[k].TIER2.toString() + ',' + tier[k].TIER3.toString() });
      }
      this.tiertree_treeview = uniqueNamestier1;
    }

    /* Builds state checkbox tree */
    else if (treelevel == 'states') {
      var state = [];
      var county = [];
      for (var i = 0; i < this.statetree_items.length; i++) {
        state.push(new Object({ 
          'STNAME': this.statetree_items[i].STNAME, 
          'STFIPS': this.statetree_items[i].STFIPS, 
          'SOURCEINDX': this.statetree_items[i].SOURCEINDX 
        }));
        county.push(new Object({ 
          'STNAME': this.statetree_items[i].STNAME, 
          'CYNAME': this.statetree_items[i].CYNAME, 
          'CNTYFIPS': this.statetree_items[i].CNTYFIPS, 
          'FIPS': this.statetree_items[i].STFIPS+this.statetree_items[i].CNTYFIPS,
          'SOURCEINDX': this.statetree_items[i].SOURCEINDX 
        }));
      }

    // <............................................. Creates state_clr_structure ............................................>
      /* state_clr_structure is an array including state and county data with a structure that is needed for creating state tree based on Clarity format. */
      var state_current = null;
      var state_next = null;
      for (var i = 0; i < county.length; i++) {
        state_current = county[i].STNAME;
        if (i === 0) {
          var j = 0;
          this.state_clr_structure.push(new Object({ 
            'STNAME': county[i].STNAME,
            'STFIPS': county[i].FIPS.substring(0,2),
            'index': i,
            'selected': ClrSelectedState.UNSELECTED, 
            'counties': [
              {
                'county': county[i].CYNAME,
                'FIPS': county[i].FIPS,
                'selected': ClrSelectedState.UNSELECTED
              }
            ]
          }));
        }
        if (i+1 !== county.length) {
          state_next = county[i+1].STNAME;
          if (state_current === state_next) {
            this.state_clr_structure[j].counties.push(
              {
              'county': county[i+1].CYNAME,
              'FIPS': county[i+1].FIPS,
              'selected': ClrSelectedState.UNSELECTED
              }
            );
          } else {
            j++;
            this.state_clr_structure.push({ 
              'STNAME': county[i+1].STNAME,
              'STFIPS': county[i+1].FIPS.substring(0,2),
              'index': j,
              'selected': ClrSelectedState.UNSELECTED, 
              'counties': [ 
                {
                  'county': county[i+1].CYNAME,
                  'FIPS': county[i+1].FIPS,
                  'selected': ClrSelectedState.UNSELECTED
                }
              ]
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
            'text': state[j].STNAME, 'STFIPS': state[j].STFIPS, 'SOURCEINDX': -1, 'uniqueid': state[j].STFIPS, 'items': [{}]
          });
        }
      }

      var count = 0;
      var count2 = 0;
      for (var k = 0; k < county.length; k++) {
        if (namecheck.indexOf(county[k].STNAME) == (count2 + 1)) {
          count = 0;
          count2++;
        }
        uniqueNames[namecheck.indexOf(county[k].STNAME)]['items'][count] = { 'text': county[k].CYNAME, 'STFIPS': state[k].STFIPS, 'CNTYFIPS': county[k].CNTYFIPS, 'SOURCEINDX': county[k].SOURCEINDX, 'uniqueid': state[k].STFIPS+county[k].CNTYFIPS };
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
    for (var i = 0; i < this.state_clr_structure.length; i++ ) {
      if (this.state_clr_structure[i].selected === 1) {
        this.statetree_items_selected.push(this.state_clr_structure[i].STFIPS);
        if (this.state_clr_structure[i].STFIPS != 11) {
          this.stateCountyBadgesList.push(this.state_clr_structure[i].STNAME + " - All Counties");
        } else {
          this.stateCountyBadgesList.push("District of Columbia - DC");
        }
      } else {
        if (this.state_clr_structure[i].selected === 2) {
          for (var j = 0; j < this.state_clr_structure[i].counties.length; j++) {
            if (this.state_clr_structure[i].counties[j].selected === 1) {
              this.statetree_items_selected.push(this.state_clr_structure[i].counties[j].FIPS);
              this.stateCountyBadgesList.push(this.state_clr_structure[i].counties[j].county + ", " + this.state_clr_structure[i].STNAME);
            }
          }
        }
      }
    }
    if (this.statetree_items_selected.length == 0) {
      this.last_returned_emissions = {baseline: [], control: []};
      for (var i = 0; i < 5; i++) {
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
    var add_to_scenario_btn = document.getElementById("add_to_scenario_btn");
    for (var i = 0; i < this.state_clr_structure.length; i++) {
      this.state_clr_structure[i].selected = ClrSelectedState.UNSELECTED;
      for (var j = 0; j < this.state_clr_structure[i].counties.length; j++) {
        this.state_clr_structure[i].counties[j].selected = ClrSelectedState.UNSELECTED;
      }
    }
    this.statetree_items_selected = null;
    this.last_returned_emissions = {baseline: [], control: []};
    for (var i = 0; i < 5; i++) {
      this.showErrorLargerThanBaseline[i] = false;
      this.showErrorCantReduceBaseline[i] = false;
    }
    add_to_scenario_btn.setAttribute("disabled", "");
    this.setBaselineAndControl();
    this.locationPresent = false;
  }
  // <------------------------------------------ Clears state and county selections/End --------------------------------------->

  // <------------------------------------------------ Selects all states entirely -------------------------------------------->
  public selectAllStates() {
    for (var i = 0; i < this.state_clr_structure.length; i++) {
      this.state_clr_structure[i].selected = ClrSelectedState.SELECTED;
      for (var j = 0; j < this.state_clr_structure[i].counties.length; j++) {
        this.state_clr_structure[i].counties[j].selected = ClrSelectedState.SELECTED;
      }
    }
    this.updateStatetreeItemsSelected();
  }
  // <------------------------------------------------ Selects all states entirely -------------------------------------------->

  // <--------------------- Updates Tier2 Dropdown and updates tiertree_items_selected once changing Tier1 -------------------->
  /* index_tier1 is the value of the selected option in the dropdown and index1 refers to the index of that Tier1 option in the related array in tiertree_treeview */
  /* In the case that no option is selected, index1 equals -1 */
  public updateTier2Dropdown(index: any) {
    var index1 = null;
    index1 = index-1;
    this.index_tier1 = index;
    this.tier2_items = this.tiertree_treeview[index1].items;
    var target_dd2 = document.getElementById("tier2");
    target_dd2.removeAttribute("disabled");
    this.tier3_items = [];
    var target_dd3 = document.getElementById("tier3");
    target_dd3.setAttribute("disabled", "");
    this.tiertree_items_selected = [index];
    this.activateDeactivateAddToScenarioButton();
    this.executedatarequest();
    this.tierPresent = true;
    this.tier1_selection_text = this.tiertree_treeview[index1].text;
    this.tier2_selection_text = null;
    this.tier3_selection_text = null;
  }
  // <------------------- Updates Tier2 Dropdown and updates tiertree_items_selected once changing Tier1/End ------------------>

  // <--------------------- Updates Tier3 Dropdown and updates tiertree_items_selected once changing Tier2 -------------------->
  public updateTier3Dropdown(index: any) {
    var index1 = null;
    var index2 = null;
    this.index_tier2 = index;
    if (index !== "all") {
      var indexArr = index.split(',');
      index1 = indexArr[0]-1;
      index2 = indexArr[1]-1;
      this.tier3_items = this.tiertree_treeview[index1].items[index2].items;
      var target_dd = document.getElementById("tier3");
      target_dd.removeAttribute("disabled");
      this.tiertree_items_selected = [index];
      this.tier2_selection_text = this.tiertree_treeview[index1].items[index2].text;
    } else {
      this.tier3_items = [];
      var target_dd3 = document.getElementById("tier3");
      target_dd3.setAttribute("disabled", "");
      this.tiertree_items_selected = [this.index_tier1];
      this.tier2_selection_text = null;
    }
    this.executedatarequest();
    this.tier3_selection_text = null;
  }
  // <------------------- Updates Tier3 Dropdown and updates tiertree_items_selected once changing Tier2/End ------------------>

  // <---------------------------------- Updates tiertree_items_selected once changing Tier3 ---------------------------------->
  public updateItemsSelectedTier3(index: any) {
    var index3 = null;
    if (index !== "all") {

      if (index == "5,2,6") {
        index = "5,2,3";
      }

      this.tiertree_items_selected = [index];

      if (index == "5,2,3") {
        this.tiertree_items_selected = ["5,2,6"];
      }

      var indexArr = index.split(',');
      if (indexArr[2] == 99) {
        this.tier3_selection_text = "OTHER";
      } else {
        index3 = indexArr[2]-1;
        this.tier3_selection_text = this.tier3_items[index3].text;
      }

    } else if (this.index_tier2 !== "all") {
      this.tiertree_items_selected = [this.index_tier2];
    } else {
      this.tiertree_items_selected = [this.index_tier1];
    }
    if (index == "all") {
      this.tier3_selection_text = null;
    }
    this.executedatarequest();
  }
  // <-------------------------------- Updates tiertree_items_selected once changing Tier3/End -------------------------------->

  // <-------------------------------------------------- Clears tier selections ----------------------------------------------->
  public clearTierSelections() {
    var add_to_scenario_btn = document.getElementById("add_to_scenario_btn");
    var tier1_dropdown = document.getElementById('tier1') as HTMLSelectElement;
    tier1_dropdown.selectedIndex = 0;
    this.tier2_items = [];
    var target_dd2 = document.getElementById("tier2");
    target_dd2.setAttribute("disabled", "");
    this.tier3_items = [];
    var target_dd3 = document.getElementById("tier3");
    target_dd3.setAttribute("disabled", "");
    this.tiertree_items_selected = null;
    this.last_returned_emissions = {baseline: [], control: []};
    for (var i = 0; i < 5; i++) {
      this.showErrorLargerThanBaseline[i] = false;
      this.showErrorCantReduceBaseline[i] = false;
    }
    add_to_scenario_btn.setAttribute("disabled", "");
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
    var add_to_scenario_btn = document.getElementById("add_to_scenario_btn");
    this.PM25ri = 'reduce';
    this.PM25pt = 'tons';
    this.SO2ri = 'reduce';
    this.SO2pt = 'tons';
    this.NOXri = 'reduce';
    this.NOXpt = 'tons';
    this.NH3ri = 'reduce';
    this.NH3pt = 'tons';
    this.VOCri = 'reduce';
    this.VOCpt = 'tons';
    this.cPM25 = null;
    this.cSO2 = null;
    this.cNOX = null;
    this.cNH3 = null;
    this.cVOC = null;
    if (this.last_returned_emissions["baseline"].length == 0) {
      this.pm25_baseline = 0;
      this.so2_baseline = 0;
      this.nox_baseline = 0;
      this.nh3_baseline = 0;
      this.voc_baseline = 0;
    }
    if (this.last_returned_emissions["control"].length == 0) {
      this.pm25_control = 0;
      this.so2_control = 0;
      this.nox_control = 0;
      this.nh3_control = 0;
      this.voc_control = 0;
    }
    if (this.last_returned_emissions["control"].length != 0) {
      var control = this.last_returned_emissions["control"][0];
      control["PM25"] = this.pm25_baseline;
      control["SO2"] = this.so2_baseline;
      control["NO2"] = this.nox_baseline;
      control["NH3"] = this.nh3_baseline;
      control["VOC"] = this.voc_baseline;
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
    add_to_scenario_btn.setAttribute("disabled", "");
  }
  // <------------------------------------------- Clears emissions subpanel inputs/End ---------------------------------------->

  // <-------------------------------------------- Validates inputs in Emissions panel ---------------------------------------->
  public validateEmissionsPanelInput(i: number) {
    var inputValues = [this.cPM25, this.cSO2, this.cNOX, this.cNH3, this.cVOC];
    var inputValue = inputValues[i];
    var baselines = [this.pm25_baseline_rounded_up, this.so2_baseline_rounded_up, this.nox_baseline_rounded_up, this.nh3_baseline_rounded_up, this.voc_baseline_rounded_up];
    var baselineValue = baselines[i];
    var reduceIncreaseToggles = [this.PM25ri, this.SO2ri, this.NOXri, this.NH3ri, this.VOCri];
    var reduceIncreaseToggle = reduceIncreaseToggles[i];
    var tonsPercentToggles = [this.PM25pt, this.SO2pt, this.NOXpt, this.NH3pt, this.VOCpt];
    var tonsPercentToggle = tonsPercentToggles[i];
    // remove all error messages for current input to validate again
    this.showErrorNotNumber[i] = false;
    this.showErrorOutOfRange[i] = false;
    this.showErrorLargerThanBaseline[i] = false;
    this.showErrorCantReduceBaseline[i] = false;
    // validate the inputs
    var validNumber = /^[+]?([0-9]+(?:[\.][0-9]*)?|\.[0-9]+)$/.test(inputValue) || inputValue == undefined || inputValue == "";
    if (!validNumber) {
      this.showErrorNotNumber[i] = true;
    } else {
      if (reduceIncreaseToggle == "reduce" && tonsPercentToggle == "percent") {
        if ((inputValue < 0 || inputValue > 100) && baselineValue != 0) {
          this.showErrorOutOfRange[i] = true;
        }
      }
      if (reduceIncreaseToggle == "reduce" && tonsPercentToggle == "tons" && baselineValue != undefined && this.last_returned_emissions["baseline"].length != 0 && this.statetree_items_selected.length != 0) {
        if (inputValue > baselineValue) {
          this.showErrorLargerThanBaseline[i] = true;
        }
      }
      if (reduceIncreaseToggle == "reduce" && tonsPercentToggle == "percent" && baselineValue != undefined && this.last_returned_emissions["baseline"].length != 0 && this.statetree_items_selected.length != 0) {
        if (baselineValue == 0 && inputValue > 0) {
          this.showErrorCantReduceBaseline[i] = true;
        }
      }
    }
    this.activateDeactivateAddToScenarioButton();
  }
  // <------------------------------------------ Validates inputs in Emissions panel/End -------------------------------------->

  // <------------------------------------- Activates and deactivates Add to Scenario button ---------------------------------->
  public activateDeactivateAddToScenarioButton() {
    var inputValues = [this.cPM25, this.cSO2, this.cNOX, this.cNH3, this.cVOC];
    var add_to_scenario_btn = document.getElementById("add_to_scenario_btn");
    if (this.statetree_items_selected == null || this.tiertree_items_selected == null) {
      add_to_scenario_btn.setAttribute("disabled", "");
    } else if (this.statetree_items_selected.length == 0 || this.tiertree_items_selected.length == 0) {
        add_to_scenario_btn.setAttribute("disabled", "");
    } else if ((inputValues[0] == undefined || inputValues[0] == "") && (inputValues[1] == undefined || inputValues[1] == "") && (inputValues[2] == undefined || inputValues[2] == "") && (inputValues[3] == undefined || inputValues[3] == "") && (inputValues[4] == undefined || inputValues[4] == "")) {
        add_to_scenario_btn.setAttribute("disabled", "");
    } else if(this.showErrorNotNumber.indexOf(true) != -1 || this.showErrorOutOfRange.indexOf(true) != -1 || this.showErrorLargerThanBaseline.indexOf(true) != -1 || this.showErrorCantReduceBaseline.indexOf(true) != -1) {
        add_to_scenario_btn.setAttribute("disabled", "");
    } else {
      add_to_scenario_btn.removeAttribute("disabled");
    }
  }
  // <----------------------------------- Activates and deactivates Add to Scenario button/End -------------------------------->

  // <----------------------------------- Checks if there is any conflicts between components --------------------------------->
  /* This function checks if the new component that we are trying to create is in conflict with any other component that is already added to scenario. If any conflict is detected; 'foundConflict' is set to true, an alert appears on the page and the new component is not added to scenario. */
  public checkForPossibleConflicts(newComponentData: any) {
    this.foundConflict = false;
 
    /* Create an array listing FIPS codes of all selected counties in the new component. */
    var presentCounties = [];
    for (var i = 0; i < this.state_clr_structure.length; i++) {
      if (this.state_clr_structure[i].selected == 1 || this.state_clr_structure[i].selected == 2) {
        for (var j = 0; j < this.state_clr_structure[i].counties.length; j++) {
          if (this.state_clr_structure[i].counties[j].selected === 1) {
            presentCounties.push(this.state_clr_structure[i].counties[j].FIPS);
          }
        }
      }
    }

    /* Create an array listing all pollutants that have an input value in the new component. */
    var presentPollutants = [];
    if (newComponentData["cPM25"]) presentPollutants.push("PM25");
    if (newComponentData["cSO2"]) presentPollutants.push("SO2");
    if (newComponentData["cNOX"]) presentPollutants.push("NOX");
    if (newComponentData["cNH3"]) presentPollutants.push("NH3");
    if (newComponentData["cVOC"]) presentPollutants.push("VOC");

    /* Create an array listing all the combinations that exist in the new component based on the selections made in this component. */
    var presentTiersCode = newComponentData["tiertree_items_selected"];
    var currentCombination = "";
    var newComponentExistingCombinations = [];
    for (var i = 0; i < presentCounties.length; i++) {
      for (var j = 0; j < presentPollutants.length; j++) {
        currentCombination = presentCounties[i] + '-' + presentPollutants[j] + '-' + presentTiersCode;
        newComponentExistingCombinations.push(currentCombination);
      }
    }

    /* Create an array listing all possible combinations that can be considered as parent for the combinations that exist in the new component. Parent is referring to tier selections that implicitly include 'presentTiersCode'.) */
    var possibleParentCombinationsForNewComponent = [];
    var tierCodeSplits = presentTiersCode[0].split(",");
    if (tierCodeSplits.length == 2) {
      for (var i = 0; i < presentCounties.length; i++) {
        for (var j = 0; j < presentPollutants.length; j++) {
          currentCombination = presentCounties[i] + '-' + presentPollutants[j] + '-' + tierCodeSplits[0];
          possibleParentCombinationsForNewComponent.push(currentCombination);
        }
      }
    }
    if (tierCodeSplits.length == 3) {
      for (var i = 0; i < presentCounties.length; i++) {
        for (var j = 0; j < presentPollutants.length; j++) {
          currentCombination = presentCounties[i] + '-' + presentPollutants[j] + '-' + tierCodeSplits[0];
          possibleParentCombinationsForNewComponent.push(currentCombination);
        }
      }
      for (var i = 0; i < presentCounties.length; i++) {
        for (var j = 0; j < presentPollutants.length; j++) {
          currentCombination = presentCounties[i] + '-' + presentPollutants[j] + '-' + tierCodeSplits[0] + ',' + tierCodeSplits[1];
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
      var componentFromListExistingCombinations = this.reviewPanelComponents[i]["listOfExistingCombinations"];
      for (var j = 0; j < newComponentExistingCombinations.length; j++) {
        // Check for explicit conflicts(type1).
        if (componentFromListExistingCombinations.indexOf(newComponentExistingCombinations[j]) > -1) {
          this.foundConflict = true;
          alert("The emission changes for this sector and location(s) conflict with emissions changes you have already entered. Please enter emission changes for a different sector or location.");
          return
        }
      }
      for (var j = 0; j < possibleParentCombinationsForNewComponent.length; j++) {
        // Check if any of the possible parent combinations of the new component already exists in review panel(type2).
        if (componentFromListExistingCombinations.indexOf(possibleParentCombinationsForNewComponent[j]) > -1) {
          this.foundConflict = true;
          alert("The emission changes for this sector and location(s) conflict with emissions changes you have already entered. Please enter emission changes for a different sector or location.");
          return
        }
      }
    }

    // For every component in the review panel, check if any of its possible parent combinations is created in the new component. It would mean to check if any combination in the review panel can be considered as a child for what is created in the new component(type3).
    for (var i = 0; i < this.reviewPanelComponents.length; i++) {
      var componentFromListPossibleParentCombinations = this.reviewPanelComponents[i]["listOfPossibleParentCombinations"];
      for (var j = 0; j < componentFromListPossibleParentCombinations.length; j++) {
        if (newComponentExistingCombinations.indexOf(componentFromListPossibleParentCombinations[j]) > -1) {
          this.foundConflict = true;
          alert("The emission changes for this sector and location(s) conflict with emissions changes you have already entered. Please enter emission changes for a different sector or location.");
          return
        }
      }
    }

    if (!this.foundConflict) {
      this.component_data_for_reviewpanel["listOfExistingCombinations"] = newComponentExistingCombinations;
      this.component_data_for_reviewpanel["listOfPossibleParentCombinations"] = possibleParentCombinationsForNewComponent;
    }
  }
  // <--------------------------------- Checks if there is any conflicts between components/End ------------------------------->

  // <----------------------------------- Removes component from reviewPanelComponents array ---------------------------------->
  /* This function is called when a component is removed from review panel to also remove that component from reviewPanelComponents array which is defiened in emissions panel and is needed when looking for conflicts. */
  removeComponentFromReviewPanelComponentsArray(index: number) {
    this.reviewPanelComponents.splice(index, 1);
  }
  // <----------------------------------- Removes component from reviewPanelComponents array ---------------------------------->
}
