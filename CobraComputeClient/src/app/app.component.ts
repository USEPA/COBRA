import { Component } from '@angular/core';
import { CobraDataService } from './cobra-data-service.service';
import { Observable, of } from 'rxjs';
import { Token } from './Token';
import { GlobalsService } from './globals.service';

import { HttpClientModule }    from '@angular/common/http';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import 'rxjs/add/operator/map';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent {
  title = 'COBRA';
  token$: string = '';

  constructor(private cobraDataService: CobraDataService, private http: HttpClient, private global: GlobalsService) { }

  ngOnInit() {
    this.getToken();
  }

  getToken(): void {
    this.cobraDataService.getToken().subscribe(
        data => {this.token$ = data.value; this.global.setToken(data.value)},
        err => console.error(err),
        () => console.log('Failed to obtain token.')
      );
  }
}
