import { decodeToken } from './jwt.util';

function base64UrlEncode(json: object): string {
  const base64 = btoa(JSON.stringify(json));
  return base64.replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
}

function buildToken(payload: object): string {
  return `header.${base64UrlEncode(payload)}.signature`;
}

const NAME_CLAIM = 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name';
const ROLE_CLAIM = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role';

describe('decodeToken', () => {
  it('extracts username, role, and expiry from a valid token', () => {
    const exp = Math.floor(Date.now() / 1000) + 3600;
    const token = buildToken({ [NAME_CLAIM]: 'jane', [ROLE_CLAIM]: 'admin', exp });

    const decoded = decodeToken(token);

    expect(decoded).not.toBeNull();
    expect(decoded?.username).toBe('jane');
    expect(decoded?.role).toBe('admin');
  });

  it('returns null for an expired token', () => {
    const exp = Math.floor(Date.now() / 1000) - 3600;
    const token = buildToken({ [NAME_CLAIM]: 'jane', [ROLE_CLAIM]: 'visitor', exp });

    expect(decodeToken(token)).toBeNull();
  });

  it('returns null for a malformed token', () => {
    expect(decodeToken('not-a-jwt')).toBeNull();
  });

  it('returns null when required claims are missing', () => {
    const token = buildToken({ exp: Math.floor(Date.now() / 1000) + 3600 });

    expect(decodeToken(token)).toBeNull();
  });
});
