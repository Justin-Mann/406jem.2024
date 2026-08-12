using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using ResumeFunctions.Auth;
using ResumeFunctions.Auth.Dtos;
using ResumeFunctions.Auth.Models;
using ResumeFunctions.Auth.Storage;

namespace ResumeFunctions
{
    /// <summary>
    /// #45's public directory of registered Resume Admins ("resume posters"): lists them by
    /// display name only (GET, anonymous). Read-only, so it deliberately does not depend on
    /// IEmailSender — that lives on <see cref="ResumePosterContactApi"/> instead, so this
    /// endpoint keeps working even if email sending isn't configured (a real production
    /// incident: this class originally held both endpoints on one constructor, so a missing
    /// Email:AcsConnectionString app setting 500'd this read-only listing too, since Azure
    /// Functions resolves one shared instance per class regardless of which method is invoked).
    /// </summary>
    public class ResumePostersApi
    {
        private readonly ILogger<ResumePostersApi> _logger;
        private readonly IUserStore _userStore;

        public ResumePostersApi(ILogger<ResumePostersApi> logger, IUserStore userStore)
        {
            _logger = logger;
            _userStore = userStore;
        }

        [Function("listResumePosters")]
        public async Task<HttpResponseData> List(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "resume-posters")] HttpRequestData req)
        {
            var users = await _userStore.ListAsync();
            var posters = users
                .Where(u => AccountRoles.IsResumePosterRole(u.Role))
                .Select(u => new ResumePosterDto(u.RowKey, EffectiveDisplayName(u)))
                .OrderBy(p => p.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(posters);
            return response;
        }

        private static string EffectiveDisplayName(UserAccountEntity user) =>
            string.IsNullOrWhiteSpace(user.DisplayName) ? user.Username : user.DisplayName;
    }
}
