using System.Net;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using ResumeFunctions.Auth;
using ResumeFunctions.Auth.Dtos;
using ResumeFunctions.Auth.Middleware;
using ResumeFunctions.Auth.Models;
using ResumeFunctions.Auth.Storage;

namespace ResumeFunctions
{
    /// <summary>
    /// Authenticated CRUD over a Resume Admin's own GitHub Activity display settings (#69), plus
    /// the public read side resolved through SiteConfig.PublicProjectsOwnerId — same shape as
    /// ResumeAdminApi/ProjectListingsApi's owner-scoped split. No GitHub API calls happen here;
    /// this is purely storage for what an admin has configured. The actual fetch/display of
    /// GitHub data is #68.
    /// </summary>
    public class GitHubActivitySettingsApi
    {
        public const int DefaultRepoCount = 5;

        private readonly ILogger<GitHubActivitySettingsApi> _logger;
        private readonly IGitHubActivitySettingsStore _settingsStore;
        private readonly ISiteConfigStore _siteConfigStore;

        public GitHubActivitySettingsApi(
            ILogger<GitHubActivitySettingsApi> logger,
            IGitHubActivitySettingsStore settingsStore,
            ISiteConfigStore siteConfigStore)
        {
            _logger = logger;
            _settingsStore = settingsStore;
            _siteConfigStore = siteConfigStore;
        }

        [Function("getMyGitHubActivitySettings")]
        public async Task<HttpResponseData> GetMine(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "github-activity-settings/mine")] HttpRequestData req,
            FunctionContext context)
        {
            var user = context.GetAuthenticatedUser();
            if (user is null || !context.IsInRoleOrHigher(AccountRoles.ResumeAdmin))
            {
                return await Forbidden(req, user);
            }

            var owner = NormalizedUsername(user);
            var settings = await _settingsStore.GetByOwnerAsync(owner);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(ToDto(settings));
            return response;
        }

        [Function("updateMyGitHubActivitySettings")]
        public async Task<HttpResponseData> UpdateMine(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "github-activity-settings/mine")] HttpRequestData req,
            FunctionContext context)
        {
            var user = context.GetAuthenticatedUser();
            if (user is null || !context.IsInRoleOrHigher(AccountRoles.ResumeAdmin))
            {
                return await Forbidden(req, user);
            }

            var payload = await req.ReadFromJsonAsync<UpdateGitHubActivitySettingsRequest>();
            var owner = NormalizedUsername(user);
            var repoCount = payload?.RepoCount is > 0 ? payload.RepoCount.Value : DefaultRepoCount;
            var pinnedRepoNames = payload?.PinnedRepoNames ?? new List<string>();

            var updated = await _settingsStore.SetAsync(
                owner,
                payload?.Enabled ?? false,
                payload?.GitHubUsername,
                repoCount,
                pinnedRepoNames);

            _logger.LogInformation(
                "GitHub Activity settings updated for owner '{Owner}': enabled={Enabled}, username='{Username}'.",
                owner, updated.Enabled, updated.GitHubUsername);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(ToDto(updated));
            return response;
        }

        [Function("getPublicGitHubActivitySettings")]
        public async Task<HttpResponseData> GetPublic(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "github-activity-settings/public")] HttpRequestData req)
        {
            var config = await _siteConfigStore.GetAsync();
            var ownerId = config?.PublicProjectsOwnerId;
            if (string.IsNullOrWhiteSpace(ownerId))
            {
                return req.CreateResponse(HttpStatusCode.NotFound);
            }

            var settings = await _settingsStore.GetByOwnerAsync(ownerId!);
            if (settings is null || !settings.Enabled)
            {
                return req.CreateResponse(HttpStatusCode.NotFound);
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(ToDto(settings));
            return response;
        }

        private static string NormalizedUsername(ClaimsPrincipal user) =>
            (user.Identity?.Name ?? string.Empty).Trim().ToLowerInvariant();

        private static GitHubActivitySettingsDto ToDto(GitHubActivitySettingsEntity? entity)
        {
            if (entity is null)
            {
                return new GitHubActivitySettingsDto(false, null, DefaultRepoCount, Array.Empty<string>());
            }

            IReadOnlyList<string> pinned;
            try
            {
                pinned = JsonSerializer.Deserialize<List<string>>(entity.PinnedRepoNamesJson) ?? new List<string>();
            }
            catch (JsonException)
            {
                pinned = Array.Empty<string>();
            }

            return new GitHubActivitySettingsDto(entity.Enabled, entity.GitHubUsername, entity.RepoCount, pinned);
        }

        private static async Task<HttpResponseData> Forbidden(HttpRequestData req, ClaimsPrincipal? user) =>
            await HttpResponseHelpers.Forbidden(req, user, "Resume Admin role required.");
    }
}
