import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient, withXhr } from '@angular/common/http';
import { AuthService } from './auth.service';
import { environment } from '../../../environments/environment';

const NAME_CLAIM = 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name';
const ROLE_CLAIM = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role';

function base64UrlEncode(json: object): string {
  const base64 = btoa(JSON.stringify(json));
  return base64.replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
}

function buildToken(username: string, role: string, expiresInSeconds = 3600): string {
  const exp = Math.floor(Date.now() / 1000) + expiresInSeconds;
  return `header.${base64UrlEncode({ [NAME_CLAIM]: username, [ROLE_CLAIM]: role, exp })}.signature`;
}

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    sessionStorage.clear();
    TestBed.configureTestingModule({
      providers: [provideHttpClient(withXhr()), provideHttpClientTesting()]
    });
    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    sessionStorage.clear();
  });

  it('starts unauthenticated when there is no stored token', () => {
    expect(service.isAuthenticated()).toBeFalse();
    expect(service.username()).toBeNull();
  });

  it('login stores the token and marks the user authenticated', () => {
    let error: string | null | undefined;
    service.login({ username: 'jane', password: 'password123' }).subscribe(e => error = e);

    const req = httpMock.expectOne(`${environment.apiBaseUrl}/api/auth/login`);
    expect(req.request.method).toBe('POST');
    req.flush({ token: buildToken('jane', 'visitor'), username: 'jane', role: 'visitor', expiresAtUtc: new Date().toISOString() });

    expect(error).toBeNull();
    expect(service.isAuthenticated()).toBeTrue();
    expect(service.username()).toBe('jane');
    expect(sessionStorage.getItem('authToken')).toBeTruthy();
  });

  it('login returns the server error message on failure', () => {
    let error: string | null | undefined;
    service.login({ username: 'jane', password: 'wrong' }).subscribe(e => error = e);

    const req = httpMock.expectOne(`${environment.apiBaseUrl}/api/auth/login`);
    req.flush({ message: 'Invalid username or password.' }, { status: 401, statusText: 'Unauthorized' });

    expect(error).toBe('Invalid username or password.');
    expect(service.isAuthenticated()).toBeFalse();
  });

  it('register returns null on success', () => {
    let error: string | null | undefined;
    service.register({ username: 'jane', email: 'jane@example.com', password: 'password123' }).subscribe(e => error = e);

    const req = httpMock.expectOne(`${environment.apiBaseUrl}/api/auth/register`);
    req.flush({ username: 'jane', role: 'visitor' }, { status: 201, statusText: 'Created' });

    expect(error).toBeNull();
  });

  it('register returns the server error message on conflict', () => {
    let error: string | null | undefined;
    service.register({ username: 'jane', email: 'jane@example.com', password: 'password123' }).subscribe(e => error = e);

    const req = httpMock.expectOne(`${environment.apiBaseUrl}/api/auth/register`);
    req.flush({ message: 'That username is already taken.' }, { status: 409, statusText: 'Conflict' });

    expect(error).toBe('That username is already taken.');
  });

  it('isAdmin reflects the role claim from the token', () => {
    service.login({ username: 'admin', password: 'password123' }).subscribe();
    httpMock.expectOne(`${environment.apiBaseUrl}/api/auth/login`)
      .flush({ token: buildToken('admin', 'admin'), username: 'admin', role: 'admin', expiresAtUtc: new Date().toISOString() });

    expect(service.isAdmin()).toBeTrue();
  });

  it('logout clears the session and notifies the server', () => {
    service.login({ username: 'jane', password: 'password123' }).subscribe();
    httpMock.expectOne(`${environment.apiBaseUrl}/api/auth/login`)
      .flush({ token: buildToken('jane', 'visitor'), username: 'jane', role: 'visitor', expiresAtUtc: new Date().toISOString() });

    service.logout();

    expect(service.isAuthenticated()).toBeFalse();
    expect(sessionStorage.getItem('authToken')).toBeNull();
    httpMock.expectOne(`${environment.apiBaseUrl}/api/auth/logout`).flush(null);
  });
});
