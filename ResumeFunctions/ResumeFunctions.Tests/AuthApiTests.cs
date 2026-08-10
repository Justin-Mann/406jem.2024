using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using ResumeFunctions.Auth;
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
        var tokenService = new JwtAuthTokenService(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Auth:JwtSigningKey"] = "unit-test-signing-key-that-is-long-enough-1234" })
            .Build());
        var identityProvider = new LocalPasswordIdentityProvider(_userStore, _hasher);

        _api = new AuthApi(Substitute.For<ILogger<AuthApi>>(), _userStore, _hasher, identityProvider, tokenService);
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
    public async Task Login_Returns200WithToken_ForCorrectCredentials()
    {
        var (_, registerRequest) = BuildJsonRequest(new { Username = "jane", Email = "jane@example.com", Password = "password123" });
        await _api.Register(registerRequest);

        var (response, loginRequest) = BuildJsonRequest(new { Username = "jane", Password = "password123" });
        var result = await _api.Login(loginRequest);
        var body = await ReadBody<AuthResponseBody>(response);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.False(string.IsNullOrWhiteSpace(body!.Token));
        Assert.Equal("jane", body.Username);
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

    private record AuthResponseBody(string Token, string Username, string Role);
}
