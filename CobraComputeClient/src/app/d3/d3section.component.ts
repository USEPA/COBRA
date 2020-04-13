import { Component, OnInit, Input, Output, EventEmitter } from '@angular/core';
import * as d3 from 'd3';
import * as topojson from "topojson";
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { CobraDataService } from '../cobra-data-service.service';
import { GridDataResult, PageChangeEvent, ExcelModule } from '@progress/kendo-angular-grid';
import { ExcelExportData } from '@progress/kendo-angular-excel-export';
import { Token } from '../Token';

@Component({
  selector: 'app-d3section',
  templateUrl: './d3section.component.html',
  styleUrls: ['./d3section.component.css']

})

export class D3sectionComponent implements OnInit {
  public delta_results: any[] = [];
  public statetree_treeview: any[];
  public results_for_table: any[] = [];
  public unit_check: any[] = [];
  public pageSize = 40;
  public skip = 0;
  private data: Object[];
  @Input() token: Token;
  @Output() theEmitter = new EventEmitter<any>();

  public gridView: GridDataResult;

  constructor(private cobraDataService: CobraDataService, private http: HttpClient) {
  }

  ngOnInit() {
  }

  command(action: any) {
    this.theEmitter.emit(action);
  }

  getTableResults() {
    this.cobraDataService.currentData.subscribe(data => this.delta_results = data);
    this.calculateDelta();
  }

  calculateDelta() {
      if (Object.keys(this.delta_results).length > 0) {
          for (var i = 0; i < this.delta_results.length; i++) {    
              this.results_for_table.push(this.delta_results[i]);
          }
          
          this.results_for_table = this.results_for_table.reverse().filter((thing, index) => {
              const _thing = JSON.stringify({'state':thing['state'],'county':thing['county'],'tier1name':thing['tier1name']});
            
              return index === this.results_for_table.findIndex(obj => {
                  return JSON.stringify({'state':obj['state'],'county':obj['county'],'tier1name':obj['tier1name']}) === _thing;
              });
          });
          if (this.delta_results[0]['reset']){
            this.results_for_table = [];
            this.cobraDataService.changeData([]);
          }
          this.results_for_table = this.results_for_table.reverse();
      }

    this.gridView = {
      data: this.delta_results.slice(this.skip, this.skip + this.pageSize),
      total: Object.keys(this.delta_results).length
    };
  }
  public pageChange(event: PageChangeEvent): void {
    this.skip = event.skip;
    this.loadItems();
  }

  private loadItems(): void {
    this.gridView = {
      data: this.delta_results.slice(this.skip, this.skip + this.pageSize),
      total: this.delta_results.length
    };
  }

  public allData(): ExcelExportData {
    const result: ExcelExportData = {
      data: this.delta_results
    };
    return result;
  }


}
