import { HttpClient } from '@angular/common/http';
import { Injectable, inject, signal, computed } from '@angular/core';
import { catchError, map, Observable, of, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthResponse, ErrorResponse, LoginRequest, MeResponse, RegisterRequest } from '../../interfaces/auth.interface';

interface AuthUser {
  username: string;
  role: string;
}

/**
 * Session lives in an httpOnly, Domain=406jem.com cookie set by the API (#47), shared by every
 * browser-based client on the site - not in sessionStorage/a JS-readable token. The cookie is
 * deliberately unreadable from JS, so this service hydrates "am I logged in, as whom" by asking
 * GET /api/auth/me on startup rather than decoding a stored token.
 *
 * Phase 1 only talks to the in-app username/password endpoints. A future Microsoft Entra ID
 * phase can add a parallel sign-in path (e.g. via MSAL) that ends by calling the same
 * setSession()/clearSession() seam, without the rest of the app needing to change.
 */
@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private http = inject(HttpClient);
  private readonly apiBaseUrl = environment.apiBaseUrl;

  private currentUser = signal<AuthUser | null>(null);

  readonly username = computed(() => this.currentUser()?.username ?? null);
  readonly isAuthenticated = computed(() => this.currentUser() !== null);
  // SuperAdmin (#28) implies ResumeAdmin's "admin" permissions, mirroring the backend's
  // FunctionContextAuthExtensions.IsInRoleOrHigher.
  readonly isAdmin = computed(() => {
    const role = this.currentUser()?.role;
    return role === 'admin' || role === 'superadmin';
  });

  constructor() {
    this.http.get<MeResponse>(`${this.apiBaseUrl}/api/auth/me`).pipe(
      catchError(() => of(null))
    ).subscribe(response => {
      if (response) {
        this.currentUser.set({ username: response.username, role: response.role });
      }
    });
  }

  register(request: RegisterRequest): Observable<string | null> {
    return this.http.post(`${this.apiBaseUrl}/api/auth/register`, request).pipe(
      map(() => null),
      catchError(err => of(this.extractError(err)))
    );
  }

  login(request: LoginRequest): Observable<string | null> {
    return this.http.post<AuthResponse>(`${this.apiBaseUrl}/api/auth/login`, request).pipe(
      tap(response => this.currentUser.set({ username: response.username, role: response.role })),
      map(() => null),
      catchError(err => of(this.extractError(err)))
    );
  }

  logout(): void {
    this.http.post(`${this.apiBaseUrl}/api/auth/logout`, null).subscribe({
      next: () => this.currentUser.set(null),
      error: () => this.currentUser.set(null),
    });
  }

  private extractError(err: any): string {
    const body: ErrorResponse | undefined = err?.error;
    return body?.message || `Request failed (${err?.status ?? 'unknown'}).`;
  }
}
