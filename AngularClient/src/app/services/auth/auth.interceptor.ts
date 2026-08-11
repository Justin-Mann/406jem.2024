import { HttpInterceptorFn } from '@angular/common/http';

const XSRF_COOKIE_NAME = 'XSRF-TOKEN';
const XSRF_HEADER_NAME = 'X-XSRF-TOKEN';
const MUTATING_METHODS = new Set(['POST', 'PUT', 'PATCH', 'DELETE']);

/**
 * Attaches the session cookie to every API call (withCredentials) and, on state-changing
 * requests, echoes the non-httpOnly XSRF-TOKEN cookie back in a custom header - the
 * double-submit CSRF check the API requires alongside SameSite=Lax (#47). Not read via
 * Angular's built-in HttpClientXsrfModule because that module skips absolute cross-origin
 * URLs by default, and every API call here is cross-origin (angular.406jem.com -> the
 * Functions app's own domain).
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  let cloned = req.clone({ withCredentials: true });

  if (MUTATING_METHODS.has(cloned.method)) {
    const xsrfToken = readCookie(XSRF_COOKIE_NAME);
    if (xsrfToken) {
      cloned = cloned.clone({ setHeaders: { [XSRF_HEADER_NAME]: xsrfToken } });
    }
  }

  return next(cloned);
};

function readCookie(name: string): string | null {
  const match = document.cookie.match(new RegExp(`(?:^|; )${name}=([^;]*)`));
  return match ? decodeURIComponent(match[1]) : null;
}
