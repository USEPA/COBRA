import { BrowserModule } from '@angular/platform-browser';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { NgModule } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClientModule }    from '@angular/common/http';

import { ClarityModule } from '@clr/angular';

import { AppComponent } from './app.component';
import { EmissionspanelComponent } from './components/emissionspanel/emissionspanel.component';
import { ReviewpanelComponent } from './components/reviewpanel/reviewpanel.component';
import { ResultspanelComponent } from './components/resultspanel/resultspanel.component';
import { LoadinganimationComponent } from './components/loadinganimation/loadinganimation.component';


@NgModule({
  declarations: [
    AppComponent,
    EmissionspanelComponent,
    ReviewpanelComponent,
    ResultspanelComponent,
    LoadinganimationComponent
  ],
  imports: [
    BrowserModule,
    BrowserAnimationsModule,
    HttpClientModule,
    FormsModule,
    ClarityModule
  ],
  providers: [
    EmissionspanelComponent,
    ReviewpanelComponent,
    ResultspanelComponent
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }
