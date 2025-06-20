import { ComponentFixture, TestBed, waitForAsync } from '@angular/core/testing';

import { ResultspanelComponent } from './resultspanel.component';

describe('ResultspanelComponent', () => {
  let component: ResultspanelComponent;
  let fixture: ComponentFixture<ResultspanelComponent>;

  beforeEach(waitForAsync(() => {
    TestBed.configureTestingModule({
      declarations: [ ResultspanelComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(ResultspanelComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
