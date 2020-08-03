import { Component, OnInit, Input, Output, EventEmitter, ViewChild } from '@angular/core';
import * as d3Base  from 'd3';
import {legendColor} from 'd3-svg-legend';
import * as topojson from "topojson";
import { Impacts } from '../impacts';
import { Token } from '../Token';
import { CobraDataService } from '../cobra-data-service.service';
import { GridDataResult, PageChangeEvent, ExcelModule } from '@progress/kendo-angular-grid';
import { ExcelExportData } from '@progress/kendo-angular-excel-export';
import { findIndex } from 'rxjs/operators';
const d3 = Object.assign(d3Base, {legendColor })

@Component({
  selector: 'app-resultssection',
  templateUrl: './resultssection.component.html',
  styleUrls: ['./resultssection.component.css']
})

export class ResultssectionComponent implements OnInit {
  @Input() token: Token;
  @Output() theEmitter = new EventEmitter<any>();

  @ViewChild('dropdownlistcounties') private dropdownlistcounties;

  public delta_results: any[] = [];

  public statetree_treeview: any[] = [];
  public statedropdown_data: any[] = [];

  public map_filter: any[] = [];
  public selectedMapValue: string = "";
  public gridView: GridDataResult;

  public pageSize = 40;
  public skip = 0;

  private data: Object[];
  public items: any[] = null;

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
  public states: any[] = [];
  public counties: any[] = [];
  public ddstate_defaultItem: { text: string, value: string } = { text: "US", value: '00' };
  public ddstate_defaultCountyItem: { text: string, value: string } = { text: "ALL", value: '' };
  public statefiltervalue = '00';
  public countyfiltervalue = '000';
  public filtervalue = '00';
  public filteredMapArray: any[] = [];
  public filterDisplayName: any[] = [];
  public scaleName: any[] = ["Equal Interval",
  "Equal Frequency"];
  public scaleValue: 'Equal Interval';
  public scaleDisabled: boolean = true;
  public mapTitledisabled: boolean = true;
  public mapVar: any[] = [];

  findWithAttr(array, attr, value) {
    for (var i = 0; i < array.length; i += 1) {
      if (array[i][attr] === value) {
        return i;
      }
    }
    return -1;
  }


  constructor(private cobraDataService: CobraDataService) {
    this.allData = this.allData.bind(this);
  }

  public stateselectionChange(value: any): void {
    this.statefiltervalue = value.value;
    this.filtervalue = this.statefiltervalue;

    this.counties = value.thecounties;
    this.dropdownlistcounties.value = this.ddstate_defaultCountyItem;

    console.log('state selection change', this.filtervalue);
    this.getResults(null);
  }


  public countyselectionChange(value: any): void {
    console.log('county selection change.', value.value);
    this.countyfiltervalue = value.value;
    this.filtervalue = this.statefiltervalue + this.countyfiltervalue;
    console.log('county selection change. filter now ', this.filtervalue);
    this.getResults(null);
  }

  public mapFilter(value: any): void {
    this.filteredMapArray = [];
    var ddMapIndex = this.map_filter[this.filterDisplayName.indexOf(value)];
    for (var m = 0; m < this.items.length; m++) {
      this.filteredMapArray.push({name:ddMapIndex,value:this.items[m][ddMapIndex],fips:this.items[m]['FIPS']});
    }
    this.mapTitledisabled = false;
    this.mapVar = value;
    this.createD3Map(this.filteredMapArray,this.scaleValue);
  
  }

  public scaleFilter(value: any): void {
    this.scaleValue = value;
    this.scaleDisabled = false;
    this.createD3Map(this.filteredMapArray,this.scaleValue);
  
  }


  getTableResults() {

    this.cobraDataService.currentData.subscribe(data => this.delta_results = data);
    this.cobraDataService.stateCountyData.subscribe(data => this.statetree_treeview = data);
    this.states = [];
    for (var k = 0; k < this.statetree_treeview.length; k++) {
      var counties: any[] = [];
      for (var l = 0; l < this.statetree_treeview[k].items.length; l++) {
        counties.push( { text: this.statetree_treeview[k].items[l]['text'], value: this.statetree_treeview[k].items[l]['CNTYFIPS'] } );
      }
      this.states.push( { text: this.statetree_treeview[k]['text'], value: this.statetree_treeview[k]['STFIPS'], thecounties : counties } );
    }
  }

  createD3Map(map_array,scaleValue) {
    var svg = d3.select("svg");
    var projection = d3.geoAlbers()
      .translate([1400 / 2, 600 / 2])
      .scale([1000]);

    var path = d3.geoPath().projection(projection);
    var scale_changer = [];
    for (var i = 0; i < map_array.length; i += 1) {
        scale_changer.push(map_array[i]['value']);
      }

      var scale_min = Math.min(...scale_changer);
      var scale_max = Math.max(...scale_changer);
var colorRange = ['#f7fbff',
  '#deebf7',
  '#c6dbef',
  '#9ecae1',
  '#6baed6',
  '#4292c6',
  '#2171b5',
  '#08519c',
  '#08306b'];
      /* default scale to Equal Interval */
      var colorScale = d3.scaleQuantize()
      .domain(d3.extent(scale_changer))
      .range(colorRange);


  
 /*if(scaleValue == 'Equal Frequency'){
    colorScale = d3.scaleQuantile()
    .domain(scale_changer)
    .range(colorRange);
  
 }else if (scaleValue == 'Equal Interval'){
    colorScale = d3.scaleQuantize()
    .domain(d3.extent(scale_changer))
    .range(colorRange);
 }*/

 /* add legend */
 svg.append("g")
  .attr("class", "legendQuant")
  .attr("transform", "translate(30,80)");
var legend = d3.legendColor();
if(this.mapVar.includes("$")){
  legend = d3.legendColor()
  .labelFormat(d3.format("$,.2f"))
  .scale(colorScale);
}else{
 legend = d3.legendColor()
  .labelFormat(d3.format(",.2f"))
  .scale(colorScale);
}
svg.select(".legendQuant")
  .call(legend);
          
    var map = d3.json("./assets/map.js");
    //var cities = d3.csv("cities.csv");
    Promise.all([map,map_array]).then(function (data) {
        svg.append("g")
            .attr("stroke", "#777")
            .attr("stroke-width", 0.35)
            .selectAll("path")
            .data(data[0].features)
            .enter()
            //draw counties
            .append("path")
            .attr("class", "counties")
            .attr("d", path)
            // fill
            .attr("fill", function (d) {
              for (var i = 0; i < map_array.length; i ++) {
                if (map_array[i]['fips'] === d['properties']['GEOID']) {
                  var mapvalue = map_array[i]['value'];
                }
              }
              return colorScale(mapvalue);
            });
            


    });

    // add a legend
/* var lowColor = colorRange[0];
 var highColor = colorRange[colorRange.length-1];
   var w = 140, h = 300;
   d3.select(".legend").remove()
   var key = d3.select("svg")
   .append("svg")
     .attr("width", w)
     .attr("height", h)
     .attr("class", "legend");
 
   var legend = key.append("defs")
     .append("svg:linearGradient")
     .attr("id", "gradient")
     .attr("x1", "100%")
     .attr("y1", "0%")
     .attr("x2", "100%")
     .attr("y2", "100%")
     .attr("spreadMethod", "pad");
 
   legend.append("stop")
     .attr("offset", "0%")
     .attr("stop-color", highColor)
     .attr("stop-opacity", 1);
     
   legend.append("stop")
     .attr("offset", "100%")
     .attr("stop-color", lowColor)
     .attr("stop-opacity", 1);
 
   key.append("rect")
     .attr("width", w - 100)
     .attr("height", h)
     .style("fill", "url(#gradient)")
     .attr("transform", "translate(0,10)");
 
   var y = d3.scaleLinear()
     .range([h, 0])
     .domain([scale_min, scale_max]);
 
   var yAxis = d3.axisRight(y);
 
   key.append("g")
     .attr("class", "y axis")
     .attr("transform", "translate(41,10)")
     .call(yAxis)*/

     
  }


  ngOnInit() {
   

  }

  onButtonClick() {
    this.getResults(null);
  }

  command(action: any) {
    this.theEmitter.emit(action);
  }

  SummaryExport(){
    this.cobraDataService.SummaryExcelExport(this.filtervalue);
  }

  getResults(callback): void {
    if (callback) {
      console.log('Callback registered.');
    }
    this.cobraDataService.getResults('00').subscribe(
      data => { 
      console.log(data)
      if(callback) {
        console.log('Callback executing.');
        callback();
       }},
      
     
     
    err => console.error('An error occured retrieving results: ' + err),
    () => console.log('Retrieved results.')
  );
    this.cobraDataService.getResults(this.filtervalue).subscribe(
        data => { this.items = data["Impacts"];
        var item = Object.keys(this.items[0]);
        item.splice(0,2);
        item.splice(-3,3);
        this.filterDisplayName = ["Base PM 2.5",
        "Control PM 2.5",
        "Delta PM 2.5",
        "Acute Bronchitis",
        "Nonfatal Heart Attacks (high estimate)",
        "Nonfatal Heart Attacks (low estimate)",
        "Asthma Exacerbation, Cough",
        "Asthma Exacerbation, Shortness of Breath",
        "Asthma Exacerbation, Wheeze",
        "Emergency Room Visits, Asthma",
        "Hospital Admits, Cardiovascular (except heart attacks)",
        "Hospital Admits, All Respiratory",
        "Hospital Admits, Asthma",
        "Hospital Admits, Chronic Lung Disease",
        "Lower Respiratory Symptoms",
        "Minor Restricted Activity Days",
        "Mortality (low estimate)",
        "Mortality (high estimate)",
        "Infant Mortality",
        "Upper Respiratory Symptoms",
        "Work Loss Days",
        "$ Total Health Benefits Low Value",
        "$ Total Health Benefits High Value",
        "$ Acute Bronchitis",
        "$ Nonfatal Heart Attacks (high estimate)",
        "$ Nonfatal Heart Attacks (low estimate)",
        "$ Asthma Exacerbation",
        "$ Emergency Room Visits, Asthma",
        "$ Hospital Admits, Cardiovascular (except heart attacks)",
        "$ Hospital Admits, All Respiratory",
        "$ Lower Respiratory Symptoms",
        "$ Minor Restricted Activity Days",
        "$ Mortality (low estimate)",
        "$ Mortality (high estimate)",
        "$ Infant Mortality",
        "$ Upper Respiratory Symptoms",
        "$ Work Loss Days"];
      
        this.map_filter = item;
        console.log(this.map_filter);

        var summary = data["Summary"];
        this.TotalHealthBenefitsValue_high = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0, minimumFractionDigits: 0 }).format(summary["TotalHealthBenefitsValue_high"]);
        this.TotalHealthBenefitsValue_low = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0, minimumFractionDigits: 0 }).format(summary["TotalHealthBenefitsValue_low"]);

        this.Mortality_low = new Intl.NumberFormat('en-US', { style: 'decimal', maximumFractionDigits: 3, minimumFractionDigits: 3 }).format(summary["Mortality_All_Cause__low_"]);
        this.Mortality_high = new Intl.NumberFormat('en-US', { style: 'decimal', maximumFractionDigits: 3, minimumFractionDigits: 3 }).format(summary["Mortality_All_Cause__high_"]);
        this.NonfatalHeartAttacks_low = new Intl.NumberFormat('en-US', { style: 'decimal', maximumFractionDigits: 3, minimumFractionDigits: 3 }).format(summary["Acute_Myocardial_Infarction_Nonfatal__low_"]);
        this.NonfatalHeartAttacks_high = new Intl.NumberFormat('en-US', { style: 'decimal', maximumFractionDigits: 3, minimumFractionDigits: 3 }).format(summary["Acute_Myocardial_Infarction_Nonfatal__high_"]);

        this.InfantMortality = new Intl.NumberFormat('en-US', { style: 'decimal', maximumFractionDigits: 3, minimumFractionDigits: 3 }).format(summary["Infant_Mortality"]);
        this.HospitalAdmitsAllRespiratory = new Intl.NumberFormat('en-US', { style: 'decimal', maximumFractionDigits: 3, minimumFractionDigits: 3 }).format(summary["HA_All_Respiratory"] + summary["HA_Chronic_Lung_Disease"]);

        this.HospitalAdmitsCardiovascularexceptheartattacks = new Intl.NumberFormat('en-US', { style: 'decimal', maximumFractionDigits: 3, minimumFractionDigits: 3 }).format(summary["HA_All_Cardiovascular__less_Myocardial_Infarctions_"]);

        this.AcuteBronchitis = new Intl.NumberFormat('en-US', { style: 'decimal', maximumFractionDigits: 3, minimumFractionDigits: 3 }).format(summary["Acute_Bronchitis"]);
        this.UpperRespiratorySymptoms = new Intl.NumberFormat('en-US', { style: 'decimal', maximumFractionDigits: 3, minimumFractionDigits: 3 }).format(summary["Upper_Respiratory_Symptoms"]);
        this.LowerRespiratorySymptoms = new Intl.NumberFormat('en-US', { style: 'decimal', maximumFractionDigits: 3, minimumFractionDigits: 3 }).format(summary["Lower_Respiratory_Symptoms"]);
        this.EmergencyRoomVisitsAsthma = new Intl.NumberFormat('en-US', { style: 'decimal', maximumFractionDigits: 3, minimumFractionDigits: 3 }).format(summary["Emergency_Room_Visits_Asthma"]);
        this.AsthmaExacerbation = new Intl.NumberFormat('en-US', { style: 'decimal', maximumFractionDigits: 3, minimumFractionDigits: 3 }).format(summary["Asthma_Exacerbation_Cough"] + summary["Asthma_Exacerbation_Shortness_of_Breath"] + summary["Asthma_Exacerbation_Wheeze"]);
        this.MinorRestrictedActivityDays = new Intl.NumberFormat('en-US', { style: 'decimal', maximumFractionDigits: 3, minimumFractionDigits: 3 }).format(summary["Minor_Restricted_Activity_Days"]);
        this.WorkLossDays = new Intl.NumberFormat('en-US', { style: 'decimal', maximumFractionDigits: 3, minimumFractionDigits: 3 }).format(summary["Work_Loss_Days"]);


        this.MortalityValue_low = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0, minimumFractionDigits: 0 }).format(summary["C__Mortality_All_Cause__low_"]);
        this.MortalityValue_high = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0, minimumFractionDigits: 0 }).format(summary["C__Mortality_All_Cause__high_"]);
        this.NonfatalHeartAttacksValue_low = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0, minimumFractionDigits: 0 }).format(summary["C__Acute_Myocardial_Infarction_Nonfatal__low_"]);
        this.NonfatalHeartAttacksValue_high = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0, minimumFractionDigits: 0 }).format(summary["C__Acute_Myocardial_Infarction_Nonfatal__high_"]);

        this.InfantMortalityValue = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0, minimumFractionDigits: 0 }).format(summary["C__Infant_Mortality"]);
        this.HospitalAdmitsAllRespiratoryValue = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0, minimumFractionDigits: 0 }).format(summary["C__CVD_Hosp_Adm"]);

        this.HospitalAdmitsCardiovascularexceptheartattacksValue = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0, minimumFractionDigits: 0 }).format(summary["C__Resp_Hosp_Adm"]);
        this.AcuteBronchitisValue = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0, minimumFractionDigits: 0 }).format(summary["C__Acute_Bronchitis"]);

        this.UpperRespiratorySymptomsValue = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0, minimumFractionDigits: 0 }).format(summary["C__Upper_Respiratory_Symptoms"]);
        this.LowerRespiratorySymptomsValue = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0, minimumFractionDigits: 0 }).format(summary["C__Lower_Respiratory_Symptoms"]);
        this.EmergencyRoomVisitsAsthmaValue = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0, minimumFractionDigits: 0 }).format(summary["C__Emergency_Room_Visits_Asthma"]);

        this.AsthmaExacerbationValue = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0, minimumFractionDigits: 0 }).format(summary["C__Asthma_Exacerbation"]);
        this.MinorRestrictedActivityDaysValue = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0, minimumFractionDigits: 0 }).format(summary["C__Minor_Restricted_Activity_Days"]);
        this.WorkLossDaysValue = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0, minimumFractionDigits: 0 }).format(summary["C__Work_Loss_Days"]);

        if (callback) {
          console.log('Callback executing.');
          callback();
        }

        this.loadItems();


      },
      err => console.error('An error occured retrieving results: ' + err),
      () => console.log('Retrieved results.')
    );
  }

  public pageChange(event: PageChangeEvent): void {
    this.skip = event.skip;
    this.loadItems();
  }

  private loadItems(): void {
    this.gridView = {
      data: this.items.slice(this.skip, this.skip + this.pageSize),
      total: this.items.length
    };
  }

  public allData(): ExcelExportData {
    const result: ExcelExportData = {
      data: this.items
    };
    return result;
  }

}
