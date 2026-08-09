namespace BlazorApp.Models
{
    public class AuthResponse
    {
        public string Token { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public DateTimeOffset ExpiresAtUtc { get; set; }
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
