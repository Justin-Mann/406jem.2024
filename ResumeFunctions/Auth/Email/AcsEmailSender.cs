using Azure;
using Azure.Communication.Email;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ResumeFunctions.Auth.Email
{
    /// <summary>
    /// Sends email via Azure Communication Services Email — stays within the existing Azure
    /// stack (no new SaaS vendor/account to provision) and has a workable free tier for this
    /// site's volume.
    /// </summary>
    public class AcsEmailSender : IEmailSender
    {
        private readonly EmailClient _client;
        private readonly string _senderAddress;
        private readonly ILogger<AcsEmailSender> _logger;

        public AcsEmailSender(IConfiguration configuration, ILogger<AcsEmailSender> logger)
        {
            var connectionString = configuration["Email:AcsConnectionString"];
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "The 'Email:AcsConnectionString' app setting is missing. Set it (as an Azure " +
                    "Functions app setting / Key Vault reference in production, or in " +
                    "local.settings.json for local dev) before starting the host.");
            }

            var senderAddress = configuration["Email:SenderAddress"];
            if (string.IsNullOrWhiteSpace(senderAddress))
            {
                throw new InvalidOperationException(
                    "The 'Email:SenderAddress' app setting is missing. It must be a sender address " +
                    "verified on the ACS Email domain.");
            }

            _senderAddress = senderAddress;
            _client = new EmailClient(connectionString);
            _logger = logger;
        }

        public async Task SendAsync(string to, string subject, string htmlBody, string textBody, CancellationToken cancellationToken = default)
        {
            var content = new EmailContent(subject)
            {
                Html = htmlBody,
                PlainText = textBody,
            };
            var message = new EmailMessage(_senderAddress, to, content);

            _logger.LogInformation("Sending email to {Recipient} with subject '{Subject}'.", to, subject);

            // Started, not Completed: we don't need to block a request on ACS finishing
            // delivery, only on it accepting the send — matches this being a fire-and-log
            // transactional sender, not a delivery-tracking system.
            var operation = await _client.SendAsync(WaitUntil.Started, message, cancellationToken);
            _logger.LogInformation("ACS accepted email send to {Recipient}, operation id '{OperationId}'.", to, operation.Id);
        }
    }
}
