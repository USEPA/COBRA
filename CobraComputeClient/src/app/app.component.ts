import { Component, Renderer2 } from '@angular/core';
import { CobraDataService } from './cobra-data-service.service';
import { GlobalsService } from './globals.service';
@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss']
})

export class AppComponent {
  title = 'CobraApp';
  token$: string = '';
  showAppErrorModal: boolean = false;

  constructor(private cobraDataService: CobraDataService, private global: GlobalsService, private renderer: Renderer2) { 
  }

  ngOnInit() {
    document.querySelector("header").scrollIntoView({behavior:'smooth'});
    this.getToken();
  }

  getToken(): void {
    this.cobraDataService.getToken().subscribe(
      data => {
        this.token$ = data.value; 
        this.global.setToken(data.value);
      },
      err => this.showAppErrorModal = true,
      () => {
              document.getElementById("statetree_spinner").setAttribute("hidden","true");
              document.getElementById("statestree_and_btns").removeAttribute("hidden");
            } 
    );
  }
}