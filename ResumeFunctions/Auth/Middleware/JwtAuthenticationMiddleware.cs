using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using ResumeFunctions.Auth.Tokens;

namespace ResumeFunctions.Auth.Middleware
{
    /// <summary>
    /// Runs on every function invocation. If a valid "Authorization: Bearer &lt;jwt&gt;" header
    /// is present, stashes the resulting ClaimsPrincipal in the FunctionContext for endpoints to
    /// read via <see cref="FunctionContextAuthExtensions"/>. A missing or invalid header is not
    /// itself an error here — anonymous endpoints (myResume, resumes, and testimonial listing)
    /// must keep working exactly as before. Endpoints that require a caller to be logged in
    /// check for the principal themselves and return 401/403.
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
                TryAuthenticate(context, request.Headers);
            }

            await next(context);
        }

        /// <summary>Header-parsing/validation logic split out so it's testable without needing
        /// to fake the GetHttpRequestDataAsync() static extension.</summary>
        internal void TryAuthenticate(FunctionContext context, HttpHeadersCollection headers)
        {
            if (!headers.TryGetValues("Authorization", out var values))
            {
                return;
            }

            var header = values.FirstOrDefault();
            if (header is null || !header.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var token = header[BearerPrefix.Length..].Trim();
            var principal = _tokenService.ValidateToken(token);
            if (principal is not null)
            {
                context.Items[ContextItemKey] = principal;
            }
        }
    }
}
