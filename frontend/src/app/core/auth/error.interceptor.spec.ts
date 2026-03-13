import { TestBed } from '@angular/core/testing';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { HttpClient } from '@angular/common/http';
import { errorInterceptor } from './error.interceptor';
import { AuthService } from './auth.service';

const PROTECTED_URL = '/api/tasks';
const LOGIN_URL = '/api/auth/login';
const ME_URL = '/api/auth/me';

describe('errorInterceptor', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;
  const mockAuthService = { logout: vi.fn() };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([errorInterceptor])),
        provideHttpClientTesting(),
        { provide: AuthService, useValue: mockAuthService }
      ]
    });

    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
    vi.clearAllMocks();
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should call authService.logout() on 401 for a protected endpoint', () => {
    http.get(PROTECTED_URL).subscribe({ error: () => undefined });

    httpMock.expectOne(PROTECTED_URL).flush(null, { status: 401, statusText: 'Unauthorized' });

    expect(mockAuthService.logout).toHaveBeenCalledOnce();
  });

  it('should not call authService.logout() on 401 for /api/auth/login', () => {
    http.post(LOGIN_URL, {}).subscribe({ error: () => undefined });

    httpMock.expectOne(LOGIN_URL).flush(null, { status: 401, statusText: 'Unauthorized' });

    expect(mockAuthService.logout).not.toHaveBeenCalled();
  });

  it('should not call authService.logout() on 401 for /api/auth/me', () => {
    http.get(ME_URL).subscribe({ error: () => undefined });

    httpMock.expectOne(ME_URL).flush(null, { status: 401, statusText: 'Unauthorized' });

    expect(mockAuthService.logout).not.toHaveBeenCalled();
  });

  it('should not call authService.logout() on 500 errors', () => {
    http.get(PROTECTED_URL).subscribe({ error: () => undefined });

    httpMock.expectOne(PROTECTED_URL).flush(null, { status: 500, statusText: 'Server Error' });

    expect(mockAuthService.logout).not.toHaveBeenCalled();
  });
});
