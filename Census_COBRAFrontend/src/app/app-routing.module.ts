import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HomeComponent } from './components/home/home.component';
import { ExternalScenarioComponent } from './components/external-scenario/external-scenario.component';
import { RouterModule, Routes } from '@angular/router';
import { FAQComponent } from './components/faq/faq.component';

const routes: Routes = [
  { path: '', component: HomeComponent },
  { path: 'externalscenario/:token', component: ExternalScenarioComponent },
  { path: 'cobra-beta-questions-and-answers', component: FAQComponent }
];

@NgModule({
  imports: [RouterModule.forRoot(routes, { relativeLinkResolution: 'legacy' })],
  exports: [RouterModule]
})
  
  
export class AppRoutingModule { }
