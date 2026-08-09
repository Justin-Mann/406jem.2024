using System.Security.Claims;

namespace ResumeFunctions.Auth.Tokens
{
    public record IssuedToken(string Token, DateTimeOffset ExpiresAtUtc);

    public interface IAuthTokenService
    {
        IssuedToken IssueToken(string username, string role);

        ClaimsPrincipal? ValidateToken(string token);
    }
}
