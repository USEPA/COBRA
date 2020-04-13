import { TestBed } from '@angular/core/testing';

import { GlobalsService } from './globals.service';

describe('GlobalsService', () => {
  beforeEach(() => TestBed.configureTestingModule({}));

  it('should be created', () => {
    const service: GlobalsService = TestBed.get(GlobalsService);
    expect(service).toBeTruthy();
  });
});
