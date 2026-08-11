using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using ResumeFunctions.Auth;
using ResumeFunctions.Auth.Cookies;
using ResumeFunctions.Auth.Middleware;
using ResumeFunctions.Auth.Tokens;
using ResumeFunctions.Tests.Helpers;
using Xunit;

namespace ResumeFunctions.Tests.Auth;

public class JwtAuthenticationMiddlewareTests
{
    private static readonly IReadOnlyCollection<IHttpCookie> NoCookies = Array.Empty<IHttpCookie>();

    private static JwtAuthTokenService BuildTokenService() =>
        new(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Auth:JwtSigningKey"] = "unit-test-signing-key-that-is-long-enough-1234" })
            .Build());

    [Fact]
    public void TryAuthenticate_PopulatesAuthenticatedUser_ForValidSessionCookie()
    {
        var tokenService = BuildTokenService();
        var issued = tokenService.IssueToken("jane", AccountRoles.ResumeAdmin);
        var middleware = new JwtAuthenticationMiddleware(tokenService);
        var context = TestFunctionContextFactory.Create();
        var cookies = new IHttpCookie[] { new HttpCookie(CookieNames.Auth, issued.Token) };

        middleware.TryAuthenticate(context, new HttpHeadersCollection(), cookies);

        var user = context.GetAuthenticatedUser();
        Assert.NotNull(user);
        Assert.Equal("jane", user!.Identity!.Name);
        Assert.True(user.IsInRole(AccountRoles.ResumeAdmin));
    }

    [Fact]
    public void TryAuthenticate_PopulatesAuthenticatedUser_ForValidBearerToken()
    {
        var tokenService = BuildTokenService();
        var issued = tokenService.IssueToken("jane", AccountRoles.ResumeAdmin);
        var middleware = new JwtAuthenticationMiddleware(tokenService);
        var context = TestFunctionContextFactory.Create();
        var headers = new HttpHeadersCollection();
        headers.Add("Authorization", $"Bearer {issued.Token}");

        middleware.TryAuthenticate(context, headers, NoCookies);

        var user = context.GetAuthenticatedUser();
        Assert.NotNull(user);
        Assert.Equal("jane", user!.Identity!.Name);
        Assert.True(user.IsInRole(AccountRoles.ResumeAdmin));
    }

    [Fact]
    public void TryAuthenticate_PrefersSessionCookie_OverBearerHeader()
    {
        var tokenService = BuildTokenService();
        var cookieIssued = tokenService.IssueToken("jane", AccountRoles.ResumeAdmin);
        var headerIssued = tokenService.IssueToken("otheruser", AccountRoles.Visitor);
        var middleware = new JwtAuthenticationMiddleware(tokenService);
        var context = TestFunctionContextFactory.Create();
        var headers = new HttpHeadersCollection();
        headers.Add("Authorization", $"Bearer {headerIssued.Token}");
        var cookies = new IHttpCookie[] { new HttpCookie(CookieNames.Auth, cookieIssued.Token) };

        middleware.TryAuthenticate(context, headers, cookies);

        Assert.Equal("jane", context.GetAuthenticatedUser()!.Identity!.Name);
    }

    [Fact]
    public void TryAuthenticate_LeavesUserUnset_WhenNoCookieOrAuthorizationHeader()
    {
        var middleware = new JwtAuthenticationMiddleware(BuildTokenService());
        var context = TestFunctionContextFactory.Create();

        middleware.TryAuthenticate(context, new HttpHeadersCollection(), NoCookies);

        Assert.Null(context.GetAuthenticatedUser());
    }

    [Fact]
    public void TryAuthenticate_LeavesUserUnset_ForInvalidToken()
    {
        var middleware = new JwtAuthenticationMiddleware(BuildTokenService());
        var context = TestFunctionContextFactory.Create();
        var headers = new HttpHeadersCollection();
        headers.Add("Authorization", "Bearer not-a-real-token");

        middleware.TryAuthenticate(context, headers, NoCookies);

        Assert.Null(context.GetAuthenticatedUser());
    }

    [Fact]
    public void TryAuthenticate_LeavesUserUnset_ForNonBearerAuthorizationHeader()
    {
        var middleware = new JwtAuthenticationMiddleware(BuildTokenService());
        var context = TestFunctionContextFactory.Create();
        var headers = new HttpHeadersCollection();
        headers.Add("Authorization", "Basic dXNlcjpwYXNz");

        middleware.TryAuthenticate(context, headers, NoCookies);

        Assert.Null(context.GetAuthenticatedUser());
    }
}
