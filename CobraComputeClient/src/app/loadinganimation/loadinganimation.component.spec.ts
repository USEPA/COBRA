import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { LoadinganimationComponent } from './loadinganimation.component';

describe('LoadinganimationComponent', () => {
  let component: LoadinganimationComponent;
  let fixture: ComponentFixture<LoadinganimationComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ LoadinganimationComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(LoadinganimationComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
