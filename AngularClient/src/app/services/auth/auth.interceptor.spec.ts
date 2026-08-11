import { TestBed } from '@angular/core/testing';
import { HttpClient, provideHttpClient, withInterceptors, withXhr } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { authInterceptor } from './auth.interceptor';

function setXsrfCookie(value: string): void {
  document.cookie = `XSRF-TOKEN=${value}; path=/`;
}

function clearXsrfCookie(): void {
  document.cookie = 'XSRF-TOKEN=; path=/; expires=Thu, 01 Jan 1970 00:00:00 GMT';
}

describe('authInterceptor', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    clearXsrfCookie();

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withXhr(), withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
      ]
    });

    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    clearXsrfCookie();
  });

  it('sends every request withCredentials so the session cookie is attached', () => {
    http.get('/api/testimonials').subscribe();

    const req = httpMock.expectOne('/api/testimonials');
    expect(req.request.withCredentials).toBeTrue();
    req.flush([]);
  });

  it('adds the X-XSRF-TOKEN header on a mutating request when the cookie is present', () => {
    setXsrfCookie('the-csrf-token');

    http.post('/api/testimonials', { message: 'hi' }).subscribe();

    const req = httpMock.expectOne('/api/testimonials');
    expect(req.request.headers.get('X-XSRF-TOKEN')).toBe('the-csrf-token');
    req.flush({});
  });

  it('does not add the X-XSRF-TOKEN header on a GET request', () => {
    setXsrfCookie('the-csrf-token');

    http.get('/api/testimonials').subscribe();

    const req = httpMock.expectOne('/api/testimonials');
    expect(req.request.headers.has('X-XSRF-TOKEN')).toBeFalse();
    req.flush([]);
  });

  it('does not add the X-XSRF-TOKEN header on a mutating request when there is no cookie', () => {
    http.post('/api/testimonials', { message: 'hi' }).subscribe();

    const req = httpMock.expectOne('/api/testimonials');
    expect(req.request.headers.has('X-XSRF-TOKEN')).toBeFalse();
    req.flush({});
  });

  it('adds the X-XSRF-TOKEN header on a DELETE request when the cookie is present', () => {
    setXsrfCookie('the-csrf-token');

    http.delete('/api/testimonials/1').subscribe();

    const req = httpMock.expectOne('/api/testimonials/1');
    expect(req.request.headers.get('X-XSRF-TOKEN')).toBe('the-csrf-token');
    req.flush({});
  });
});
