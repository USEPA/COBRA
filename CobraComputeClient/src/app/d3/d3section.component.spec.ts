import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { EmissionssectionComponent } from './emissionssection.component';

describe('EmissionssectionComponent', () => {
  let component: EmissionssectionComponent;
  let fixture: ComponentFixture<EmissionssectionComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ EmissionssectionComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(EmissionssectionComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
