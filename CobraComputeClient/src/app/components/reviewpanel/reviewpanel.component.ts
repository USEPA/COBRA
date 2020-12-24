import { Component, ViewEncapsulation, OnInit, Output, EventEmitter } from '@angular/core';
import { CobraDataService } from '../../cobra-data-service.service';
@Component({
  selector: 'app-reviewpanel',
  templateUrl: './reviewpanel.component.html',
  styleUrls: ['../../../../node_modules/@clr/ui/clr-ui.css', '../../../theme/styles.scss','./reviewpanel.component.scss'],
  encapsulation: ViewEncapsulation.None
})

export class ReviewpanelComponent implements OnInit {
  @Output() reviewpanelToEmissionspanelEmitter = new EventEmitter<any>();
  @Output() reviewpanelToResultspanelPendingScreenEmitter = new EventEmitter<any>();
  @Output() reviewpanelToResultspanelEmitterHeartbeat = new EventEmitter<any>();
  @Output() reviewpanelToResultspanelRemovedAllComponentsEmitter = new EventEmitter<any>();
  @Output() reviewpanelToResultspanelEmitter = new EventEmitter<any>();

  constructor(private cobraDataService: CobraDataService) { }

  /* variables related to components array */
  public components = [];

  /* variables used to show and hide different review screens */
  public showNoReviewScreen = true;
  public showReviewScreen = false;

  /* discount rate variables */
  public discountRate = "3";
  public disCusValue: any = "";
  public showErrorNotValid: boolean = false;
  public dataForResultsPanel: any = {};

  ngOnInit(): void {
  }
  
  // <----------------------------------- Calls reviewpanelToResultspanelPendingScreenEmitter --------------------------------->
  emitFromReviewPanelToResultspanelPendingScreen() {
    this.reviewpanelToResultspanelPendingScreenEmitter.emit(null);
  }
  // <--------------------------------- Calls reviewpanelToResultspanelPendingScreenEmitter/End ------------------------------->

  // <--------------------------------------- Adds the new component to components array -------------------------------------->
  public addNewComponent(data: any) {
    var component = null;
    component = {
      index: this.components.length,
      stateCountyBadgesList: data["stateCountyBadgesList"],
      tierSelections: [
                        data["tier1Text"],
                        data["tier2Text"],
                        data["tier3Text"]
                      ],
      pollutantsList: [
                        {
                          name: "PM2.5",
                          name_sub: "PM\u2082.\u2085",
                          reduce_increase: data["PM25ri"],
                          value: data["cPM25"],
                          value_formatted: new Intl.NumberFormat('en-US', { maximumFractionDigits: 2}).format(data["cPM25"]),
                          percent_tons: data["PM25pt"]
                        },
                        {
                          name: "SO2",
                          name_sub: "SO\u2082",
                          reduce_increase: data["SO2ri"],
                          value: data["cSO2"],
                          value_formatted: new Intl.NumberFormat('en-US', { maximumFractionDigits: 2}).format(data["cSO2"]),
                          percent_tons: data["SO2pt"]
                        },
                        {
                          name: "NOX",
                          name_sub: "NO\u2093",
                          reduce_increase: data["NOXri"],
                          value: data["cNOX"],
                          value_formatted: new Intl.NumberFormat('en-US', { maximumFractionDigits: 2}).format(data["cNOX"]),
                          percent_tons: data["NOXpt"]
                        },
                        {
                          name: "NH3",
                          name_sub: "NH\u2083",
                          reduce_increase: data["NH3ri"],
                          value: data["cNH3"],
                          value_formatted: new Intl.NumberFormat('en-US', { maximumFractionDigits: 2}).format(data["cNH3"]),
                          percent_tons: data["NH3pt"]
                        },
                        {
                          name: "VOC",
                          name_sub: "VOC",
                          reduce_increase: data["VOCri"],
                          value: data["cVOC"],
                          value_formatted: new Intl.NumberFormat('en-US', { maximumFractionDigits: 2}).format(data["cVOC"]),
                          percent_tons: data["VOCpt"]
                        }
                      ],
      updatePacket: data["updatePacket"]
    }
    if (this.components.length == 0) {
      var step2_panel = document.getElementById("step2");
      step2_panel.setAttribute("class", "panel panel-active");
      this.showNoReviewScreen = false;
      this.showReviewScreen = true;
      this.emitFromReviewPanelToResultspanelPendingScreen();
    }
    this.components.push(component);
  }
  // <------------------------------------- Adds the new component to components array/End ------------------------------------>

  // <---------------------------------------------- Updates data for results panel ------------------------------------------->
  updateDataForResultsPanel() {
    if (this.discountRate != "custom") {
      this.dataForResultsPanel["discountRate"] = parseFloat(this.discountRate);
    } else {
      this.dataForResultsPanel["discountRate"] = parseFloat(this.disCusValue);
    }
  }
  // <-------------------------------------------- Updates data for results panel/End ----------------------------------------->

  // <------------------------------------- Calls reviewpanelToResultspanelEmitterHeartbeat ----------------------------------->
  emitFromReviewPanelToResultspanelHeartbeat() {
    this.reviewpanelToResultspanelEmitterHeartbeat.emit(null);
  }
  // <----------------------------------- Calls reviewpanelToResultspanelEmitterHeartbeat/End --------------------------------->

  // <------------------------------------------ Calls reviewpanelToResultspanelEmitter --------------------------------------->
  emitFromReviewPanelToResultspanel(data: any) {
    this.reviewpanelToResultspanelEmitter.emit(data);
  }
  // <---------------------------------------- Calls reviewpanelToResultspanelEmitter/End ------------------------------------->

  // <---------------------------------------------------- Updates database --------------------------------------------------->
  /* This function is called to make an update for every single component separately. After making every update, it checks the index of the component that did the update for. If this is the last component in components array, the emit function will be called in order to call getResults() in resultspanel and show the results in the table. */
  updateDataBase(updatePacket: any, componentIndex: number, componentsArrayLength: number) {
    this.cobraDataService.updateEmissionsData(updatePacket).subscribe(
      data => { 
        console.log('Update transmitted.');
      },
      err => console.error('An error occured during update: '+err),
      () => {
        console.log('Done with update.');
        if (componentIndex+1 == componentsArrayLength) {
          this.updateDataForResultsPanel();
          this.emitFromReviewPanelToResultspanel(this.dataForResultsPanel);
        }
      }
    );
  }
  // <-------------------------------------------------- Updates database/End ------------------------------------------------->

  // <------------------------------------------ Runs scenario for created components ----------------------------------------->
  /* This function loops through components array and updates the database for every single component. */
  runScenario() {
    var step1_panel = document.getElementById("step1");
    var step2_panel = document.getElementById("step2");
    var step3_panel = document.getElementById("step3");
    var panel_circle = document.getElementById("panel_circle_id");
    step1_panel.setAttribute("class", "panel");
    step2_panel.setAttribute("class", "panel");
    step3_panel.setAttribute("class", "panel panel-active");
    panel_circle.setAttribute("hidden", "true");
    this.emitFromReviewPanelToResultspanelHeartbeat();
    var updatePacket = {};
    for (var i = 0; i < this.components.length; i++) {
      var component = this.components[i];
      updatePacket = component.updatePacket;
      this.updateDataBase(updatePacket, component.index, this.components.length);
    }
    var info_text_table = document.getElementById("info_text_table");
    info_text_table.removeAttribute("hidden");
  }
  // <---------------------------------------- Runs scenario for created components/End --------------------------------------->

  // <----------------------------------------- Calls reviewpanelToEmissionspanelEmitter -------------------------------------->
  emitFromReviewPanelToEmissionspanel(data: any) {
    this.reviewpanelToEmissionspanelEmitter.emit(data);
  }
  // <--------------------------------------- Calls reviewpanelToEmissionspanelEmitter/End ------------------------------------>

  // <-------------------------------- Calls reviewpanelToResultspanelRemovedAllComponentsEmitter ----------------------------->
  emitFromReviewPanelToResultspanelRemovedAllComponents() {
    this.reviewpanelToResultspanelRemovedAllComponentsEmitter.emit(null);
  }
  // <------------------------------ Calls reviewpanelToResultspanelRemovedAllComponentsEmitter/End --------------------------->

  // <------------------------------------------- Removes component from review panel ----------------------------------------->
  /* This function is called when the close icon for one component is clicked to remove that component from review panel. Then the index of the removed component is passed to emissionspanel to remove the deleted component from reviewPanelComponents array which is defiened in emissions panel and is needed when looking for conflicts. The for loop updates the index property for every remaining component in components array. */
  removeComponent(index: any) {
    this.components.splice(index, 1);
    this.emitFromReviewPanelToEmissionspanel(index);
    for (var i = 0; i < this.components.length; i++) {
      var component = this.components[i];
      component.index = i;
    }
    if (this.components.length == 0) {
      var step1_panel = document.getElementById("step1");
      var step2_panel = document.getElementById("step2");
      var step3_panel = document.getElementById("step3");
      var panel_circle = document.getElementById("panel_circle_id");
      step1_panel.setAttribute("class", "panel panel-active");
      step2_panel.setAttribute("class", "panel");
      step3_panel.setAttribute("class", "panel");
      panel_circle.removeAttribute("hidden");
      this.showNoReviewScreen = true;
      this.showReviewScreen = false;
      this.emitFromReviewPanelToResultspanelRemovedAllComponents();
    }
  }
  // <----------------------------------------- Removes component from review panel/End --------------------------------------->

  // <------------------------------------------- Validates discount rate custom input ---------------------------------------->
  validateDiscountRateInput() {
    var inputValue = this.disCusValue;
    if (this.discountRate == "custom") {
      var validNumber = /^[+]?([0-9]+(?:[\.][0-9]*)?|\.[0-9]+)$/.test(inputValue) || inputValue == undefined || inputValue == "";
      if (validNumber) {
        this.showErrorNotValid = false;
      } else {
        this.showErrorNotValid = true;
      }
    } else {
      this.showErrorNotValid = false;
    }
    this.activateDeactivateRunScenarioButton();
  }
  // <----------------------------------------- Validates discount rate custom input/End -------------------------------------->

  // <--------------------------------------- Activates and deactivates Run Scenario button ----------------------------------->
  activateDeactivateRunScenarioButton() {
    var run_scenario_btn = document.getElementById("run_scenario_btn");
    if (this.discountRate != "custom") {
      run_scenario_btn.removeAttribute("disabled");
    } else if (this.discountRate == "custom" && this.disCusValue == undefined || this.disCusValue == "" || this.showErrorNotValid) {
      run_scenario_btn.setAttribute("disabled", "");
    } else {
      run_scenario_btn.removeAttribute("disabled");
    }
  }
  // <--------------------------------------- Activates and deactivates Run Scenario button ----------------------------------->
}
