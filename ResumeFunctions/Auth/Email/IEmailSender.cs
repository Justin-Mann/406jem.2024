namespace ResumeFunctions.Auth.Email
{
    /// <summary>
    /// Sends transactional (system-generated) email. The only concrete implementation today is
    /// <see cref="AcsEmailSender"/>, but nothing outside this file should assume that — same
    /// provider-seam pattern as <see cref="ResumeFunctions.Auth.Identity.IIdentityProvider"/>, so
    /// a future switch away from Azure Communication Services doesn't touch callers.
    /// </summary>
    public interface IEmailSender
    {
        Task SendAsync(string to, string subject, string htmlBody, string textBody, CancellationToken cancellationToken = default);
    }
}
