import { ComponentFixture, TestBed } from '@angular/core/testing';

import { PrintablereportComponent } from './printablereport.component';
import { HttpClientTestingModule } from '@angular/common/http/testing';

describe('PrintablereportComponent', () => {
  let component: PrintablereportComponent;
  let fixture: ComponentFixture<PrintablereportComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ PrintablereportComponent ],
      imports: [HttpClientTestingModule],
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(PrintablereportComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
