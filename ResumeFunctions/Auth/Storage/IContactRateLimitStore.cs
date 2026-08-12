namespace ResumeFunctions.Auth.Storage
{
    /// <summary>
    /// Basic abuse protection for #45's anonymous, email-sending contact-relay endpoint -
    /// same persisted (not in-memory) rate limiting rationale as the login-lockout fields on
    /// <see cref="ResumeFunctions.Auth.Models.UserAccountEntity"/>.
    /// </summary>
    public interface IContactRateLimitStore
    {
        /// <returns>true if this attempt is allowed (and has been recorded), false if the
        /// caller identified by <paramref name="clientKey"/> has exceeded the limit for the
        /// current window.</returns>
        Task<bool> TryRecordAttemptAsync(string clientKey, CancellationToken cancellationToken = default);
    }
}
