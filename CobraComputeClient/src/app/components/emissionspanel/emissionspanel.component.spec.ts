import { ComponentFixture, TestBed, waitForAsync } from '@angular/core/testing';

import { EmissionspanelComponent } from './emissionspanel.component';

describe('EmissionspanelComponent', () => {
  let component: EmissionspanelComponent;
  let fixture: ComponentFixture<EmissionspanelComponent>;

  beforeEach(waitForAsync(() => {
    TestBed.configureTestingModule({
      declarations: [ EmissionspanelComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(EmissionspanelComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
