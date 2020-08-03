import { Component, OnInit, ViewChild, AfterViewInit } from '@angular/core';
import { ResultssectionComponent } from '../resultssection/resultssection.component';
import { ReviewresultsComponent } from '../reviewresults/reviewsection.component';
import { EmissionssectionComponent } from '../emissionssection/emissionssection.component';
import { CobraDataService } from '../cobra-data-service.service';
import { SelectMultipleControlValueAccessor } from '@angular/forms';

@Component({
  selector: 'app-mainpanel',
  templateUrl: './mainpanel.component.html',
  styleUrls: ['./mainpanel.component.css']
})
export class MainpanelComponent implements AfterViewInit {
  public theBoundCallback: Function;

  showLoadAnimation: Boolean = false;

  public expandedItem = 'Create Scenario';

  @ViewChild(ResultssectionComponent) ressection;
  
  /* link between main panel and output table in review scenario section */
  @ViewChild(ReviewresultsComponent) reviewresultssection;

  @ViewChild(EmissionssectionComponent) emissionssection;

  @ViewChild('panelbar') private panelbar;

  onStateChange(e) {
    this.expandedItem = e.filter(item => item.selected)[0].title;
    for (var i = 0; i < e.length; i++) {
      if(e[i]['expanded'] == true && e[i]['title'] == "Review Scenario"){
        this.reviewresultssection.getTableResults();
      }else if(e[i]['expanded'] == true && e[i]['title'] == "Results"){
        this.ressection.getTableResults();
      }
    } 
  }


  hideSpinner(): void {
    this.showLoadAnimation = false;
  }
  
  processEmittedEvent(event: any) {
    if (event['action'] == 'review') {
      this.panelbar.stateChange.next([{ title: 'Review Scenario', expanded: true, selected: true }]);
    } else if (event['action'] == 'startspin') {
      this.showLoadAnimation = true;
    } else if (event['action'] == 'stopspin') {
      this.showLoadAnimation = false;
    } else {
      this.showLoadAnimation = true;
      if (event['action'] == 'go') {
        this.ressection.getResults(this.theBoundCallback);
        this.panelbar.stateChange.next([{ title: 'Results', expanded: true, selected: true }]);
      } else {
        this.cobraDataService.ExcelExport(event['action'], this.theBoundCallback);
      }
    }
  }

  constructor(private cobraDataService: CobraDataService) {
    this.theBoundCallback = this.hideSpinner.bind(this);
  }
  
  ngAfterViewInit() {
  }

}
