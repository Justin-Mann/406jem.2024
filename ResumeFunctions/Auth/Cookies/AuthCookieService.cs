using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;

namespace ResumeFunctions.Auth.Cookies
{
    /// <summary>
    /// Appends/clears the session (httpOnly JWT) and XSRF-TOKEN (JS-readable, double-submit)
    /// cookies on login/logout responses.
    ///
    /// Domain is configurable via the "Auth:CookieDomain" app setting rather than hardcoded,
    /// because a Set-Cookie Domain attribute is only valid when it's the same domain as, or a
    /// parent of, the host that actually sent the response - browsers silently reject the
    /// whole cookie otherwise. Production sets this to "406jem.com" so the cookie is shared
    /// with 406jem.com, angular.406jem.com, and any future subdomain (the point of #47); local
    /// dev leaves it unset, which yields a host-only cookie that still works fine locally since
    /// cookie storage keys ignore port.
    /// </summary>
    public class AuthCookieService
    {
        private readonly string? _domain;
        private readonly bool _secure;

        public AuthCookieService(IConfiguration configuration)
        {
            var configuredDomain = configuration["Auth:CookieDomain"];
            _domain = string.IsNullOrWhiteSpace(configuredDomain) ? null : configuredDomain;

            // Defaults to true (Secure) - local dev over plain http must explicitly opt out via
            // Auth:CookieSecure=false in local.settings.json, since a Secure cookie is silently
            // dropped by the browser on a non-https origin.
            _secure = !bool.TryParse(configuration["Auth:CookieSecure"], out var configuredSecure) || configuredSecure;
        }

        public void AppendSessionCookies(HttpResponseData response, string jwt, string xsrfToken, DateTimeOffset expiresAtUtc)
        {
            response.Cookies.Append(Build(CookieNames.Auth, jwt, expiresAtUtc, httpOnly: true));
            response.Cookies.Append(Build(CookieNames.Xsrf, xsrfToken, expiresAtUtc, httpOnly: false));
        }

        public void AppendClearCookies(HttpResponseData response)
        {
            var expired = DateTimeOffset.UnixEpoch;
            response.Cookies.Append(Build(CookieNames.Auth, string.Empty, expired, httpOnly: true));
            response.Cookies.Append(Build(CookieNames.Xsrf, string.Empty, expired, httpOnly: false));
        }

        private HttpCookie Build(string name, string value, DateTimeOffset expiresAtUtc, bool httpOnly) =>
            new(name, value)
            {
                Path = "/",
                Expires = expiresAtUtc,
                SameSite = SameSite.Lax,
                Secure = _secure,
                HttpOnly = httpOnly,
                Domain = _domain,
            };
    }
}
