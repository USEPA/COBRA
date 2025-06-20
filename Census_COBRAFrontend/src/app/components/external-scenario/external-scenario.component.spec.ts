import { ComponentFixture, TestBed, waitForAsync } from '@angular/core/testing';

import { ExternalScenarioComponent } from './external-scenario.component';

describe('ExternalScenarioComponent', () => {
  let component: ExternalScenarioComponent;
  let fixture: ComponentFixture<ExternalScenarioComponent>;

  beforeEach(waitForAsync(() => {
    TestBed.configureTestingModule({
      declarations: [ ExternalScenarioComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(ExternalScenarioComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
