import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { provideRouter, Router } from '@angular/router';
import { AuthService } from './auth.service';

const VALID_EMAIL = 'user@example.com';
const VALID_PASSWORD = 'SecurePass1';
const MOCK_USER_ID = '11111111-1111-1111-1111-111111111111';
const API_BASE = 'http://localhost:5000';

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([])
      ]
    });

    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('initialize() — should set isAuthenticated to true when /me returns 200', () => {
    const mockProfile = { id: MOCK_USER_ID, email: VALID_EMAIL, createdAt: '2026-01-01T00:00:00Z' };

    service.initialize().subscribe();

    const req = httpMock.expectOne(`${API_BASE}/api/auth/me`);
    expect(req.request.method).toBe('GET');
    expect(req.request.withCredentials).toBe(true);
    req.flush(mockProfile);

    expect(service.isAuthenticated()).toBe(true);
  });

  it('initialize() — should set isAuthenticated to false when /me returns 401', () => {
    service.initialize().subscribe();

    const req = httpMock.expectOne(`${API_BASE}/api/auth/me`);
    req.flush('Unauthorized', { status: 401, statusText: 'Unauthorized' });

    expect(service.isAuthenticated()).toBe(false);
  });

  it('login() — should set isAuthenticated to true when POST /api/auth/login succeeds', () => {
    service.login({ email: VALID_EMAIL, password: VALID_PASSWORD }).subscribe();

    const req = httpMock.expectOne(`${API_BASE}/api/auth/login`);
    expect(req.request.method).toBe('POST');
    expect(req.request.withCredentials).toBe(true);
    req.flush(null);

    expect(service.isAuthenticated()).toBe(true);
  });

  it('logout() — should POST /api/auth/logout and navigate to /login', () => {
    const router = TestBed.inject(Router);
    const navigateSpy = vi.spyOn(router, 'navigate');

    service.logout();

    const req = httpMock.expectOne(`${API_BASE}/api/auth/logout`);
    expect(req.request.method).toBe('POST');
    expect(req.request.withCredentials).toBe(true);
    req.flush(null);

    expect(service.isAuthenticated()).toBe(false);
    expect(navigateSpy).toHaveBeenCalledWith(['/login']);
  });

  it('register() — should POST /api/auth/register with correct body', () => {
    const registerRequest = { email: VALID_EMAIL, password: VALID_PASSWORD };
    const mockResponse = { id: MOCK_USER_ID, email: VALID_EMAIL };

    service.register(registerRequest).subscribe(response => {
      expect(response).toEqual(mockResponse);
    });

    const req = httpMock.expectOne(`${API_BASE}/api/auth/register`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(registerRequest);
    req.flush(mockResponse);
  });

  it('getCurrentUser() — should GET /api/auth/me with credentials', () => {
    const mockProfile = { id: MOCK_USER_ID, email: VALID_EMAIL, createdAt: '2026-01-01T00:00:00Z' };

    service.getCurrentUser().subscribe(profile => {
      expect(profile).toEqual(mockProfile);
    });

    const req = httpMock.expectOne(`${API_BASE}/api/auth/me`);
    expect(req.request.method).toBe('GET');
    expect(req.request.withCredentials).toBe(true);
    req.flush(mockProfile);
  });

  it('isAuthenticated() — should default to false before initialize() is called', () => {
    expect(service.isAuthenticated()).toBe(false);
  });
});
