import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { ResultssectionComponent } from './resultssection.component';

describe('ResultssectionComponent', () => {
  let component: ResultssectionComponent;
  let fixture: ComponentFixture<ResultssectionComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ ResultssectionComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(ResultssectionComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
