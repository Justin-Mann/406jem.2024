using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using ResumeFunctions.Auth.Cookies;
using ResumeFunctions.Auth.Tokens;

namespace ResumeFunctions.Auth.Middleware
{
    /// <summary>
    /// Runs on every function invocation. If a valid JWT is present - either in the httpOnly
    /// session cookie (the browser-client path since #47) or an "Authorization: Bearer &lt;jwt&gt;"
    /// header (kept for any future non-browser/bearer-token client) - stashes the resulting
    /// ClaimsPrincipal in the FunctionContext for endpoints to read via
    /// <see cref="FunctionContextAuthExtensions"/>. A missing or invalid token is not itself an
    /// error here — anonymous endpoints (myResume, resumes, and testimonial listing) must keep
    /// working exactly as before. Endpoints that require a caller to be logged in check for the
    /// principal themselves and return 401/403.
    /// </summary>
    public class JwtAuthenticationMiddleware : IFunctionsWorkerMiddleware
    {
        public const string ContextItemKey = "AuthenticatedUser";
        private const string BearerPrefix = "Bearer ";

        private readonly IAuthTokenService _tokenService;

        public JwtAuthenticationMiddleware(IAuthTokenService tokenService)
        {
            _tokenService = tokenService;
        }

        public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
        {
            var request = await context.GetHttpRequestDataAsync();
            if (request is not null)
            {
                TryAuthenticate(context, request.Headers, request.Cookies);
            }

            await next(context);
        }

        /// <summary>Token-extraction/validation logic split out so it's testable without needing
        /// to fake the GetHttpRequestDataAsync() static extension.</summary>
        internal void TryAuthenticate(FunctionContext context, HttpHeadersCollection headers, IReadOnlyCollection<IHttpCookie> cookies)
        {
            var token = GetCookieToken(cookies) ?? GetBearerToken(headers);
            if (token is null)
            {
                return;
            }

            var principal = _tokenService.ValidateToken(token);
            if (principal is not null)
            {
                context.Items[ContextItemKey] = principal;
            }
        }

        private static string? GetCookieToken(IReadOnlyCollection<IHttpCookie> cookies) =>
            cookies.FirstOrDefault(c => c.Name == CookieNames.Auth)?.Value;

        private static string? GetBearerToken(HttpHeadersCollection headers)
        {
            if (!headers.TryGetValues("Authorization", out var values))
            {
                return null;
            }

            var header = values.FirstOrDefault();
            if (header is null || !header.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return header[BearerPrefix.Length..].Trim();
        }
    }
}
