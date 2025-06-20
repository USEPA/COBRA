import { Component, OnInit, Renderer2, ViewChild } from '@angular/core';
import { CobraDataService } from 'src/app/cobra-data-service.service';
import { GlobalsService } from 'src/app/globals.service';
import { EmissionspanelComponent } from 'src/app/components/emissionspanel/emissionspanel.component';
import { ReviewpanelComponent } from 'src/app/components/reviewpanel/reviewpanel.component';

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.scss'],
})
export class HomeComponent implements OnInit {
  @ViewChild(EmissionspanelComponent) emissionspanelComponent: EmissionspanelComponent;
  @ViewChild(ReviewpanelComponent) reviewpanelComponent: ReviewpanelComponent;

  title = 'CobraApp';
  token$: string = '';
  mode$: string = '';
  queueData: {};

  showAppErrorModal: boolean = false;

  constructor(private cobraDataService: CobraDataService, private global: GlobalsService, private renderer: Renderer2) {
  }

  ngOnInit() {
    document.querySelector("header").scrollIntoView({ behavior: 'smooth' });
    //also get queue
    this.getToken();
  }

  ngAfterViewInit() {
    setTimeout(() => {
      if(this.mode$ == "AVERT") {
        this.emissionspanelComponent.addComponentToScenario(null, this.queueData, false);
      }
    })
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