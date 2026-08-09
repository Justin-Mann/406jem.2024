import { TestBed } from '@angular/core/testing';
import { HttpClient, provideHttpClient, withInterceptors, withXhr } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { authInterceptor } from './auth.interceptor';
import { AuthService } from './auth.service';

describe('authInterceptor', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;
  let authServiceSpy: jasmine.SpyObj<AuthService>;

  beforeEach(() => {
    authServiceSpy = jasmine.createSpyObj('AuthService', ['getToken']);

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withXhr(), withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        { provide: AuthService, useValue: authServiceSpy }
      ]
    });

    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('adds an Authorization header when a token is present', () => {
    authServiceSpy.getToken.and.returnValue('the-token');

    http.get('/api/testimonials').subscribe();

    const req = httpMock.expectOne('/api/testimonials');
    expect(req.request.headers.get('Authorization')).toBe('Bearer the-token');
    req.flush([]);
  });

  it('does not add an Authorization header when there is no token', () => {
    authServiceSpy.getToken.and.returnValue(null);

    http.get('/api/testimonials').subscribe();

    const req = httpMock.expectOne('/api/testimonials');
    expect(req.request.headers.has('Authorization')).toBeFalse();
    req.flush([]);
  });
});
