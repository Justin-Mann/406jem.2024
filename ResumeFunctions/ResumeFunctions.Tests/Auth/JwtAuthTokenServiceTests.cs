using Microsoft.Extensions.Configuration;
using ResumeFunctions.Auth;
using ResumeFunctions.Auth.Tokens;
using Xunit;

namespace ResumeFunctions.Tests.Auth;

public class JwtAuthTokenServiceTests
{
    private static JwtAuthTokenService BuildService(string signingKey = "unit-test-signing-key-that-is-long-enough-1234") =>
        new(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Auth:JwtSigningKey"] = signingKey })
            .Build());

    [Fact]
    public void Constructor_Throws_WhenSigningKeyMissing()
    {
        var config = new ConfigurationBuilder().Build();
        Assert.Throws<InvalidOperationException>(() => new JwtAuthTokenService(config));
    }

    [Fact]
    public void Constructor_Throws_WhenSigningKeyTooShort()
    {
        Assert.Throws<InvalidOperationException>(() => BuildService("too-short"));
    }

    [Fact]
    public void IssueToken_ThenValidateToken_RoundTripsUsernameAndRole()
    {
        var service = BuildService();

        var issued = service.IssueToken("jane", AccountRoles.Admin);
        var principal = service.ValidateToken(issued.Token);

        Assert.NotNull(principal);
        Assert.Equal("jane", principal!.Identity!.Name);
        Assert.True(principal.IsInRole(AccountRoles.Admin));
    }

    [Fact]
    public void ValidateToken_ReturnsNull_ForGarbageToken()
    {
        var service = BuildService();

        Assert.Null(service.ValidateToken("not-a-real-jwt"));
    }

    [Fact]
    public void ValidateToken_ReturnsNull_WhenSignedWithDifferentKey()
    {
        var issuer = BuildService("first-signing-key-that-is-long-enough-123456");
        var validator = BuildService("second-signing-key-that-is-long-enough-123456");

        var issued = issuer.IssueToken("jane", AccountRoles.Visitor);

        Assert.Null(validator.ValidateToken(issued.Token));
    }
}
