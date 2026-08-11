using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using ResumeFunctions.Auth;
using ResumeFunctions.Auth.Cookies;
using ResumeFunctions.Auth.Identity;
using ResumeFunctions.Auth.Security;
using ResumeFunctions.Auth.Tokens;
using ResumeFunctions.Tests.Helpers;
using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;

namespace ResumeFunctions.Tests;

public class AuthApiTests
{
    private readonly FakeUserStore _userStore = new();
    private readonly Pbkdf2PasswordHasher _hasher = new();
    private readonly FunctionContext _functionContext = TestFunctionContextFactory.Create();
    private readonly AuthApi _api;

    public AuthApiTests()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Auth:JwtSigningKey"] = "unit-test-signing-key-that-is-long-enough-1234",
                ["Auth:CookieDomain"] = "406jem.com",
            })
            .Build();
        var tokenService = new JwtAuthTokenService(configuration);
        var identityProvider = new LocalPasswordIdentityProvider(_userStore, _hasher);
        var cookieService = new AuthCookieService(configuration);

        _api = new AuthApi(Substitute.For<ILogger<AuthApi>>(), _userStore, _hasher, identityProvider, tokenService, cookieService);
    }

    private (TestHttpResponseData response, TestHttpRequestData request) BuildJsonRequest(object body)
    {
        var response = new TestHttpResponseData(_functionContext);
        var json = JsonSerializer.Serialize(body);
        var bodyStream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var request = new TestHttpRequestData(_functionContext, response, bodyStream, "POST");
        return (response, request);
    }

    private static async Task<T?> ReadBody<T>(TestHttpResponseData response)
    {
        response.Body.Position = 0;
        return await JsonSerializer.DeserializeAsync<T>(response.Body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    private static async Task<string> RawBody(TestHttpResponseData response)
    {
        response.Body.Position = 0;
        using var reader = new StreamReader(response.Body, leaveOpen: true);
        return await reader.ReadToEndAsync();
    }

    [Fact]
    public async Task Register_Returns201_ForNewVisitor()
    {
        var (response, request) = BuildJsonRequest(new { Username = "jane", Email = "jane@example.com", Password = "password123" });

        var result = await _api.Register(request);

        Assert.Equal(HttpStatusCode.Created, result.StatusCode);
    }

    [Fact]
    public async Task Register_CreatesVisitorRole_NotAdmin()
    {
        var (_, request) = BuildJsonRequest(new { Username = "jane", Email = "jane@example.com", Password = "password123" });

        await _api.Register(request);
        var user = await _userStore.FindByUsernameAsync("jane");

        Assert.Equal(AccountRoles.Visitor, user!.Role);
    }

    [Fact]
    public async Task Register_Returns409_ForDuplicateUsername()
    {
        var (_, first) = BuildJsonRequest(new { Username = "jane", Email = "jane@example.com", Password = "password123" });
        await _api.Register(first);

        var (response, second) = BuildJsonRequest(new { Username = "jane", Email = "other@example.com", Password = "password123" });
        var result = await _api.Register(second);

        Assert.Equal(HttpStatusCode.Conflict, result.StatusCode);
    }

    [Fact]
    public async Task Register_Returns400_ForShortPassword()
    {
        var (response, request) = BuildJsonRequest(new { Username = "jane", Email = "jane@example.com", Password = "short" });

        var result = await _api.Register(request);

        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
    }

    [Fact]
    public async Task Register_Returns400_ForMissingFields()
    {
        var (response, request) = BuildJsonRequest(new { Username = "jane" });

        var result = await _api.Register(request);

        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
    }

    [Fact]
    public async Task Login_Returns200WithoutToken_ForCorrectCredentials()
    {
        var (_, registerRequest) = BuildJsonRequest(new { Username = "jane", Email = "jane@example.com", Password = "password123" });
        await _api.Register(registerRequest);

        var (response, loginRequest) = BuildJsonRequest(new { Username = "jane", Password = "password123" });
        var result = await _api.Login(loginRequest);
        var body = await ReadBody<AuthResponseBody>(response);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal("jane", body!.Username);
        // The JWT must never appear in the response body - only in the httpOnly cookie.
        Assert.DoesNotContain("Token", await RawBody(response), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Login_SetsHttpOnlySessionCookie_AndReadableXsrfCookie()
    {
        var (_, registerRequest) = BuildJsonRequest(new { Username = "jane", Email = "jane@example.com", Password = "password123" });
        await _api.Register(registerRequest);

        var (response, loginRequest) = BuildJsonRequest(new { Username = "jane", Password = "password123" });
        await _api.Login(loginRequest);

        var cookies = ((TestHttpCookies)response.Cookies).Appended;
        var authCookie = Assert.Single(cookies, c => c.Name == CookieNames.Auth);
        var xsrfCookie = Assert.Single(cookies, c => c.Name == CookieNames.Xsrf);

        Assert.True(authCookie.HttpOnly == true);
        Assert.False(string.IsNullOrWhiteSpace(authCookie.Value));
        Assert.True(authCookie.Secure == true);
        Assert.Equal(SameSite.Lax, authCookie.SameSite);
        Assert.Equal("406jem.com", authCookie.Domain);

        Assert.True(xsrfCookie.HttpOnly != true);
        Assert.False(string.IsNullOrWhiteSpace(xsrfCookie.Value));
        Assert.NotEqual(authCookie.Value, xsrfCookie.Value);
    }

    [Fact]
    public async Task Login_Returns401_ForWrongPassword()
    {
        var (_, registerRequest) = BuildJsonRequest(new { Username = "jane", Email = "jane@example.com", Password = "password123" });
        await _api.Register(registerRequest);

        var (response, loginRequest) = BuildJsonRequest(new { Username = "jane", Password = "wrong-password" });
        var result = await _api.Login(loginRequest);

        Assert.Equal(HttpStatusCode.Unauthorized, result.StatusCode);
    }

    [Fact]
    public async Task Login_Returns401_ForUnknownUsername()
    {
        var (response, loginRequest) = BuildJsonRequest(new { Username = "nobody", Password = "password123" });

        var result = await _api.Login(loginRequest);

        Assert.Equal(HttpStatusCode.Unauthorized, result.StatusCode);
    }

    [Fact]
    public async Task Login_Returns429_AfterAccountIsLockedOut()
    {
        var (_, registerRequest) = BuildJsonRequest(new { Username = "jane", Email = "jane@example.com", Password = "password123" });
        await _api.Register(registerRequest);

        for (var i = 0; i < 5; i++)
        {
            var (_, badRequest) = BuildJsonRequest(new { Username = "jane", Password = "wrong-password" });
            await _api.Login(badRequest);
        }

        var (response, request) = BuildJsonRequest(new { Username = "jane", Password = "password123" });
        var result = await _api.Login(request);

        Assert.Equal(HttpStatusCode.TooManyRequests, result.StatusCode);
    }

    [Fact]
    public void Logout_Returns204()
    {
        var response = new TestHttpResponseData(_functionContext);
        var request = new TestHttpRequestData(_functionContext, response, method: "POST");

        var result = _api.Logout(request);

        Assert.Equal(HttpStatusCode.NoContent, result.StatusCode);
    }

    [Fact]
    public void Logout_ClearsSessionAndXsrfCookies()
    {
        var response = new TestHttpResponseData(_functionContext);
        var request = new TestHttpRequestData(_functionContext, response, method: "POST");

        _api.Logout(request);

        var cookies = ((TestHttpCookies)response.Cookies).Appended;
        var authCookie = Assert.Single(cookies, c => c.Name == CookieNames.Auth);
        var xsrfCookie = Assert.Single(cookies, c => c.Name == CookieNames.Xsrf);

        Assert.Equal(string.Empty, authCookie.Value);
        Assert.Equal(string.Empty, xsrfCookie.Value);
        Assert.True(authCookie.Expires < DateTimeOffset.UtcNow);
        Assert.True(xsrfCookie.Expires < DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task Me_Returns401_WhenNotLoggedIn()
    {
        var response = new TestHttpResponseData(_functionContext);
        var request = new TestHttpRequestData(_functionContext, response, method: "GET");

        var result = await _api.Me(request, _functionContext);

        Assert.Equal(HttpStatusCode.Unauthorized, result.StatusCode);
    }

    [Fact]
    public async Task Me_ReturnsCurrentUser_WhenLoggedIn()
    {
        var user = TestFunctionContextFactory.CreateUser("jane", AccountRoles.ResumeAdmin);
        var context = TestFunctionContextFactory.Create(user);
        var response = new TestHttpResponseData(context);
        var request = new TestHttpRequestData(context, response, method: "GET");

        var result = await _api.Me(request, context);
        var body = await ReadBody<MeResponseBody>(response);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal("jane", body!.Username);
        Assert.Equal(AccountRoles.ResumeAdmin, body.Role);
    }

    private record AuthResponseBody(string Username, string Role);

    private record MeResponseBody(string Username, string Role);
}
