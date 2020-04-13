import { TestBed } from '@angular/core/testing';

import { CobraDataServiceService } from './cobra-data-service.service';

describe('CobraDataServiceService', () => {
  beforeEach(() => TestBed.configureTestingModule({}));

  it('should be created', () => {
    const service: CobraDataServiceService = TestBed.get(CobraDataServiceService);
    expect(service).toBeTruthy();
  });
});
