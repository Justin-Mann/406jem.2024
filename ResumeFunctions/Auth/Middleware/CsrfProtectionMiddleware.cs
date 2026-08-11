using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using ResumeFunctions.Auth.Cookies;
using ResumeFunctions.Auth.Dtos;

namespace ResumeFunctions.Auth.Middleware
{
    /// <summary>
    /// Double-submit-cookie CSRF check, required now that auth moved from a bearer token the
    /// frontend attaches deliberately to an httpOnly cookie the browser attaches automatically
    /// (see #47). SameSite=Lax on the session cookie already blocks cross-site fetch/XHR from
    /// carrying it, but this is the defense-in-depth layer the issue asked for explicitly: any
    /// mutating request that carries the session cookie must also echo the XSRF-TOKEN cookie's
    /// value back in the <see cref="HeaderName"/> header. A cross-site page can make the
    /// browser attach the cookie automatically, but the Same-Origin Policy stops it from
    /// reading the cookie's value to also set the header, so the two can never be made to
    /// match by a forged request.
    /// </summary>
    public class CsrfProtectionMiddleware : IFunctionsWorkerMiddleware
    {
        internal const string HeaderName = "X-XSRF-TOKEN";

        private static readonly HashSet<string> MutatingMethods =
            new(StringComparer.OrdinalIgnoreCase) { "POST", "PUT", "PATCH", "DELETE" };

        // No session cookie exists yet when these are called, so there's nothing to
        // double-submit against - and requiring the header here would make it impossible to
        // ever log in from a fresh browser.
        private static readonly string[] ExemptPaths = { "/api/auth/login", "/api/auth/register" };

        public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
        {
            var request = await context.GetHttpRequestDataAsync();
            if (request is not null && !IsValid(request.Method, request.Url.AbsolutePath, request.Cookies, request.Headers))
            {
                var response = request.CreateResponse(HttpStatusCode.Forbidden);
                await response.WriteAsJsonAsync(new ErrorResponse("Missing or invalid CSRF token."));
                context.GetInvocationResult().Value = response;
                return;
            }

            await next(context);
        }

        /// <summary>Split out from Invoke so it's testable without faking GetHttpRequestDataAsync().</summary>
        internal static bool IsValid(string method, string path, IReadOnlyCollection<IHttpCookie> cookies, HttpHeadersCollection headers)
        {
            if (!MutatingMethods.Contains(method))
            {
                return true;
            }

            if (ExemptPaths.Any(p => path.Equals(p, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            var sessionCookie = cookies.FirstOrDefault(c => c.Name == CookieNames.Auth)?.Value;
            if (string.IsNullOrEmpty(sessionCookie))
            {
                // No ambient cookie session in play (e.g. a future bearer-token-only client) -
                // nothing for a forged cross-site request to ride on.
                return true;
            }

            var xsrfCookie = cookies.FirstOrDefault(c => c.Name == CookieNames.Xsrf)?.Value;
            var xsrfHeader = headers.TryGetValues(HeaderName, out var values) ? values.FirstOrDefault() : null;

            if (string.IsNullOrEmpty(xsrfCookie) || string.IsNullOrEmpty(xsrfHeader))
            {
                return false;
            }

            var cookieBytes = Encoding.UTF8.GetBytes(xsrfCookie);
            var headerBytes = Encoding.UTF8.GetBytes(xsrfHeader);
            return cookieBytes.Length == headerBytes.Length && CryptographicOperations.FixedTimeEquals(cookieBytes, headerBytes);
        }
    }
}
