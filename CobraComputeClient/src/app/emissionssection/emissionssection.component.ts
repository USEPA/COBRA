import { Component, ViewChild, ViewEncapsulation, OnInit, Input, Output, EventEmitter } from '@angular/core';
import { Impacts } from '../impacts';
import { Token } from '../Token';
import { CobraDataService } from '../cobra-data-service.service';
import { CheckableSettings, CheckedState} from '@progress/kendo-angular-treeview';
import { of } from 'rxjs/observable/of';
import { TreeItemLookup } from '@progress/kendo-angular-treeview';
import { NgForm } from '@angular/forms';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { BehaviorSubject } from 'rxjs/BehaviorSubject';
import { isNull } from 'util';
import { LayoutModule } from '@progress/kendo-angular-layout';
import { loadQueryList } from '@angular/core/src/render3/instructions';


@Component({
  selector: 'app-emissionssection',
  templateUrl: './emissionssection.component.html',
  styleUrls: ['./emissionssection.component.css'],
  encapsulation: ViewEncapsulation.None
})

export class EmissionssectionComponent implements OnInit {
  @Input() token: Token;
  public selected: number = 1;
  
  constructor(private cobraDataService: CobraDataService, private http: HttpClient) { }
  @ViewChild('tabstrip') public tabstrip: any;

  /*State filter initialization */
  private statetree_items: any[] = null;
  
  /* returned variables */
  public results_for_table: any[] = [];
  public returned_results: any[] = [];
  public pm25_baseline: number;
  public so2_baseline: number;
  public nox_baseline: number;
  public nh3_baseline: number;
  public voc_baseline: number;
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
  public endApiCall: boolean;
  applyChangesReview = '';
  public cPM25: number;
  public cSO2: number;
  public cNOX: number;
  public cNH3: number;
  public cVOC: number;

  @Output() theEmitter = new EventEmitter<any>();

  public table_results: any[] = [];
  public unit_check: any[] = [];
  public delta_results: any[] = [];
  /*state variables*/
  public statetree_checkedKeys: any[] = [];
  public statetree_key = 'uniqueid';
  public statetree_enableCheck = true;
  public statetree_checkChildren = true;
  public statetree_checkParents = false;
  public statetree_checkOnClick = true;
  public statetree_checkMode: any = 'multiple';
  public statetree_selectionMode: any = 'single';
  public statetree_pageSize = 10;
  public statetree_treeview: any[];
  findWithAttr(array, attr, value) {
    for (var i = 0; i < array.length; i += 1) {
      if (array[i][attr] === value) {
        return i;
      }
    }
    return -1;
  }
  private statetree_items_selected: any = null;
  private dataSource = new BehaviorSubject<any>([]);
  currentData = this.dataSource.asObservable();
  changeData(data: any) {
      this.dataSource.next(data);
  }
  public statetree_fetchChildren(node: any) {
    // Return the parent node's items collection as children
    return of(node.items);
  }

  public onTabSelect(e) {
    this.selected = e["index"] + 1;
  }

  updateDataBase(){
    // Update in database
    if (this.returned_results["control"] != []) {
     this.cobraDataService.updateEmissionsData(this.returned_results["control"][0]).subscribe(
       data => { console.log('Update transmitted.');this.executedatarequest(); this.gatherBaselineAndControl();},
        err => console.error('An error occured during update: '+err),
        () => {console.log('Done with update.');this.createReviewTable();}
        );
    }
    
  }

  /* Modifies control result */
    applyChanges(apform: NgForm): void {

        
    var chem_list = ['PM25', 'SO2', 'NOX', 'NH3', 'VOC'];
    var value = 0;
    var conversion = "";
    var adjustment = "";
    for (var i = 0; i < chem_list.length; i++) {
      value = parseFloat(apform.value["change" + chem_list[i]]);
      if (value === undefined || value== null || isNaN(value)) value = 0;
      conversion = apform.value[chem_list[i] + "pt"];
      adjustment = apform.value[chem_list[i] + "ri"];
      this.table_results[chem_list[i] + '_unit'] = conversion;
   
      if (conversion == "percent" && value > 100) {
        alert(chem_list[i] + " needs to be within 0 and 100%.");
        break;
      }
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
    
        //update returned results
    if (this.returned_results["control"].length != 0) {
      this.returned_results["control"][0]["PM25"] = this.pm25_control;
      this.returned_results["control"][0]["SO2"] = this.so2_control;
      this.returned_results["control"][0]["NO2"] = this.nox_control;
      this.returned_results["control"][0]["NH3"] = this.nh3_control;
      this.returned_results["control"][0]["VOC"] = this.voc_control;

      this.returned_results["control"][0]["fipscodes"] = this.statetree_items_selected;
      this.returned_results["control"][0]["tierselection"] = this.tiertree_items_selected;
    }

    this.updateDataBase();

    
  }

  changeTab(number){
    this.tabstrip.selectTab(number);
  }

  validateApply(): void {

  }

  applyVerify(review): void {
    if (review == "location"){
      this.clearLocations();
      this.clearTiers()
      this.tabstrip.selectTab(0);
    }else if (review == "review"){
      this.theEmitter.emit({ action: 'review'});
    }
  }

 /* applyChangesNew(apform: NgForm,type): void {
    this.applyChanges(apform);
    this.applyVerify("new location")
  }

  applyChangesReview(apform: NgForm): void{
    
    this.applyChanges(apform);
     
    this.applyVerify("review")
      
    
  }*/
  applyChangesClick(apform: NgForm,type){
    this.applyChanges(apform);
    this.applyChangesReview = type;
  }
  createReviewTable(){
    if (this.tiertree_items_selected != null && this.statetree_items_selected != null) {
      if (this.tiertree_items_selected.length != 0 && this.statetree_items_selected.length != 0) {
        this.cobraDataService.getSummarizedEmissionsData(this.statetree_items_selected, this.tiertree_items_selected).subscribe(
          data => {
            this.table_results['DATA'] = data;
            this.table_results['DATA']['reset'] = false;
            for (var j = 0; j < this.table_results['DATA']['baseline'].length; j++) {
              
              var county_index = this.findWithAttr(this.statetree_items, 'SOURCEINDX', this.table_results['DATA']['baseline'][j]['sourceindx']);
              var tier_index = this.findWithAttr(this.tiertree_items, 'TIER1', this.table_results['DATA']['baseline'][j]['TIER1']);
              if (county_index == -1) {
                var county_index2 = this.findWithAttr(this.statetree_items, 'STFIPS', this.statetree_items_selected[j]);
                this.table_results['DATA']['baseline'][j]['state'] = this.statetree_items[county_index2]['STNAME'];
                this.table_results['DATA']['control'][j]['state'] = this.statetree_items[county_index2]['STNAME'];
              } else {
                this.table_results['DATA']['baseline'][j]['state'] = this.statetree_items[county_index]['STNAME'];
                this.table_results['DATA']['baseline'][j]['county'] = this.statetree_items[county_index]['CYNAME'];
                this.table_results['DATA']['control'][j]['state'] = this.statetree_items[county_index]['STNAME'];
                this.table_results['DATA']['control'][j]['county'] = this.statetree_items[county_index]['CYNAME'];
              }

              this.table_results['DATA']['baseline'][j]['tier1name'] = this.tiertree_items[tier_index]['TIER1NAME'];
              this.table_results['DATA']['control'][j]['tier1name'] = this.tiertree_items[tier_index]['TIER1NAME'];
              
                };

                if (Object.keys(this.table_results).length > 0) {
            
                    for (var i = 0; i < this.table_results['DATA']['baseline'].length; i++) {
            
            
                        if (this.table_results['PM25_unit'] == "tons") {
                            var PM25 = this.table_results['DATA']['control'][i]['PM25'] - this.table_results['DATA']['baseline'][i]['PM25'];;
                            this.unit_check['PM25_unit'] = "tons";
                        } else {
                            var PM25 = 100 * (this.table_results['DATA']['control'][i]['PM25'] - this.table_results['DATA']['baseline'][i]['PM25']) / this.table_results['DATA']['baseline'][i]['PM25']
                            this.unit_check['PM25_unit'] = "percent";
                        }
            
            
            
                        if (this.table_results['SO2_unit'] == "tons") {
                            var SO2 = this.table_results['DATA']['control'][i]['SO2'] - this.table_results['DATA']['baseline'][i]['SO2'];
                            this.unit_check['SO2_unit'] = "tons";
                        } else {
                            var SO2 = 100 * (this.table_results['DATA']['control'][i]['SO2'] - this.table_results['DATA']['baseline'][i]['SO2']) / this.table_results['DATA']['baseline'][i]['SO2'];
                            this.unit_check['SO2_unit'] = "percent";
                        }
            
            
                        if (this.table_results['NOX_unit'] == "tons") {
                            var NO2 = this.table_results['DATA']['control'][i]['NO2'] - this.table_results['DATA']['baseline'][i]['NO2'];
                            this.unit_check['NOX_unit'] = "tons";
                        } else {
                            var NO2 = 100 * (this.table_results['DATA']['control'][i]['NO2'] - this.table_results['DATA']['baseline'][i]['NO2']) / this.table_results['DATA']['baseline'][i]['NO2'];
                            this.unit_check['NOX_unit'] = "percent";
                        }
            
                        if (this.table_results['NH3_unit'] == "tons") {
                            var NH3 = this.table_results['DATA']['control'][i]['NH3'] - this.table_results['DATA']['baseline'][i]['NH3'];
                            this.unit_check['NH3_unit'] = "tons";
                        } else {
                            var NH3 = 100 * (this.table_results['DATA']['control'][i]['NH3'] - this.table_results['DATA']['baseline'][i]['NH3']) / this.table_results['DATA']['baseline'][i]['NH3'];
                            this.unit_check['NH3_unit'] = "percent";
                        }
            
                        if (this.table_results['VOC_unit'] == "tons") {
                            var VOC = this.table_results['DATA']['control'][i]['VOC'] - this.table_results['DATA']['baseline'][i]['VOC'];
                            this.unit_check['VOC_unit'] = "tons";
                        } else {
                            var VOC = 100 * (this.table_results['DATA']['control'][i]['VOC'] - this.table_results['DATA']['baseline'][i]['VOC']) / this.table_results['DATA']['baseline'][i]['VOC'];
                            this.unit_check['VOC_unit'] = "percent";
                        }
            
                        if (isNaN(PM25)) { PM25 = 0; }
                        if (isNaN(SO2)) { SO2 = 0; }
                        if (isNaN(NO2)) { NO2 = 0; }
                        if (isNaN(NH3)) { NH3 = 0; }
                        if (isNaN(VOC)) { VOC = 0; }
                      
                    if(+(PM25.toFixed(2)) != 0 || +(SO2.toFixed(2)) != 0 || +(NO2.toFixed(2)) != 0 || +(NH3.toFixed(2)) != 0 || +(VOC.toFixed(2)) != 0) {
                      this.delta_results.push({'units':this.unit_check,'state': this.table_results['DATA']['control'][i]['state'], 
                                                'county': this.table_results['DATA']['control'][i]['county'], 
                                                'tier1name': this.table_results['DATA']['control'][i]['tier1name'], 
                                                'deltaPM25': PM25, 
                                                'deltaNO2': NO2, 
                                                'deltaSO2': SO2, 
                                                'deltaNH3': NH3, 
                                                'deltaVOC': VOC,
                                                'basePM25': this.table_results['DATA']['baseline'][i]['PM25'],
                                                'baseNO2': this.table_results['DATA']['baseline'][i]['NO2'],
                                                'baseSO2': this.table_results['DATA']['baseline'][i]['SO2'],
                                                'baseNH3': this.table_results['DATA']['baseline'][i]['NH3'],
                                                'baseVOC': this.table_results['DATA']['baseline'][i]['VOC'],
                                                'controlPM25': this.table_results['DATA']['control'][i]['PM25'],
                                                'controlNO2': this.table_results['DATA']['control'][i]['NO2'],
                                                'controlSO2': this.table_results['DATA']['control'][i]['SO2'],
                                                'controlNH3': this.table_results['DATA']['control'][i]['NH3'],
                                                'controlVOC': this.table_results['DATA']['control'][i]['VOC'],
                                                'reset':this.table_results['DATA']['reset']} )
                      }
                      
            
            
                    }
                    console.log(this.unit_check);
                    this.cobraDataService.changeData(this.delta_results);
                    this.applyVerify(this.applyChangesReview);
                  }
                  
                  
        },
          err => console.error(err),
          () => console.log('Retrieved tier items.')


        );
      }
    }
  }
  

  noRecordsReturnedCheck() {
    if ((this.endApiCall && (this.tiertree_items_selected.length == 0 || this.statetree_items_selected.length == 0)) || (this.endApiCall && (this.returned_results["baseline"].length == 0 && this.returned_results["control"].length == 0))) {
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
      this.returned_results["control"].push({ "PM25": this.pm25_control, "SO2": this.so2_control, "NO2": this.nox_control, "NH3": this.nh3_control, "VOC": this.voc_control, "fipscodes": this.statetree_items_selected, "tierselection": this.tiertree_items_selected });
      this.returned_results["baseline"].push({ "PM25": this.pm25_control, "SO2": this.so2_control, "NO2": this.nox_control, "NH3": this.nh3_control, "VOC": this.voc_control, "fipscodes": this.statetree_items_selected, "tierselection": this.tiertree_items_selected });

    } else {
      this.endApiCall = false;
    }
  }

  gatherBaselineAndControl() {
    if (this.returned_results["baseline"].length == 0) {
      this.pm25_baseline = 0;
      this.so2_baseline = 0;
      this.nox_baseline = 0;
      this.nh3_baseline = 0;
      this.voc_baseline = 0;
    }
    if (this.returned_results["control"].length == 0) {
      this.pm25_control = 0;
      this.so2_control = 0;
      this.nox_control = 0;
      this.nh3_control = 0;
      this.voc_control = 0;
    }
    if (this.returned_results["control"].length != 0) {
      var control = this.returned_results["control"][0];
      this.pm25_control = control["PM25"];
      this.so2_control = control["SO2"];
      this.nox_control = control["NO2"];
      this.nh3_control = control["NH3"];
      this.voc_control = control["VOC"];
    }
    if (this.returned_results["baseline"].length != 0) {
      var baseline = this.returned_results["baseline"][0];
      this.pm25_baseline = baseline["PM25"];
      this.so2_baseline = baseline["SO2"];
      this.nox_baseline = baseline["NO2"];
      this.nh3_baseline = baseline["NH3"];
      this.voc_baseline = baseline["VOC"];
    }
    this.endApiCall = true;
    this.noRecordsReturnedCheck();
    
  }

  clearTiers() {
    this.tiertree_checkedKeys = [];
    this.tiertree_items_selected = [];
    this.endApiCall = true;
    this.noRecordsReturnedCheck();
  }
  clearLocations() {
    this.statetree_checkedKeys = [];
    this.statetree_items_selected = [];
    this.endApiCall = true;
    this.noRecordsReturnedCheck();
  }
  pagereload(){
    if (confirm('Warning: this will reset all emissions entered for this scenario. Do you wish to proceed?')) {
      window.location.reload();
    } else {
      // Do nothing!
    }
  }
  reset() {
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
    this.pm25_control = 0;
    this.so2_control = 0;
    this.nox_control = 0;
    this.nh3_control = 0;
    this.voc_control = 0;
    this.returned_results["control"][0]["PM25"] = this.pm25_baseline;
    this.returned_results["control"][0]["SO2"] = this.so2_baseline;
    this.returned_results["control"][0]["NO2"] = this.nox_baseline;
    this.returned_results["control"][0]["NH3"] = this.nh3_baseline;
    this.returned_results["control"][0]["VOC"] = this.voc_baseline;
    this.returned_results["control"][0]["fipscodes"] = this.statetree_items_selected;
    this.returned_results["control"][0]["tierselection"] = this.tiertree_items_selected;
    this.clearTiers();
    this.clearLocations();
    this.cPM25 = null;
    this.cSO2 = null;
    this.cNOX = null;
    this.cNH3 = null;
    this.cVOC = null;
    this.table_results = [];
    this.delta_results[0]['reset'] = true;
    
    this.results_for_table = [];
    this.updateDataBase();
    this.cobraDataService.changeData(this.delta_results);

  }
  public statetree_hasChildren(node: any): boolean {
    // Check if the parent node has children
    return node.items && node.items.length > 0;
  }

  /*Tier filter initialization */
  public tiertree_treeview: any[];
  public tiertree_checkedKeys: any[] = [];
  public tiertree_key = 'uniqueid';
  public tiertree_enableCheck = true;
  public tiertree_checkChildren = false;
  public tiertree_checkParents = false;
  public tiertree_checkOnClick = true;
  public tiertree_checkMode: any = 'single';
  public tiertree_selectionMode: any = 'single';
  public tiertree_pageSize = 10;
  private tiertree_items: any[] = null;
  private tiertree_items_selected: any = null;

  public tiertree_fetchChildren(node: any) {
    // Return the parent node's items collection as children
    return of(node.items);
  }

  public tiertree_hasChildren(node: any): boolean {
    // Check if the parent node has children
    return node.items && node.items.length > 0;
  }

  public get statetree_checkableSettings(): CheckableSettings {
    return {
      checkChildren: this.statetree_checkChildren,
      checkParents: this.statetree_checkParents,
      enabled: this.statetree_enableCheck,
      mode: this.statetree_checkMode,
      checkOnClick: this.statetree_checkOnClick
    };
  }

  public get tiertree_checkableSettings(): CheckableSettings {
    return {
      checkChildren: this.tiertree_checkChildren,
      checkParents: this.tiertree_checkParents,
      enabled: this.tiertree_enableCheck,
      mode: this.tiertree_checkMode,
      checkOnClick: this.tiertree_checkOnClick
    };
  }


  /* Custom logic handling Indeterminate state when custom data item property is persisted */
  public statetree_isChecked = (dataItem: any, index: string): CheckedState => {
    if (this.statetree_containsItem(dataItem)) {return 'checked';}

    if (this.statetree_isIndeterminate(dataItem.items)) { return 'indeterminate'; }

    return 'none';
  }

  private statetree_containsItem(item: any): boolean {
    return this.statetree_checkedKeys.indexOf(item[this.statetree_key]) > -1;
  }

  private statetree_isIndeterminate(items: any[] = []): boolean {
    let idx = 0;
    let item;

    while (item = items[idx]) {
      if (this.statetree_isIndeterminate(item.items) || this.statetree_containsItem(item)) {
        return true;
      }

      idx += 1;
    }

    return false;
  }
  /* Custom logic handling Indeterminate state when custom tier data item property is persisted */
  public tiertree_isChecked = (dataItem: any, index: string): CheckedState => {
    if (this.tiertree_containsItem(dataItem)) { return 'checked'; }

    return 'none';
  }

  private tiertree_containsItem(item: any): boolean {
    return this.tiertree_checkedKeys.indexOf(item[this.tiertree_key]) > -1;
  }

  /*Identifies selected fields in both state and tier categories  */
  public statetree_handleChecking(itemLookup: TreeItemLookup): void {
    this.statetree_items_selected = itemLookup;

/* only returns state id if state AND all counties are selected */
    const states = this.statetree_items_selected.map(sItem => {
      return sItem.length == 2;
  });
  
  var selectedStates = this.statetree_items_selected.filter((item, i) => states[i]);
  for (var i = 0; i < selectedStates.length;i++) {
    const counties = this.statetree_items_selected.map(cItem => {
      return cItem.length == 5 && cItem.startsWith(selectedStates[i]);
    });
    var selectedCounties = this.statetree_items_selected.filter((item, i) => counties[i]);
    var ts = this.findWithAttr(this.statetree_treeview, 'STFIPS',selectedStates[i]);
    if(this.statetree_treeview[ts]['items'].length == selectedCounties.length){
      this.statetree_items_selected = this.statetree_items_selected.filter((item, i) => !counties[i]);
    };
    
  }
  
    this.executedatarequest();
  }
  
  /*Identifies selected fields in both state and tier categories*/
  public tiertree_handleChecking(itemLookup: TreeItemLookup): void {
    this.tiertree_items_selected = itemLookup;
    this.executedatarequest();
  }

  public executedatarequest(): void {
    
    if (this.tiertree_items_selected != null && this.statetree_items_selected != null) {
      if (this.tiertree_items_selected.length != 0 && this.statetree_items_selected.length != 0) {
        this.cobraDataService.getEmissionsData(this.statetree_items_selected, this.tiertree_items_selected).subscribe(
          data => { this.returned_results = data; this.gatherBaselineAndControl();},
          err => console.error('An error occured getting tier items: ' + err),
          () => console.log('Retrieved tier items.'));
      } else {
        this.endApiCall = true;
        this.noRecordsReturnedCheck();
      }
    }
  }
    

  /*Requests state and tier data from API*/
  ngOnInit() {

    this.cobraDataService.currentData.subscribe(data => this.delta_results = data);
  
    this.cobraDataService.getDataDictionary_Tiers().subscribe(
      data => { this.tiertree_items = data; this.statetree_loadItems('tiers') },
      err => console.error('An error occured retrieving tier items: ' + err),
      () => console.log('Retrieved tier items')
    );
    this.cobraDataService.getDataDictionary_State().subscribe(
      data => { this.statetree_items = data; this.statetree_loadItems('states') },
      err => console.error('An error occured retrieving state items: ' + err),
      () => console.log('Retrieved state items')
    );
  }

  /*Function used to index objects in JS*/
  public statetree_loadItems(treelevel): void {
  

    /*Builds tier checkbox tree*/
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
    /*Builds state checkbox tree*/
    else if (treelevel == 'states') {

      var state = [];
      var county = [];


      for (var i = 0; i < this.statetree_items.length; i++) {
        state.push(new Object({ 'STNAME': this.statetree_items[i].STNAME, 'STFIPS': this.statetree_items[i].STFIPS, 'SOURCEINDX': this.statetree_items[i].SOURCEINDX }));
        county.push(new Object({ 'STNAME': this.statetree_items[i].STNAME, 'CYNAME': this.statetree_items[i].CYNAME, 'CNTYFIPS': this.statetree_items[i].CNTYFIPS, 'SOURCEINDX': this.statetree_items[i].SOURCEINDX }));
      }

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
}
