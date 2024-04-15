import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { MainpanelComponent } from './mainpanel.component';

describe('MainpanelComponent', () => {
  let component: MainpanelComponent;
  let fixture: ComponentFixture<MainpanelComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ MainpanelComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(MainpanelComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
