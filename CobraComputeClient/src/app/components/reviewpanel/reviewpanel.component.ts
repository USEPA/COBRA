import {
  Component,
  ViewEncapsulation,
  OnInit,
  Output,
  EventEmitter,
  Renderer2,
} from '@angular/core';
import { CobraDataService } from '../../cobra-data-service.service';
import { GlobalsService } from 'src/app/globals.service';
import * as selection from 'd3-selection';

@Component({
  selector: 'app-reviewpanel',
  templateUrl: './reviewpanel.component.html',
  styleUrls: ['./reviewpanel.component.scss'],
  encapsulation: ViewEncapsulation.None,
})
export class ReviewpanelComponent implements OnInit {
  @Output() reviewpanelToEmissionspanelRemovedComponentEmitter =
    new EventEmitter<any>();
  @Output() reviewpanelToEmissionspanelClearPanelEmitter =
    new EventEmitter<any>();
  @Output() reviewpanelToResultspanelPendingScreenEmitter =
    new EventEmitter<any>();
  @Output() reviewpanelToResultspanelHeartbeatEmitter = new EventEmitter<any>();
  @Output() reviewpanelToResultspanelRemovedAllComponentsEmitter =
    new EventEmitter<any>();
  @Output() reviewpanelToResultspanelGetResultsEmitter =
    new EventEmitter<any>();

  constructor(
    private cobraDataService: CobraDataService,
    private global: GlobalsService,
    private renderer: Renderer2
  ) {}

  /* variables related to components array */
  public components = [];

  /* variables related to table */
  public tableRowsDefaultLimit = 5;
  public tableRowsToShow = this.tableRowsDefaultLimit;
  public showAllTableRows = false;

  /* variables used to show and hide different review screens */
  public showNoReviewScreen = true;
  public showReviewScreen = false;

  /* variables used to show and hide different review help messages */
  public runScenarioAlreadyClicked = false;
  public showReviewHelp = true;
  public showEditHelp3 = false;
  public showEditHelp4 = false;

  /* discount rate variables */
  public discountRate = '2';
  public disCusValue: any = '';
  public showErrorNotValid: boolean = false;
  public dataForResultsPanel: any = {};

  /* other variables */
  public mode = this.global.getMode();
  public dataIsAddedToForm = true;

  ngOnInit(): void {}

  // <----------------------------------- Calls reviewpanelToResultspanelPendingScreenEmitter --------------------------------->
  emitFromReviewPanelToResultspanelPendingScreen() {
    this.reviewpanelToResultspanelPendingScreenEmitter.emit(null);
  }
  // <--------------------------------- Calls reviewpanelToResultspanelPendingScreenEmitter/End ------------------------------->

  // <--------------------------------------- Adds the new component to components array -------------------------------------->
  public addNewComponent(data: any) {
    console.log("ADD NEW COMPONENT DATA:", data);
    var component = null;
    component = {
      index: this.components.length,
      stateCountyBadgesList: data['stateCountyBadgesList'],
      ShowUpToFiveBadges: true,
      tierSelections: [data['tier1Text'], data['tier2Text'], data['tier3Text']],
      pollutantsList: [
        {
          name: 'PM2.5',
          name_sub: 'PM<sub>2.5</sub>',
          reduce_increase: data['PM25ri'],
          value: data['cPM25'],
          value_formatted: new Intl.NumberFormat('en-US', {
            maximumFractionDigits: 2,
          }).format(data['cPM25']),
          percent_tons: data['PM25pt'],
        },
        {
          name: 'SO2',
          name_sub: 'SO<sub>2</sub>',
          reduce_increase: data['SO2ri'],
          value: data['cSO2'],
          value_formatted: new Intl.NumberFormat('en-US', {
            maximumFractionDigits: 2,
          }).format(data['cSO2']),
          percent_tons: data['SO2pt'],
        },
        {
          name: 'NOx',
          name_sub: 'NO<sub>x</sub>',
          reduce_increase: data['NOXri'],
          value: data['cNOX'],
          value_formatted: new Intl.NumberFormat('en-US', {
            maximumFractionDigits: 2,
          }).format(data['cNOX']),
          percent_tons: data['NOXpt'],
        },
        {
          name: 'NH3',
          name_sub: 'NH<sub>3</sub>',
          reduce_increase: data['NH3ri'],
          value: data['cNH3'],
          value_formatted: new Intl.NumberFormat('en-US', {
            maximumFractionDigits: 2,
          }).format(data['cNH3']),
          percent_tons: data['NH3pt'],
        },
        {
          name: 'VOC',
          name_sub: 'VOC',
          reduce_increase: data['VOCri'],
          value: data['cVOC'],
          value_formatted: new Intl.NumberFormat('en-US', {
            maximumFractionDigits: 2,
          }).format(data['cVOC']),
          percent_tons: data['VOCpt'],
        },
      ],
      updatePacket: data['updatePacket'],
    };
    if (this.components.length == 0) {
      this.showNoReviewScreen = false;
      this.showReviewScreen = true;
      this.emitFromReviewPanelToResultspanelPendingScreen();
    }
    if (this.components.length != 0) {
      this.activateDeactivateRunScenarioButton();
    }
    this.components.unshift(component);
    // ......... Update index value for all components in components array
    // after adding the new component to the beginning of this array .........
    for (var i = 0; i < this.components.length; i++) {
      this.components[i].index = i;
    }

    // ......... Update tableRowsToShow for the cases that table has
    // more rows than tableRowsDefaultLimit and all rows are shown .........
    if (this.tableRowsToShow > this.tableRowsDefaultLimit) {
      this.tableRowsToShow = this.components.length;
    }

    document.getElementById('panel_circle_id').setAttribute('hidden', 'true');
    selection.selectAll('.panel-active').classed('panel-active', false);
    selection.select('#step2').classed('panel-active', true);
    if (this.components.length == 1 || !this.dataIsAddedToForm) {
      document
        .querySelector('.panel-active')
        .scrollIntoView({ behavior: 'smooth' });
    } else if (this.components.length > 1) {
      document
        .querySelector('#review-table')
        .scrollIntoView({ behavior: 'smooth' });
    }

    // highlight background of first row for a few seconds
    setTimeout(function () {
      var newRow = document.getElementsByClassName('review-table__row')[0];
      var newRowDataCells = newRow.children;
      for (var i = 0; i <= 3; i++) {
        newRowDataCells[i].setAttribute('style', 'animation: highlightBg 5s;');
      }
    }, 700);
  }
  // <------------------------------------- Adds the new component to components array/End ------------------------------------>

  // <----------------- Toggles show more or fewer for components that have more than five state-county badges ---------------->
  toggleShowMoreOrFewerInOneRow(index: any) {
    this.components[index].ShowUpToFiveBadges =
      !this.components[index].ShowUpToFiveBadges;
  }
  // <----------------- Toggles show more or fewer for components that have more than five state-county badges ---------------->

  // <------------------------------------ Toggles show more or fewer rows in the table rows ---------------------------------->
  toggleShowMoreOrFewerRowsInTable() {
    this.showAllTableRows = !this.showAllTableRows;
    if (this.showAllTableRows) {
      this.tableRowsToShow = this.components.length;
    }
    if (!this.showAllTableRows) {
      this.tableRowsToShow = this.tableRowsDefaultLimit;
    }
  }
  // <---------------------------------- Toggles show more or fewer rows in the table rows/END -------------------------------->

  // <---------------------------------------------- Updates data for results panel ------------------------------------------->
  updateDataForResultsPanel() {
    if (this.discountRate != 'custom') {
      this.dataForResultsPanel['discountRate'] = parseFloat(this.discountRate);
    } else {
      this.dataForResultsPanel['discountRate'] = parseFloat(this.disCusValue);
    }
  }
  // <-------------------------------------------- Updates data for results panel/End ----------------------------------------->

  // <----------------------------------- Calls reviewpanelToEmissionspanelClearPanelEmitter ---------------------------------->
  emitFromReviewPanelToEmissionspanelClearPanel() {
    this.reviewpanelToEmissionspanelClearPanelEmitter.emit(null);
  }
  // <--------------------------------- Calls reviewpanelToEmissionspanelClearPanelEmitter/End -------------------------------->

  // <------------------------------------- Calls reviewpanelToResultspanelHeartbeatEmitter ----------------------------------->
  emitFromReviewPanelToResultspanelHeartbeat() {
    this.reviewpanelToResultspanelHeartbeatEmitter.emit(null);
  }
  // <----------------------------------- Calls reviewpanelToResultspanelHeartbeatEmitter/End --------------------------------->

  // <------------------------------------- Calls reviewpanelToResultspanelGetResultsEmitter ---------------------------------->
  emitFromReviewPanelToResultspanel(data: any) {
    this.reviewpanelToResultspanelGetResultsEmitter.emit(data);
  }
  // <----------------------------------- Calls reviewpanelToResultspanelGetResultsEmitter/End -------------------------------->

  // <------------------------------------------------------ Updates Mode ----------------------------------------------------->
  updateDataSource(dataIsAddedToForm: boolean) {
    this.dataIsAddedToForm = dataIsAddedToForm;
  }
  // <---------------------------------------------------- Updates Mode/END --------------------------------------------------->

  // <---------------------------------------------------- Updates database --------------------------------------------------->
  /* This function is called to make an update for every single component separately. After making every update, it checks the index of the component that did the update for. If this is the last component in components array, the emit function will be called in order to call getResults() in resultspanel and show the results in the table. */
  updateDataBase(
    updatePacket: any,
    componentIndex: number,
    componentsArrayLength: number
  ) {
    this.cobraDataService.updateEmissionsData(updatePacket).subscribe(
      (data) => {},
      (err) => console.error('An error occured during update: ' + err),
      () => {
        if (componentIndex + 1 == componentsArrayLength) {
          this.updateDataForResultsPanel();
          this.emitFromReviewPanelToResultspanel(this.dataForResultsPanel);
        }
      }
    );
  }

  /* This function is called to make a batch update. After making updates, the emit function will be called in order to call getResults() in resultspanel and show the results in the table. */
  batchupdateDataBase(packets: any) {
    this.cobraDataService
      .batchupdateEmissionsData(packets, this.mode)
      .subscribe(
        (data) => {},
        (err) => console.error('An error occured during update: ' + err),
        () => {
          this.updateDataForResultsPanel();
          this.emitFromReviewPanelToResultspanel(this.dataForResultsPanel);
        }
      );
  }

  // <-------------------------------------------------- Updates database/End ------------------------------------------------->

  // <------------------------------------------ Runs scenario for created components ----------------------------------------->
  /* This function loops through components array and updates the database for every single component. */
  runScenario() {
    if (this.runScenarioAlreadyClicked == false) {
      this.runScenarioAlreadyClicked = true;
    }
    // showing results panel and disabling run secenario button
    var step3_panel = document.getElementById('step3');
    var run_scenario_btn = document.getElementById('run_scenario_btn');
    step3_panel.style.visibility = 'visible';
    run_scenario_btn.setAttribute('disabled', '');
    // showing the right help message
    this.showReviewHelp = false;
    this.showEditHelp3 = false;
    this.showEditHelp4 = false;
    // updating the status of green borders
    document.getElementById('panel_circle_id').setAttribute('hidden', 'true');
    selection.selectAll('.panel-active').classed('panel-active', false);
    selection.select('#step3').classed('panel-active', true);
    document
      .querySelector('.panel-active')
      .scrollIntoView({ behavior: 'smooth' });

    this.emitFromReviewPanelToEmissionspanelClearPanel();
    this.emitFromReviewPanelToResultspanelHeartbeat();

    var updatePacket = {};
    // resetting database and then updating database with new inputs
    this.cobraDataService.resetEmissionsData().subscribe(
      (data) => {
        var packets = [];
        for (var i = 0; i < this.components.length; i++) {
          var component = this.components[i];
          updatePacket = component.updatePacket;
          packets.push(updatePacket);
        }
        this.batchupdateDataBase(packets); //check for side effects > triggers screen update
      },
      (err) => console.error('An error occured during update: ' + err),
      () => {}
    );
    var info_text_table = document.getElementById('info_text_table');
    info_text_table.removeAttribute('hidden');
  }
  // <---------------------------------------- Runs scenario for created components/End --------------------------------------->

  // <--------------------------------- Calls reviewpanelToEmissionspanelRemovedComponentEmitter ------------------------------>
  emitFromReviewPanelToEmissionspanel(data: any) {
    this.reviewpanelToEmissionspanelRemovedComponentEmitter.emit(data);
  }
  // <------------------------------- Calls reviewpanelToEmissionspanelRemovedComponentEmitter/End ---------------------------->

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
    if (this.components.length != 0) {
      this.activateDeactivateRunScenarioButton();
    }
    if (this.components.length == 0) {
      this.showNoReviewScreen = true;
      this.showReviewScreen = false;
      this.runScenarioAlreadyClicked = false;
      this.showReviewHelp = true;
      this.showEditHelp3 = false;
      this.showEditHelp4 = false;
      this.discountRate = '2';
      this.disCusValue = '';
      this.showErrorNotValid = false;
      this.emitFromReviewPanelToResultspanelRemovedAllComponents();

      document.getElementById('panel_circle_id').removeAttribute('hidden');
      selection.selectAll('.panel-active').classed('panel-active', false);
      selection.select('#step1').classed('panel-active', true);
      document
        .querySelector('.panel-active')
        .scrollIntoView({ behavior: 'smooth' });
    }
    if (this.components.length <= this.tableRowsDefaultLimit) {
      this.showAllTableRows = false;
      this.tableRowsToShow = this.tableRowsDefaultLimit;
    }
  }
  // <----------------------------------------- Removes component from review panel/End --------------------------------------->

  // <-------------------------------- Sets discountRate to custom when clicking on custom input ------------------------------>
  setDiscountRateToCustom() {
    if (this.discountRate != 'custom') {
      this.discountRate = 'custom';
      this.activateDeactivateRunScenarioButton();
    }
  }
  // <------------------------------ Sets discountRate to custom when clicking on custom input/End ---------------------------->

  // <-------------------------------------- Clears disCusValue when clicking on 3% or 7% ------------------------------------->
  clearCustomValue() {
    this.disCusValue = '';
    this.showErrorNotValid = false;
  }
  // <------------------------------------ Clears disCusValue when clicking on 3% or 7%/End ----------------------------------->

  // <------------------------------------------- Validates discount rate custom input ---------------------------------------->
  validateDiscountRateInput() {
    var inputValue = this.disCusValue;
    if (this.discountRate == 'custom') {
      var validNumber =
        /^[+]?([0-9]+(?:[\.][0-9]*)?|\.[0-9]+)$/.test(inputValue) ||
        inputValue == undefined ||
        inputValue == '';
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
    var run_scenario_btn = document.getElementById('run_scenario_btn');
    if (this.discountRate != 'custom') {
      if (this.runScenarioAlreadyClicked == false) {
        this.showReviewHelp = true;
        this.showEditHelp3 = false;
        this.showEditHelp4 = false;
      }
      if (this.runScenarioAlreadyClicked == true) {
        this.showReviewHelp = false;
        this.showEditHelp3 = false;
        this.showEditHelp4 = true;
      }
      if (run_scenario_btn) {
        run_scenario_btn.removeAttribute('disabled');
      }
    } else if (
      this.discountRate == 'custom' &&
      (this.disCusValue == undefined ||
        this.disCusValue == '' ||
        this.showErrorNotValid)
    ) {
      if (this.runScenarioAlreadyClicked == false) {
        this.showReviewHelp = true;
        this.showEditHelp3 = false;
        this.showEditHelp4 = false;
      }
      if (this.runScenarioAlreadyClicked == true) {
        this.showReviewHelp = false;
        this.showEditHelp3 = true;
        this.showEditHelp4 = false;
      }
      run_scenario_btn.setAttribute('disabled', '');
    } else {
      if (this.runScenarioAlreadyClicked == false) {
        this.showReviewHelp = true;
        this.showEditHelp3 = false;
        this.showEditHelp4 = false;
      }
      if (this.runScenarioAlreadyClicked == true) {
        this.showReviewHelp = false;
        this.showEditHelp3 = false;
        this.showEditHelp4 = true;
      }
      run_scenario_btn.removeAttribute('disabled');
    }
  }
  // <--------------------------------------- Activates and deactivates Run Scenario button ----------------------------------->

  // <------------------------------- Resets review panel when Build New Scenario button is clicked --------------------------->
  resetReviewPanel() {
    this.showNoReviewScreen = true;
    this.showReviewScreen = false;
    this.runScenarioAlreadyClicked = false;
    this.showReviewHelp = true;
    this.showEditHelp3 = false;
    this.showEditHelp4 = false;
    this.discountRate = '2';
    this.disCusValue = '';
    this.showErrorNotValid = false;
    this.dataForResultsPanel = {};
    this.components = [];

    document.getElementById('panel_circle_id').removeAttribute('hidden');
    selection.selectAll('.panel-active').classed('panel-active', false);
    selection.select('#step1').classed('panel-active', true);
    document
      .querySelector('.panel-active')
      .scrollIntoView({ behavior: 'smooth' });
  }
  // <----------------------------- Resets review panel when Build New Scenario button is clicked/End ------------------------->

  // <----------------------------- Resets review panel when Build New Scenario button is clicked/End ------------------------->
  showEditHelpThree() {
    if (this.showEditHelp4 == false) {
      this.showEditHelp3 = true;
    }
  }
  // <----------------------------- Resets review panel when Build New Scenario button is clicked/End ------------------------->
}
