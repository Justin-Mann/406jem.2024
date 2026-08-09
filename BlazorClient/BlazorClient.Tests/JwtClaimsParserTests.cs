using BlazorApp.BlazorClient.Services;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Xunit;

namespace BlazorClient.Tests;

public class JwtClaimsParserTests
{
    private static string BuildToken(object payload)
    {
        var payloadJson = JsonSerializer.Serialize(payload);
        var payloadSegment = Base64UrlEncode(Encoding.UTF8.GetBytes(payloadJson));
        return $"eyJhbGciOiJIUzI1NiJ9.{payloadSegment}.signature";
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');

    [Fact]
    public void ParseClaimsPrincipal_ExtractsNameAndRole()
    {
        var token = BuildToken(new Dictionary<string, object>
        {
            [ClaimTypes.Name] = "jane",
            [ClaimTypes.Role] = "admin",
            ["exp"] = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds(),
        });

        var principal = JwtClaimsParser.ParseClaimsPrincipal(token);

        Assert.NotNull(principal);
        Assert.Equal("jane", principal!.Identity!.Name);
        Assert.True(principal.IsInRole("admin"));
    }

    [Fact]
    public void ParseClaimsPrincipal_ReturnsNull_ForExpiredToken()
    {
        var token = BuildToken(new Dictionary<string, object>
        {
            [ClaimTypes.Name] = "jane",
            ["exp"] = DateTimeOffset.UtcNow.AddHours(-1).ToUnixTimeSeconds(),
        });

        Assert.Null(JwtClaimsParser.ParseClaimsPrincipal(token));
    }

    [Fact]
    public void ParseClaimsPrincipal_ReturnsNull_ForMalformedToken()
    {
        Assert.Null(JwtClaimsParser.ParseClaimsPrincipal("not-a-jwt"));
    }

    [Fact]
    public void ParseClaimsPrincipal_ReturnsNull_ForInvalidBase64Payload()
    {
        Assert.Null(JwtClaimsParser.ParseClaimsPrincipal("header.not!!valid-base64url.signature"));
    }
}
