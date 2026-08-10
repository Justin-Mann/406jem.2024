export interface DecodedToken {
  username: string;
  role: string;
  expiresAtUtc: Date;
}

/**
 * Minimal client-side JWT payload decoder — the server validates the token on every
 * API call, so the client only needs to read the two claims back out for UI state.
 */
export function decodeToken(token: string): DecodedToken | null {
  const parts = token.split('.');
  if (parts.length !== 3) {
    return null;
  }

  try {
    const payload = JSON.parse(base64UrlDecode(parts[1]));
    const username = payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'];
    const role = payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];
    const exp = payload['exp'];

    if (!username || !role || typeof exp !== 'number') {
      return null;
    }

    const expiresAtUtc = new Date(exp * 1000);
    if (expiresAtUtc.getTime() <= Date.now()) {
      return null;
    }

    return { username, role, expiresAtUtc };
  } catch {
    return null;
  }
}

function base64UrlDecode(input: string): string {
  const padded = input.replace(/-/g, '+').replace(/_/g, '/').padEnd(input.length + ((4 - (input.length % 4)) % 4), '=');
  return decodeURIComponent(
    atob(padded)
      .split('')
      .map(c => '%' + c.charCodeAt(0).toString(16).padStart(2, '0'))
      .join('')
  );
}
