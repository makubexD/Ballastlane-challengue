import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { AuthService } from './auth.service';
import { authGuard } from './auth.guard';
import { ActivatedRouteSnapshot, RouterStateSnapshot, provideRouter } from '@angular/router';

describe('authGuard', () => {
  const mockRouteSnapshot = {} as ActivatedRouteSnapshot;
  const mockRouterState = { url: '/tasks' } as RouterStateSnapshot;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        {
          provide: AuthService,
          useValue: { isAuthenticated: () => false }
        }
      ]
    });
  });

  it('should redirect to login when user is not authenticated', () => {
    const result = TestBed.runInInjectionContext(() =>
      authGuard(mockRouteSnapshot, mockRouterState)
    );

    const router = TestBed.inject(Router);
    expect(result).toEqual(router.createUrlTree(['/login']));
  });
});
