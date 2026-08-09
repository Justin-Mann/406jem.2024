namespace ResumeFunctions.Auth.Dtos
{
    public record RegisterRequest(string? Username, string? Email, string? Password);

    public record LoginRequest(string? Username, string? Password);

    public record AuthResponse(string Token, string Username, string Role, DateTimeOffset ExpiresAtUtc);

    public record TestimonialDto(string Id, string AuthorUsername, string Message, DateTimeOffset CreatedAtUtc);

    public record CreateTestimonialRequest(string? Message);

    public record ErrorResponse(string Message);
}
