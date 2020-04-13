import { BrowserModule } from '@angular/platform-browser';
import { NgModule } from '@angular/core';

import { AppComponent } from './app.component';
import { TreeViewModule } from '@progress/kendo-angular-treeview';
import { FormsModule } from '@angular/forms';
import { LayoutModule } from '@progress/kendo-angular-layout';

import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { MainpanelComponent } from './mainpanel/mainpanel.component';

import { HttpClientModule }    from '@angular/common/http';
import { GridModule } from '@progress/kendo-angular-grid';
import { ExcelModule } from '@progress/kendo-angular-grid';
import { PDFModule } from '@progress/kendo-angular-grid';
import { EmissionssectionComponent } from './emissionssection/emissionssection.component';
import { ReviewsectionComponent } from './reviewsection/reviewsection.component';
import { ResultssectionComponent } from './resultssection/resultssection.component';
import { D3sectionComponent } from './d3/d3section.component';
import { ButtonsModule } from '@progress/kendo-angular-buttons';
import { ExcelExportModule } from '@progress/kendo-angular-excel-export';
import { DropDownsModule } from '@progress/kendo-angular-dropdowns';

import { MatTabsModule } from '@angular/material/tabs';
import { LoadinganimationComponent } from './loadinganimation/loadinganimation.component';


@NgModule({
  declarations: [
    AppComponent,
    MainpanelComponent,
    ResultssectionComponent,
    ReviewsectionComponent,
    EmissionssectionComponent,
    D3sectionComponent,
    LoadinganimationComponent
  ],
  imports: [
    BrowserModule,
    TreeViewModule,
    BrowserAnimationsModule,
    LayoutModule,
    HttpClientModule,
    GridModule,
    ExcelModule,
    PDFModule,
    ButtonsModule,
    FormsModule,
    ExcelExportModule,
    MatTabsModule,
    DropDownsModule
  ],
  providers: [],
  bootstrap: [AppComponent]
})
export class AppModule { }
