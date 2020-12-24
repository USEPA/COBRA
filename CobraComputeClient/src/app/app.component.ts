import { Component } from '@angular/core';
import { CobraDataService } from './cobra-data-service.service';
import { GlobalsService } from './globals.service';
@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['../theme/styles.scss', './app.component.scss']
})

export class AppComponent {
  title = 'CobraApp';
  token$: string = '';

  constructor(private cobraDataService: CobraDataService, private global: GlobalsService) { }

  ngOnInit() {
    this.getToken();
  }

  getToken(): void {
    this.cobraDataService.getToken().subscribe(
        data => {this.token$ = data.value; this.global.setToken(data.value)},
        err => console.error('Failed to obtain token.'+err),
        () => console.log('Token call completed.')
      );
  }
}