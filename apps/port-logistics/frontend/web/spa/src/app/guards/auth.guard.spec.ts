import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { authGuard } from './auth.guard';

describe('authGuard', () => {
  const executeGuard = authGuard;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [Router]
    });
  });

  it('should be created', () => {
    expect(executeGuard).toBeTruthy();
  });
});
