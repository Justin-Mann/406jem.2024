using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace ResumeFunctions.Auth.Tokens
{
    /// <summary>
    /// Issues and validates short-lived JWTs. Logout is stateless (client discards the token) —
    /// the short expiry bounds exposure if a token leaks. A future Entra ID phase can keep this
    /// service as-is and only swap the <see cref="Identity.IIdentityProvider"/> that decides
    /// *whether* to issue one.
    /// </summary>
    public class JwtAuthTokenService : IAuthTokenService
    {
        private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(2);
        private const string Issuer = "406jem-resumefunctions";
        private const string Audience = "406jem-clients";

        private readonly SymmetricSecurityKey _signingKey;
        private readonly SigningCredentials _signingCredentials;
        private readonly JwtSecurityTokenHandler _handler = new();

        public JwtAuthTokenService(IConfiguration configuration)
        {
            var signingKeySetting = configuration["Auth:JwtSigningKey"];
            if (string.IsNullOrWhiteSpace(signingKeySetting) || Encoding.UTF8.GetByteCount(signingKeySetting) < 32)
            {
                throw new InvalidOperationException(
                    "The 'Auth:JwtSigningKey' app setting is missing or shorter than 32 bytes. " +
                    "Set it (as an Azure Functions app setting / Key Vault reference in production, " +
                    "or in local.settings.json for local dev) before starting the host.");
            }

            _signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKeySetting));
            _signingCredentials = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256);
        }

        public IssuedToken IssueToken(string username, string role)
        {
            var now = DateTimeOffset.UtcNow;
            var expiresAt = now.Add(TokenLifetime);

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Role, role),
            };

            var token = new JwtSecurityToken(
                issuer: Issuer,
                audience: Audience,
                claims: claims,
                notBefore: now.UtcDateTime,
                expires: expiresAt.UtcDateTime,
                signingCredentials: _signingCredentials);

            return new IssuedToken(_handler.WriteToken(token), expiresAt);
        }

        public ClaimsPrincipal? ValidateToken(string token)
        {
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = Issuer,
                ValidateAudience = true,
                ValidAudience = Audience,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = _signingKey,
                ClockSkew = TimeSpan.FromSeconds(30),
            };

            try
            {
                var principal = _handler.ValidateToken(token, validationParameters, out _);
                return principal;
            }
            catch (SecurityTokenException)
            {
                return null;
            }
            catch (ArgumentException)
            {
                return null;
            }
        }
    }
}
