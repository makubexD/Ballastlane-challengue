import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { AuthService } from './auth.service';

const VALID_EMAIL = 'user@example.com';
const VALID_PASSWORD = 'SecurePass1';
const MOCK_TOKEN = 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.mock';
const MOCK_USER_ID = '11111111-1111-1111-1111-111111111111';
const API_BASE = 'http://localhost:5000';

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    localStorage.clear();

    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [provideRouter([])]
    });

    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
  });

  it('should set isAuthenticated to true when login succeeds', () => {
    const loginRequest = { email: VALID_EMAIL, password: VALID_PASSWORD };
    const mockResponse = { token: MOCK_TOKEN, expiresAt: '2026-12-31T00:00:00Z', userId: MOCK_USER_ID };

    service.login(loginRequest).subscribe();
    const req = httpMock.expectOne(`${API_BASE}/api/auth/login`);
    req.flush(mockResponse);

    expect(service.isAuthenticated()).toBe(true);
    expect(localStorage.getItem('auth_token')).toBe(MOCK_TOKEN);
  });

  it('should set isAuthenticated to false when logout is called', () => {
    localStorage.setItem('auth_token', MOCK_TOKEN);

    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [provideRouter([])]
    });
    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);

    service.logout();

    expect(service.isAuthenticated()).toBe(false);
    expect(localStorage.getItem('auth_token')).toBeNull();
  });

  it('should include token in Authorization header via interceptor', () => {
    localStorage.setItem('auth_token', MOCK_TOKEN);

    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [provideRouter([])]
    });
    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);

    expect(localStorage.getItem('auth_token')).toBe(MOCK_TOKEN);
    expect(service.token()).toBe(MOCK_TOKEN);
  });

  it('should call POST /api/auth/register with correct body', () => {
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

  it('should return user profile from GET /api/auth/me', () => {
    const mockProfile = { id: MOCK_USER_ID, email: VALID_EMAIL, createdAt: '2026-01-01T00:00:00Z' };

    service.getCurrentUser().subscribe(profile => {
      expect(profile).toEqual(mockProfile);
    });

    const req = httpMock.expectOne(`${API_BASE}/api/auth/me`);

    expect(req.request.method).toBe('GET');

    req.flush(mockProfile);
  });

  it('should remain unauthenticated when no token in storage', () => {
    localStorage.clear();

    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [provideRouter([])]
    });
    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);

    expect(service.isAuthenticated()).toBe(false);
  });
});
