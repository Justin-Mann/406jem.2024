import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient, withXhr } from '@angular/common/http';
import { AuthService } from './auth.service';
import { environment } from '../../../environments/environment';

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;

  function createService(): void {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(withXhr()), provideHttpClientTesting()]
    });
    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
  }

  /** The constructor fires GET /api/auth/me to hydrate session state - every test must
   * account for it exactly once before making other assertions. */
  function flushMeAsUnauthenticated(): void {
    httpMock.expectOne(`${environment.apiBaseUrl}/api/auth/me`)
      .flush({ message: 'Not logged in.' }, { status: 401, statusText: 'Unauthorized' });
  }

  afterEach(() => {
    httpMock.verify();
  });

  it('starts unauthenticated while GET /api/auth/me is pending', () => {
    createService();

    expect(service.isAuthenticated()).toBeFalse();
    expect(service.username()).toBeNull();

    flushMeAsUnauthenticated();
  });

  it('hydrates the session from GET /api/auth/me on startup when a cookie session exists', () => {
    createService();

    httpMock.expectOne(`${environment.apiBaseUrl}/api/auth/me`)
      .flush({ username: 'jane', role: 'visitor' });

    expect(service.isAuthenticated()).toBeTrue();
    expect(service.username()).toBe('jane');
  });

  it('login marks the user authenticated without ever receiving a token', () => {
    createService();
    flushMeAsUnauthenticated();

    let error: string | null | undefined;
    service.login({ username: 'jane', password: 'password123' }).subscribe(e => error = e);

    const req = httpMock.expectOne(`${environment.apiBaseUrl}/api/auth/login`);
    expect(req.request.method).toBe('POST');
    req.flush({ username: 'jane', role: 'visitor', expiresAtUtc: new Date().toISOString() });

    expect(error).toBeNull();
    expect(service.isAuthenticated()).toBeTrue();
    expect(service.username()).toBe('jane');
  });

  it('login returns the server error message on failure', () => {
    createService();
    flushMeAsUnauthenticated();

    let error: string | null | undefined;
    service.login({ username: 'jane', password: 'wrong' }).subscribe(e => error = e);

    const req = httpMock.expectOne(`${environment.apiBaseUrl}/api/auth/login`);
    req.flush({ message: 'Invalid username or password.' }, { status: 401, statusText: 'Unauthorized' });

    expect(error).toBe('Invalid username or password.');
    expect(service.isAuthenticated()).toBeFalse();
  });

  it('register returns null on success', () => {
    createService();
    flushMeAsUnauthenticated();

    let error: string | null | undefined;
    service.register({ username: 'jane', email: 'jane@example.com', password: 'password123' }).subscribe(e => error = e);

    const req = httpMock.expectOne(`${environment.apiBaseUrl}/api/auth/register`);
    req.flush({ username: 'jane', role: 'visitor' }, { status: 201, statusText: 'Created' });

    expect(error).toBeNull();
  });

  it('register returns the server error message on conflict', () => {
    createService();
    flushMeAsUnauthenticated();

    let error: string | null | undefined;
    service.register({ username: 'jane', email: 'jane@example.com', password: 'password123' }).subscribe(e => error = e);

    const req = httpMock.expectOne(`${environment.apiBaseUrl}/api/auth/register`);
    req.flush({ message: 'That username is already taken.' }, { status: 409, statusText: 'Conflict' });

    expect(error).toBe('That username is already taken.');
  });

  it('isAdmin reflects the role returned by the server', () => {
    createService();
    flushMeAsUnauthenticated();

    service.login({ username: 'admin', password: 'password123' }).subscribe();
    httpMock.expectOne(`${environment.apiBaseUrl}/api/auth/login`)
      .flush({ username: 'admin', role: 'admin', expiresAtUtc: new Date().toISOString() });

    expect(service.isAdmin()).toBeTrue();
  });

  it('isAdmin is also true for a superadmin (#28 role hierarchy)', () => {
    createService();
    flushMeAsUnauthenticated();

    service.login({ username: 'root', password: 'password123' }).subscribe();
    httpMock.expectOne(`${environment.apiBaseUrl}/api/auth/login`)
      .flush({ username: 'root', role: 'superadmin', expiresAtUtc: new Date().toISOString() });

    expect(service.isAdmin()).toBeTrue();
  });

  it('logout clears the session and notifies the server', () => {
    createService();
    flushMeAsUnauthenticated();

    service.login({ username: 'jane', password: 'password123' }).subscribe();
    httpMock.expectOne(`${environment.apiBaseUrl}/api/auth/login`)
      .flush({ username: 'jane', role: 'visitor', expiresAtUtc: new Date().toISOString() });

    service.logout();

    httpMock.expectOne(`${environment.apiBaseUrl}/api/auth/logout`).flush(null);

    expect(service.isAuthenticated()).toBeFalse();
  });

  it('logout clears the session even if the server call fails', () => {
    createService();
    flushMeAsUnauthenticated();

    service.login({ username: 'jane', password: 'password123' }).subscribe();
    httpMock.expectOne(`${environment.apiBaseUrl}/api/auth/login`)
      .flush({ username: 'jane', role: 'visitor', expiresAtUtc: new Date().toISOString() });

    service.logout();

    httpMock.expectOne(`${environment.apiBaseUrl}/api/auth/logout`)
      .flush({ message: 'error' }, { status: 500, statusText: 'Server Error' });

    expect(service.isAuthenticated()).toBeFalse();
  });
});
