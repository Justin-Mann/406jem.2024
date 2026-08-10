using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace BlazorApp.BlazorClient.Services
{
    /// <summary>
    /// Minimal client-side JWT payload decoder — deliberately not pulling the full
    /// System.IdentityModel.Tokens.Jwt server library into the WASM bundle just to read
    /// two claims out of a token the server already validates on every API call.
    /// </summary>
    public static class JwtClaimsParser
    {
        public static ClaimsPrincipal? ParseClaimsPrincipal(string jwt)
        {
            var payload = ParsePayload(jwt);
            if (payload is null)
            {
                return null;
            }

            if (payload.TryGetValue("exp", out var exp) && exp.ValueKind == JsonValueKind.Number)
            {
                var expiresAtUtc = DateTimeOffset.FromUnixTimeSeconds(exp.GetInt64());
                if (expiresAtUtc <= DateTimeOffset.UtcNow)
                {
                    return null;
                }
            }

            var claims = payload.Select(kv => new Claim(kv.Key, kv.Value.ToString() ?? string.Empty));
            var identity = new ClaimsIdentity(claims, authenticationType: "jwt", nameType: ClaimTypes.Name, roleType: ClaimTypes.Role);
            return new ClaimsPrincipal(identity);
        }

        private static Dictionary<string, JsonElement>? ParsePayload(string jwt)
        {
            var parts = jwt.Split('.');
            if (parts.Length != 3)
            {
                return null;
            }

            try
            {
                var payloadJson = Encoding.UTF8.GetString(Base64UrlDecode(parts[1]));
                return JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(payloadJson);
            }
            catch (Exception ex) when (ex is FormatException or JsonException)
            {
                return null;
            }
        }

        private static byte[] Base64UrlDecode(string input)
        {
            var padded = input.Replace('-', '+').Replace('_', '/');
            padded = (padded.Length % 4) switch
            {
                2 => padded + "==",
                3 => padded + "=",
                _ => padded,
            };
            return Convert.FromBase64String(padded);
        }
    }
}
