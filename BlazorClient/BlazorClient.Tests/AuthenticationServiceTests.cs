using BlazorApp.BlazorClient.Services;
using BlazorClient.Tests.Helpers;
using System.Net;
using System.Net.Http;
using Xunit;

namespace BlazorClient.Tests;

public class AuthenticationServiceTests
{
    private static AuthenticationService BuildService(string json, HttpStatusCode status = HttpStatusCode.OK)
    {
        var handler = new FakeHttpHandler(json, status);
        var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        var authStateProvider = new JwtAuthenticationStateProvider();
        return new AuthenticationService(http, authStateProvider);
    }

    private static (AuthenticationService service, JwtAuthenticationStateProvider provider) BuildServiceWithRoutes(RoutedFakeHttpHandler handler)
    {
        var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        var authStateProvider = new JwtAuthenticationStateProvider();
        return (new AuthenticationService(http, authStateProvider), authStateProvider);
    }

    [Fact]
    public async Task LoginAsync_ReturnsNull_AndAuthenticatesUser_OnSuccess()
    {
        var service = BuildService("""{"username":"jane","role":"visitor","expiresAtUtc":"2099-01-01T00:00:00Z"}""");

        var error = await service.LoginAsync("jane", "password123");

        Assert.Null(error);
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
    public async Task LogoutAsync_NotifiesLoggedOut()
    {
        var handler = new RoutedFakeHttpHandler()
            .When(HttpMethod.Get, "api/auth/me", """{"username":"jane","role":"visitor"}""")
            .When(HttpMethod.Post, "api/auth/logout", "{}", HttpStatusCode.NoContent);
        var (service, provider) = BuildServiceWithRoutes(handler);
        await service.InitializeAsync();

        await service.LogoutAsync();

        var state = await provider.GetAuthenticationStateAsync();
        Assert.False(state.User.Identity?.IsAuthenticated ?? false);
    }

    [Fact]
    public async Task InitializeAsync_HydratesAuthenticatedState_WhenMeReturnsUser()
    {
        var handler = new RoutedFakeHttpHandler()
            .When(HttpMethod.Get, "api/auth/me", """{"username":"jane","role":"admin"}""");
        var (service, provider) = BuildServiceWithRoutes(handler);

        await service.InitializeAsync();

        var state = await provider.GetAuthenticationStateAsync();
        Assert.True(state.User.Identity?.IsAuthenticated);
        Assert.Equal("jane", state.User.Identity!.Name);
        Assert.True(state.User.IsInRole("admin"));
    }

    [Fact]
    public async Task InitializeAsync_LeavesAnonymous_WhenMeReturns401()
    {
        var handler = new RoutedFakeHttpHandler()
            .When(HttpMethod.Get, "api/auth/me", """{"message":"Not logged in."}""", HttpStatusCode.Unauthorized);
        var (service, provider) = BuildServiceWithRoutes(handler);

        await service.InitializeAsync();

        var state = await provider.GetAuthenticationStateAsync();
        Assert.False(state.User.Identity?.IsAuthenticated ?? false);
    }
}
