import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { signal } from '@angular/core';
import { ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { AuthService } from './auth.service';
import { noAuthGuard } from './no-auth.guard';

const MOCK_ROUTE = {} as ActivatedRouteSnapshot;
const MOCK_STATE = {} as RouterStateSnapshot;

describe('noAuthGuard', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        {
          provide: AuthService,
          useValue: { isAuthenticated: signal(false) }
        }
      ]
    });
  });

  it('should redirect to /tasks when user is already authenticated', () => {
    TestBed.overrideProvider(AuthService, {
      useValue: { isAuthenticated: signal(true) }
    });

    const result = TestBed.runInInjectionContext(() =>
      noAuthGuard(MOCK_ROUTE, MOCK_STATE)
    );

    const router = TestBed.inject(Router);
    expect(result).toEqual(router.createUrlTree(['/tasks']));
  });

  it('should allow access when user is not authenticated', () => {
    const result = TestBed.runInInjectionContext(() =>
      noAuthGuard(MOCK_ROUTE, MOCK_STATE)
    );

    expect(result).toBe(true);
  });
});
