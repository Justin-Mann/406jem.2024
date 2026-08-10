namespace ResumeFunctions.Auth.Identity
{
    public enum AuthenticationOutcome
    {
        Success,
        InvalidCredentials,
        LockedOut,
    }

    public record AuthenticationResult(AuthenticationOutcome Outcome, string? Username = null, string? Role = null)
    {
        public static AuthenticationResult Success(string username, string role) =>
            new(AuthenticationOutcome.Success, username, role);

        public static readonly AuthenticationResult InvalidCredentials = new(AuthenticationOutcome.InvalidCredentials);

        public static readonly AuthenticationResult LockedOut = new(AuthenticationOutcome.LockedOut);
    }
}
