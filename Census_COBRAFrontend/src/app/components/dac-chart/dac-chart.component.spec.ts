import { ComponentFixture, TestBed } from '@angular/core/testing';

import { DacChartComponent } from './dac-chart.component';

describe('DacChartComponent', () => {
  let component: DacChartComponent;
  let fixture: ComponentFixture<DacChartComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ DacChartComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(DacChartComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
