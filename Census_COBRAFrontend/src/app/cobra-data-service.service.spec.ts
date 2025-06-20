import { TestBed } from '@angular/core/testing';

import { CobraDataService } from './cobra-data-service.service';

describe('CobraDataService', () => {
  beforeEach(() => TestBed.configureTestingModule({}));

  it('should be created', () => {
    const service: CobraDataService = TestBed.get(CobraDataService);
    expect(service).toBeTruthy();
  });
});
