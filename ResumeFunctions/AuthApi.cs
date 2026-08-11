using System.Net;
using System.Security.Claims;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using ResumeFunctions.Auth;
using ResumeFunctions.Auth.Cookies;
using ResumeFunctions.Auth.Dtos;
using ResumeFunctions.Auth.Identity;
using ResumeFunctions.Auth.Middleware;
using ResumeFunctions.Auth.Models;
using ResumeFunctions.Auth.Security;
using ResumeFunctions.Auth.Storage;
using ResumeFunctions.Auth.Tokens;

namespace ResumeFunctions
{
    public class AuthApi
    {
        private const int MinPasswordLength = 8;

        private readonly ILogger<AuthApi> _logger;
        private readonly IUserStore _userStore;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IIdentityProvider _identityProvider;
        private readonly IAuthTokenService _tokenService;
        private readonly AuthCookieService _cookieService;

        public AuthApi(
            ILogger<AuthApi> logger,
            IUserStore userStore,
            IPasswordHasher passwordHasher,
            IIdentityProvider identityProvider,
            IAuthTokenService tokenService,
            AuthCookieService cookieService)
        {
            _logger = logger;
            _userStore = userStore;
            _passwordHasher = passwordHasher;
            _identityProvider = identityProvider;
            _tokenService = tokenService;
            _cookieService = cookieService;
        }

        [Function("register")]
        public async Task<HttpResponseData> Register(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth/register")] HttpRequestData req)
        {
            var payload = await req.ReadFromJsonAsync<RegisterRequest>();
            var username = payload?.Username?.Trim();
            var email = payload?.Email?.Trim();
            var password = payload?.Password;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                return await BadRequest(req, "Username, email, and password are all required.");
            }

            if (password.Length < MinPasswordLength)
            {
                return await BadRequest(req, $"Password must be at least {MinPasswordLength} characters.");
            }

            var user = new UserAccountEntity
            {
                Username = username,
                Email = email,
                PasswordHash = _passwordHasher.Hash(password),
                Role = AccountRoles.Visitor,
                CreatedAtUtc = DateTimeOffset.UtcNow,
            };

            var created = await _userStore.CreateAsync(user);
            if (!created)
            {
                return await ErrorResponse(req, HttpStatusCode.Conflict, "That username is already taken.");
            }

            _logger.LogInformation("Registered new visitor account '{Username}'.", username);
            var response = req.CreateResponse(HttpStatusCode.Created);
            await response.WriteAsJsonAsync(new { username, role = AccountRoles.Visitor });
            return response;
        }

        [Function("login")]
        public async Task<HttpResponseData> Login(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth/login")] HttpRequestData req)
        {
            var payload = await req.ReadFromJsonAsync<LoginRequest>();
            var username = payload?.Username?.Trim();
            var password = payload?.Password;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                return await BadRequest(req, "Username and password are required.");
            }

            var result = await _identityProvider.AuthenticateAsync(username, password);

            switch (result.Outcome)
            {
                case AuthenticationOutcome.LockedOut:
                    return await ErrorResponse(req, HttpStatusCode.TooManyRequests,
                        "Too many failed login attempts. Try again in a few minutes.");

                case AuthenticationOutcome.InvalidCredentials:
                    return await ErrorResponse(req, HttpStatusCode.Unauthorized, "Invalid username or password.");

                case AuthenticationOutcome.Success:
                default:
                    var issued = _tokenService.IssueToken(result.Username!, result.Role!);
                    var xsrfToken = CsrfTokenGenerator.Generate();
                    var response = req.CreateResponse(HttpStatusCode.OK);
                    _cookieService.AppendSessionCookies(response, issued.Token, xsrfToken, issued.ExpiresAtUtc);
                    await response.WriteAsJsonAsync(new AuthResponse(result.Username!, result.Role!, issued.ExpiresAtUtc));
                    return response;
            }
        }

        [Function("logout")]
        public HttpResponseData Logout(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth/logout")] HttpRequestData req)
        {
            // JWTs are stateless and short-lived (2 hours); logout clears the session/XSRF
            // cookies client-side. This endpoint exists as an explicit, documented contract.
            var response = req.CreateResponse(HttpStatusCode.NoContent);
            _cookieService.AppendClearCookies(response);
            return response;
        }

        [Function("me")]
        public async Task<HttpResponseData> Me(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "auth/me")] HttpRequestData req,
            FunctionContext context)
        {
            var user = context.GetAuthenticatedUser();
            if (user is null)
            {
                return await ErrorResponse(req, HttpStatusCode.Unauthorized, "Not logged in.");
            }

            var username = user.Identity?.Name ?? string.Empty;
            var role = user.FindFirst(ClaimTypes.Role)?.Value ?? AccountRoles.Visitor;

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new MeResponse(username, role));
            return response;
        }

        private static async Task<HttpResponseData> BadRequest(HttpRequestData req, string message) =>
            await ErrorResponse(req, HttpStatusCode.BadRequest, message);

        private static async Task<HttpResponseData> ErrorResponse(HttpRequestData req, HttpStatusCode status, string message)
        {
            var response = req.CreateResponse(status);
            await response.WriteAsJsonAsync(new ErrorResponse(message));
            return response;
        }
    }
}
