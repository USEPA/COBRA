import { Component, OnInit, Renderer2, ViewChild } from '@angular/core';
import { CobraDataService } from 'src/app/cobra-data-service.service';
import { GlobalsService } from 'src/app/globals.service';
import { EmissionspanelComponent } from 'src/app/components/emissionspanel/emissionspanel.component';
import { ReviewpanelComponent } from 'src/app/components/reviewpanel/reviewpanel.component';
import { ResultspanelComponent } from 'src/app/components/resultspanel/resultspanel.component';

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.scss'],
})
export class HomeComponent implements OnInit {
  @ViewChild(EmissionspanelComponent) emissionspanelComponent: EmissionspanelComponent;
  @ViewChild(ReviewpanelComponent) reviewpanelComponent: ReviewpanelComponent;
  @ViewChild(ResultspanelComponent) resultspanelComponent!: ResultspanelComponent;

  title = 'CobraApp';
  token$: string = '';
  mode$: string = '';
  queueData: {};
  populationData = [];

  showAppErrorModal: boolean = false;

  constructor(private cobraDataService: CobraDataService, private global: GlobalsService, private renderer: Renderer2) {
  }

  async ngOnInit() {
    document.querySelector("header")?.scrollIntoView({ behavior: 'smooth' });
  
    try {
      // await the token retrieval
      await this.getToken();
  
      // fetch population data
      this.cobraDataService.getPopulationData().subscribe(
        (data) => {
          this.populationData = data;
        },
        (err) => console.error('An error occurred getting population data: ' + err)
      );
    } catch (error) {
      console.error('An error occurred in ngOnInit:', error);
    }
  }

  ngAfterViewInit() {
    setTimeout(() => {
      if(this.mode$ == "AVERT") {
        this.emissionspanelComponent.addComponentToScenario(null, this.queueData, false);
      }
    })
  }

  previewReport(): boolean {
    return this.resultspanelComponent?.showReportPreview;
  }

  getFilterState(): any {
    return {
      tableStates: this.resultspanelComponent?.tableStates || {
        'all state': 'All Contiguous U.S. States',
        'selected state': '',
        'selected county': '',
      },
      countyName: this.resultspanelComponent?.countyName,
      countyFIPS: this.resultspanelComponent?.countyFIPS,
      selectedTableState: this.resultspanelComponent?.selectedTableState || 'all state',
      filterValue: this.resultspanelComponent?.filtervalue || '00',
    }
  }

  setReportPreview(setting: boolean): void {
    if (this.resultspanelComponent) {
      this.resultspanelComponent.setShowReportPreview(setting);
    }
  }

  getResultsFilter(): string {
    if (this.resultspanelComponent) {
      return this.resultspanelComponent.selectedTableState;
    }
  }


  getInputComponents(): any {
    if (this.reviewpanelComponent) {
      return this.reviewpanelComponent.components;
    }
  }

  getStatesOptions(): any {
    if (this.resultspanelComponent) {
      return this.resultspanelComponent.state_clr_structure;
    }
  }

  getDiscountRate(): any {
    if (this.reviewpanelComponent) {
      return this.reviewpanelComponent.discountRate;
    }
  }

  getCurrentResults(): any {
    if (this.resultspanelComponent) {
      return {
        Items: this.resultspanelComponent.items,
        Summary: this.resultspanelComponent.summary,
      };
    }
  }


  getToken(): void {
    let check = this.global.getToken();
    this.mode$ = this.global.getMode();

    if (check == '') {
      this.cobraDataService.getToken().subscribe(
        data => {
          this.token$ = data.value;
          this.global.setToken(data.value);
        },
        err => this.showAppErrorModal = true,
        () => {
          document.getElementById("statetree_spinner").setAttribute("hidden", "true");
          document.getElementById("statestree_and_btns").removeAttribute("hidden");
        }
      );
    } else {
      this.token$ = check;
      this.mode$ = this.global.getMode();
      this.queueData = this.global.getQeue();
      console.log("queueData ==> ", this.queueData);
    }
  }

}