import { HttpClient } from '@angular/common/http';
import { Injectable, inject, signal, computed } from '@angular/core';
import { catchError, map, Observable, of, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthResponse, ErrorResponse, LoginRequest, RegisterRequest } from '../../interfaces/auth.interface';
import { decodeToken } from './jwt.util';

const TOKEN_STORAGE_KEY = 'authToken';

interface AuthUser {
  username: string;
  role: string;
}

/**
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

  private currentUser = signal<AuthUser | null>(this.restoreSession());

  readonly username = computed(() => this.currentUser()?.username ?? null);
  readonly isAuthenticated = computed(() => this.currentUser() !== null);
  readonly isAdmin = computed(() => this.currentUser()?.role === 'admin');

  private restoreSession(): AuthUser | null {
    const token = sessionStorage.getItem(TOKEN_STORAGE_KEY);
    if (!token) {
      return null;
    }
    const decoded = decodeToken(token);
    if (!decoded) {
      sessionStorage.removeItem(TOKEN_STORAGE_KEY);
      return null;
    }
    return { username: decoded.username, role: decoded.role };
  }

  getToken(): string | null {
    return sessionStorage.getItem(TOKEN_STORAGE_KEY);
  }

  register(request: RegisterRequest): Observable<string | null> {
    return this.http.post(`${this.apiBaseUrl}/api/auth/register`, request).pipe(
      map(() => null),
      catchError(err => of(this.extractError(err)))
    );
  }

  login(request: LoginRequest): Observable<string | null> {
    return this.http.post<AuthResponse>(`${this.apiBaseUrl}/api/auth/login`, request).pipe(
      tap(response => this.setSession(response)),
      map(() => null),
      catchError(err => of(this.extractError(err)))
    );
  }

  logout(): void {
    this.clearSession();
    this.http.post(`${this.apiBaseUrl}/api/auth/logout`, null).subscribe({ error: () => {} });
  }

  private setSession(response: AuthResponse): void {
    // sessionStorage (not localStorage) so the token clears when the tab closes, shrinking
    // the exposure window on a shared machine. The 2-hour JWT expiry bounds it further.
    sessionStorage.setItem(TOKEN_STORAGE_KEY, response.token);
    this.currentUser.set({ username: response.username, role: response.role });
  }

  private clearSession(): void {
    sessionStorage.removeItem(TOKEN_STORAGE_KEY);
    this.currentUser.set(null);
  }

  private extractError(err: any): string {
    const body: ErrorResponse | undefined = err?.error;
    return body?.message || `Request failed (${err?.status ?? 'unknown'}).`;
  }
}
