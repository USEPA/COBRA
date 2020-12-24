import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { ResultspanelComponent } from './resultspanel.component';

describe('ResultspanelComponent', () => {
  let component: ResultspanelComponent;
  let fixture: ComponentFixture<ResultspanelComponent>;

  beforeEach(async(() => {
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
