using BlazorApp.BlazorClient.Services;
using BlazorClient.Tests.Helpers;
using Bunit;
using System.Net;
using Xunit;

namespace BlazorClient.Tests;

public class AuthenticationServiceTests : TestContext
{
    private AuthenticationService BuildService(string json, HttpStatusCode status = HttpStatusCode.OK)
    {
        var handler = new FakeHttpHandler(json, status);
        var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        var authStateProvider = new JwtAuthenticationStateProvider(JSInterop.JSRuntime);
        return new AuthenticationService(http, JSInterop.JSRuntime, authStateProvider);
    }

    [Fact]
    public async Task LoginAsync_ReturnsNull_AndStoresToken_OnSuccess()
    {
        JSInterop.SetupVoid("sessionStorage.setItem", _ => true).SetVoidResult();
        var service = BuildService("""{"token":"abc.def.ghi","username":"jane","role":"visitor","expiresAtUtc":"2099-01-01T00:00:00Z"}""");

        var error = await service.LoginAsync("jane", "password123");

        Assert.Null(error);
        JSInterop.VerifyInvoke("sessionStorage.setItem");
    }

    [Fact]
    public async Task LoginAsync_ReturnsErrorMessage_OnInvalidCredentials()
    {
        var service = BuildService("""{"message":"Invalid username or password."}""", HttpStatusCode.Unauthorized);

        var error = await service.LoginAsync("jane", "wrong-password");

        Assert.Equal("Invalid username or password.", error);
    }

    [Fact]
    public async Task RegisterAsync_ReturnsNull_OnSuccess()
    {
        var service = BuildService("""{"username":"jane","role":"visitor"}""", HttpStatusCode.Created);

        var error = await service.RegisterAsync("jane", "jane@example.com", "password123");

        Assert.Null(error);
    }

    [Fact]
    public async Task RegisterAsync_ReturnsErrorMessage_OnDuplicateUsername()
    {
        var service = BuildService("""{"message":"That username is already taken."}""", HttpStatusCode.Conflict);

        var error = await service.RegisterAsync("jane", "jane@example.com", "password123");

        Assert.Equal("That username is already taken.", error);
    }

    [Fact]
    public async Task LogoutAsync_ClearsStoredToken()
    {
        JSInterop.SetupVoid("sessionStorage.removeItem", _ => true).SetVoidResult();
        JSInterop.SetupVoid("sessionStorage.setItem", _ => true).SetVoidResult();
        var service = BuildService("""{"message":"ok"}""", HttpStatusCode.NoContent);

        await service.LogoutAsync();

        JSInterop.VerifyInvoke("sessionStorage.removeItem");
    }
}
