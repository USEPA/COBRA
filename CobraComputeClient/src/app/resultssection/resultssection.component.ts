import { Component, OnInit, Input, Output, EventEmitter } from '@angular/core';
import * as d3 from 'd3';
import * as topojson from "topojson";
import { Impacts } from '../impacts';
import { Token } from '../Token';
import { CobraDataService } from '../cobra-data-service.service';
import { GridDataResult, PageChangeEvent, ExcelModule } from '@progress/kendo-angular-grid';
import { ExcelExportData } from '@progress/kendo-angular-excel-export';


@Component({
  selector: 'app-resultssection',
  templateUrl: './resultssection.component.html',
  styleUrls: ['./resultssection.component.css']
})

export class ResultssectionComponent implements OnInit {
  @Input() token: Token;
  @Output() theEmitter = new EventEmitter<any>();

  public delta_results: any[] = [];
  public statetree_treeview: any[] = [];
  public statedropdown_data: any[] = [];
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
  public filtervalue = '00';

  constructor(private cobraDataService: CobraDataService) {
    this.allData = this.allData.bind(this);
  }

  public selectionChange(value: any): void {
    this.filtervalue = value.value;
    console.log('selectionChange', value);
    this.getResults(null);
  }

  getTableResults() {
    this.cobraDataService.currentData.subscribe(data => this.delta_results = data);
    this.cobraDataService.stateCountyData.subscribe(data => this.statetree_treeview = data);
    this.states = [];
    for (var k = 0; k < this.statetree_treeview.length; k++) {
      this.states.push( { text: this.statetree_treeview[k]['text'], value: this.statetree_treeview[k]['STFIPS'] } );
    }
    this.createD3Map();
  }

  createD3Map() {
    var svg = d3.select("svg");
    var projection = d3.geoAlbers()
      .translate([860 / 2, 600 / 2]);

    var path = d3.geoPath().projection(projection);

    var colorScale = d3.scaleLinear()
      .domain([0, 100])
      .range(d3.schemeBlues[7]);



    d3.json("./assets/map.js").then(function (data) {

      //console.log('map data is: ');
      //console.log(data);
      //console.log('<<');
      svg.append("g")
        .attr("stroke", "#777")
        .attr("stroke-width", 0.35)
        .selectAll("path")
        .data(data.features)
        .enter()
        //draw counties
        .append("path")
        .attr("class", "counties")
        .attr("d", path)
        // fill
        .attr("fill", function (d) {
          //console.log(d);
          //d.total = data.get(d.id) || 0;
          let pick = Math.floor(Math.random() * 80) + 20
          return colorScale(pick);
        });


    });
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

    this.cobraDataService.getResults(this.filtervalue).subscribe(
      data => {
        this.items = data["Impacts"];
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
