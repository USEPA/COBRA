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

  constructor(private cobraDataService: CobraDataService, private global: GlobalsService, private renderer: Renderer2) { 
  }

  ngOnInit() {
  }

}