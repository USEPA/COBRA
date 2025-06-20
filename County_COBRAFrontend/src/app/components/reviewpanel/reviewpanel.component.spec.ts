import { ComponentFixture, TestBed, waitForAsync } from '@angular/core/testing';

import { ReviewpanelComponent } from './reviewpanel.component';

describe('ReviewpanelComponent', () => {
  let component: ReviewpanelComponent;
  let fixture: ComponentFixture<ReviewpanelComponent>;

  beforeEach(waitForAsync(() => {
    TestBed.configureTestingModule({
      declarations: [ ReviewpanelComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(ReviewpanelComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
