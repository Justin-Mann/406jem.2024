using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using ResumeFunctions.Auth;
using ResumeFunctions.Auth.Dtos;
using ResumeFunctions.Auth.Email;
using ResumeFunctions.Auth.Middleware;
using ResumeFunctions.Auth.Models;
using ResumeFunctions.Auth.Storage;

namespace ResumeFunctions
{
    /// <summary>
    /// #45's public directory of registered Resume Admins ("resume posters"): lists them by
    /// display name only (GET, anonymous), and relays a visitor's message to one of them by
    /// email (POST, anonymous) without ever exposing the admin's real address to the browser.
    /// Distinct from ResumeAdminApi, which is the authenticated CRUD surface for a Resume
    /// Admin's own resumes.
    /// </summary>
    public class ResumePostersApi
    {
        private const int MaxMessageLength = 2000;
        private const int MaxEmailLength = 254;

        private const string HtmlTemplate =
            "<p>You have a new message from a visitor to 406jem.com.</p>" +
            "<p><strong>Visitor's reply-to email:</strong> {{replyToEmail}}</p>" +
            "<p><strong>Message:</strong></p><p>{{message}}</p>" +
            "<p>Reply to the visitor directly at the address above - this relay does not forward further replies.</p>";

        private const string TextTemplate =
            "You have a new message from a visitor to 406jem.com.\n\n" +
            "Visitor's reply-to email: {{replyToEmail}}\n\n" +
            "Message:\n{{message}}\n\n" +
            "Reply to the visitor directly at the address above - this relay does not forward further replies.";

        private readonly ILogger<ResumePostersApi> _logger;
        private readonly IUserStore _userStore;
        private readonly IEmailSender _emailSender;
        private readonly IContactRateLimitStore _rateLimitStore;

        public ResumePostersApi(
            ILogger<ResumePostersApi> logger,
            IUserStore userStore,
            IEmailSender emailSender,
            IContactRateLimitStore rateLimitStore)
        {
            _logger = logger;
            _userStore = userStore;
            _emailSender = emailSender;
            _rateLimitStore = rateLimitStore;
        }

        [Function("listResumePosters")]
        public async Task<HttpResponseData> List(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "resume-posters")] HttpRequestData req)
        {
            var users = await _userStore.ListAsync();
            var posters = users
                .Where(u => IsResumePoster(u.Role))
                .Select(u => new ResumePosterDto(u.RowKey, EffectiveDisplayName(u)))
                .OrderBy(p => p.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(posters);
            return response;
        }

        [Function("contactResumePoster")]
        public async Task<HttpResponseData> Contact(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "resume-posters/{id}/contact")] HttpRequestData req,
            string id)
        {
            var poster = await _userStore.FindByUsernameAsync(id);
            if (poster is null || !IsResumePoster(poster.Role))
            {
                return await ErrorResponse(req, HttpStatusCode.NotFound, "Resume poster not found.");
            }

            var payload = await req.ReadFromJsonAsync<ContactPosterRequest>();
            var message = payload?.Message?.Trim();
            var replyToEmail = payload?.ReplyToEmail?.Trim();

            if (string.IsNullOrWhiteSpace(message) || string.IsNullOrWhiteSpace(replyToEmail))
            {
                return await ErrorResponse(req, HttpStatusCode.BadRequest, "Message and reply-to email are required.");
            }

            if (message.Length > MaxMessageLength)
            {
                return await ErrorResponse(req, HttpStatusCode.BadRequest, $"Message must be {MaxMessageLength} characters or fewer.");
            }

            if (!IsPlausibleEmail(replyToEmail))
            {
                return await ErrorResponse(req, HttpStatusCode.BadRequest, "Reply-to email address is not valid.");
            }

            var allowed = await _rateLimitStore.TryRecordAttemptAsync(GetClientKey(req));
            if (!allowed)
            {
                return await ErrorResponse(req, HttpStatusCode.TooManyRequests, "Too many contact requests. Please try again later.");
            }

            var (html, _) = EmailTemplates.Render(
                HtmlTemplate,
                TextTemplate,
                new Dictionary<string, string>
                {
                    ["replyToEmail"] = WebUtility.HtmlEncode(replyToEmail),
                    ["message"] = WebUtility.HtmlEncode(message),
                });
            var (_, text) = EmailTemplates.Render(
                HtmlTemplate,
                TextTemplate,
                new Dictionary<string, string>
                {
                    ["replyToEmail"] = replyToEmail,
                    ["message"] = message,
                });

            await _emailSender.SendAsync(poster.Email, $"New message from {replyToEmail} via 406jem.com", html, text);
            _logger.LogInformation("Relayed a contact-form message to resume poster '{PosterId}'.", poster.Username);

            return req.CreateResponse(HttpStatusCode.Accepted);
        }

        private static bool IsResumePoster(string role) =>
            role == AccountRoles.ResumeAdmin || role == AccountRoles.SuperAdmin;

        private static string EffectiveDisplayName(UserAccountEntity user) =>
            string.IsNullOrWhiteSpace(user.DisplayName) ? user.Username : user.DisplayName;

        private static bool IsPlausibleEmail(string email)
        {
            var atIndex = email.IndexOf('@');
            return email.Length <= MaxEmailLength
                && atIndex > 0
                && atIndex < email.Length - 1
                && email.IndexOf('@', atIndex + 1) < 0;
        }

        /// <summary>Best-effort caller identity for rate limiting. Azure Functions' front end
        /// sets X-Forwarded-For as "clientIp:port" (or a comma-separated chain under further
        /// proxying); falls back to a shared bucket if the header is absent (e.g. local dev),
        /// an acceptable tradeoff for "basic" abuse protection per #45.</summary>
        private static string GetClientKey(HttpRequestData req)
        {
            if (!req.Headers.TryGetValues("X-Forwarded-For", out var values))
            {
                return "unknown";
            }

            var first = values.FirstOrDefault()?.Split(',').FirstOrDefault()?.Trim();
            if (string.IsNullOrEmpty(first))
            {
                return "unknown";
            }

            var isIPv6 = first.Count(c => c == ':') > 1;
            var lastColon = first.LastIndexOf(':');
            return !isIPv6 && lastColon > 0 ? first[..lastColon] : first;
        }

        private static async Task<HttpResponseData> ErrorResponse(HttpRequestData req, HttpStatusCode status, string message) =>
            await HttpResponseHelpers.ErrorResponse(req, status, message);
    }
}
