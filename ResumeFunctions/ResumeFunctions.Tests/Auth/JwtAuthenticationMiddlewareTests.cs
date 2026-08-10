using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using ResumeFunctions.Auth;
using ResumeFunctions.Auth.Middleware;
using ResumeFunctions.Auth.Tokens;
using ResumeFunctions.Tests.Helpers;
using Xunit;

namespace ResumeFunctions.Tests.Auth;

public class JwtAuthenticationMiddlewareTests
{
    private static JwtAuthTokenService BuildTokenService() =>
        new(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Auth:JwtSigningKey"] = "unit-test-signing-key-that-is-long-enough-1234" })
            .Build());

    [Fact]
    public void TryAuthenticate_PopulatesAuthenticatedUser_ForValidBearerToken()
    {
        var tokenService = BuildTokenService();
        var issued = tokenService.IssueToken("jane", AccountRoles.ResumeAdmin);
        var middleware = new JwtAuthenticationMiddleware(tokenService);
        var context = TestFunctionContextFactory.Create();
        var headers = new HttpHeadersCollection();
        headers.Add("Authorization", $"Bearer {issued.Token}");

        middleware.TryAuthenticate(context, headers);

        var user = context.GetAuthenticatedUser();
        Assert.NotNull(user);
        Assert.Equal("jane", user!.Identity!.Name);
        Assert.True(user.IsInRole(AccountRoles.ResumeAdmin));
    }

    [Fact]
    public void TryAuthenticate_LeavesUserUnset_WhenNoAuthorizationHeader()
    {
        var middleware = new JwtAuthenticationMiddleware(BuildTokenService());
        var context = TestFunctionContextFactory.Create();

        middleware.TryAuthenticate(context, new HttpHeadersCollection());

        Assert.Null(context.GetAuthenticatedUser());
    }

    [Fact]
    public void TryAuthenticate_LeavesUserUnset_ForInvalidToken()
    {
        var middleware = new JwtAuthenticationMiddleware(BuildTokenService());
        var context = TestFunctionContextFactory.Create();
        var headers = new HttpHeadersCollection();
        headers.Add("Authorization", "Bearer not-a-real-token");

        middleware.TryAuthenticate(context, headers);

        Assert.Null(context.GetAuthenticatedUser());
    }

    [Fact]
    public void TryAuthenticate_LeavesUserUnset_ForNonBearerAuthorizationHeader()
    {
        var middleware = new JwtAuthenticationMiddleware(BuildTokenService());
        var context = TestFunctionContextFactory.Create();
        var headers = new HttpHeadersCollection();
        headers.Add("Authorization", "Basic dXNlcjpwYXNz");

        middleware.TryAuthenticate(context, headers);

        Assert.Null(context.GetAuthenticatedUser());
    }
}
