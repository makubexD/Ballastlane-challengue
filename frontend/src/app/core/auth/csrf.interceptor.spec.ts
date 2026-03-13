import { TestBed } from '@angular/core/testing';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { HttpClient } from '@angular/common/http';
import { csrfInterceptor } from './csrf.interceptor';

const XSRF_TOKEN_VALUE = 'test-csrf-token-abc123';
const TEST_URL = '/api/tasks';

describe('csrfInterceptor', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;

  function setup(cookieValue: string): void {
    Object.defineProperty(document, 'cookie', {
      get: () => cookieValue,
      configurable: true
    });

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([csrfInterceptor])),
        provideHttpClientTesting()
      ]
    });

    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
  }

  afterEach(() => {
    httpMock.verify();
    vi.restoreAllMocks();
  });

  it('should not add X-XSRF-TOKEN header on GET requests', () => {
    setup(`XSRF-TOKEN=${XSRF_TOKEN_VALUE}`);

    http.get(TEST_URL).subscribe();

    const req = httpMock.expectOne(TEST_URL);
    expect(req.request.headers.has('X-XSRF-TOKEN')).toBe(false);
    req.flush(null);
  });

  it('should add X-XSRF-TOKEN header on POST requests when cookie is present', () => {
    setup(`XSRF-TOKEN=${XSRF_TOKEN_VALUE}`);

    http.post(TEST_URL, {}).subscribe();

    const req = httpMock.expectOne(TEST_URL);
    expect(req.request.headers.get('X-XSRF-TOKEN')).toBe(XSRF_TOKEN_VALUE);
    req.flush(null);
  });

  it('should add X-XSRF-TOKEN header on PUT requests when cookie is present', () => {
    setup(`XSRF-TOKEN=${XSRF_TOKEN_VALUE}`);

    http.put(TEST_URL, {}).subscribe();

    const req = httpMock.expectOne(TEST_URL);
    expect(req.request.headers.get('X-XSRF-TOKEN')).toBe(XSRF_TOKEN_VALUE);
    req.flush(null);
  });

  it('should add X-XSRF-TOKEN header on DELETE requests when cookie is present', () => {
    setup(`XSRF-TOKEN=${XSRF_TOKEN_VALUE}`);

    http.delete(TEST_URL).subscribe();

    const req = httpMock.expectOne(TEST_URL);
    expect(req.request.headers.get('X-XSRF-TOKEN')).toBe(XSRF_TOKEN_VALUE);
    req.flush(null);
  });

  it('should not add X-XSRF-TOKEN header when cookie is absent', () => {
    setup('');

    http.post(TEST_URL, {}).subscribe();

    const req = httpMock.expectOne(TEST_URL);
    expect(req.request.headers.has('X-XSRF-TOKEN')).toBe(false);
    req.flush(null);
  });
});
