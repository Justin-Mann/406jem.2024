namespace BlazorApp.Models
{
    /// <summary>No token field - the JWT lives only in the httpOnly session cookie (#47),
    /// never in a response body a script could read.</summary>
    public class AuthResponse
    {
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public DateTimeOffset ExpiresAtUtc { get; set; }
    }

    /// <summary>Body of GET /api/auth/me, used to hydrate "am I logged in, as whom" on app
    /// startup since the session cookie itself is deliberately unreadable from JS.</summary>
    public class MeResponse
    {
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }

    public class ErrorResponse
    {
        public string Message { get; set; } = string.Empty;
    }

    public class TestimonialItem
    {
        public string Id { get; set; } = string.Empty;
        public string AuthorUsername { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTimeOffset CreatedAtUtc { get; set; }
    }
}
