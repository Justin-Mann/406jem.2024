using ResumeFunctions.Auth.Email;

namespace ResumeFunctions.Tests.Helpers;

/// <summary>
/// In-memory <see cref="IEmailSender"/> for tests that need to assert an email was (or wasn't)
/// sent without ever hitting the real ACS API.
/// </summary>
public class FakeEmailSender : IEmailSender
{
    public List<(string To, string Subject, string HtmlBody, string TextBody)> SentMessages { get; } = new();

    public Task SendAsync(string to, string subject, string htmlBody, string textBody, CancellationToken cancellationToken = default)
    {
        SentMessages.Add((to, subject, htmlBody, textBody));
        return Task.CompletedTask;
    }
}
