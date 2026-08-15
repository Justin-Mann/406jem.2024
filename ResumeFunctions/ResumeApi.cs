using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ResumeFunctions.Auth.Models;
using ResumeFunctions.Auth.Storage;
using ResumeFunctions.Models;
using System.Net;
using System.Text.Json;

namespace ResumeFunctions
{
    public class ResumeApi
    {
        private readonly ILogger<ResumeApi> _logger;
        private readonly string _resumeDataPath;
        private readonly ISiteConfigStore? _siteConfigStore;
        private readonly IResumeStore? _resumeStore;
        private readonly IResumeSnapshotStore? _resumeSnapshotStore;
        private readonly IConfiguration? _configuration;

        public ResumeApi(
            ILogger<ResumeApi> logger,
            string? resumeDataPath = null,
            ISiteConfigStore? siteConfigStore = null,
            IResumeStore? resumeStore = null,
            IResumeSnapshotStore? resumeSnapshotStore = null,
            IConfiguration? configuration = null)
        {
            _logger = logger;
            _resumeDataPath = resumeDataPath ?? Path.Combine(AppContext.BaseDirectory, "StaticData", "Resumes", "JustinMann_062024.json");
            _siteConfigStore = siteConfigStore;
            _resumeStore = resumeStore;
            _resumeSnapshotStore = resumeSnapshotStore;
            _configuration = configuration;
        }

        [Function("resumes")]
        public async Task<HttpResponseData> GetAllResumes(
            [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req)
        {
            _logger.LogInformation("Getting all resumes...");
            var (featured, allowStaticFallback) = await TryGetFeaturedResumeAsync();
            var data = featured is not null
                ? new[] { featured }
                : allowStaticFallback
                    ? JsonFileReader.Read<DigitalResumeModel[]>(_resumeDataPath)
                    : Array.Empty<DigitalResumeModel>();
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(data);
            return response;
        }

        [Function("myResume")]
        public async Task<HttpResponseData> GetResume(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "resumes/myresume")] HttpRequestData req)
        {
            _logger.LogInformation("Getting my resume...");
            var (featured, allowStaticFallback) = await TryGetFeaturedResumeAsync();
            var payload = featured
                ?? (allowStaticFallback ? JsonFileReader.Read<DigitalResumeModel[]>(_resumeDataPath)?.FirstOrDefault() : null);
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(payload);
            return response;
        }

        /// <summary>
        /// Resolves the public resume through SiteConfig (#28) — null (with the static-file
        /// fallback allowed) if no SiteConfig row exists yet or no owner is configured, matching
        /// the pre-#28 behavior. Once an owner is resolved, prefers the live Table Storage
        /// lookup; if that can't resolve a featured resume (store unhealthy, or none marked
        /// featured yet), falls back to that owner's durable snapshot (#39). Both the live and
        /// snapshot lookups are individually try/caught so an actual store exception (not just a
        /// null/empty result) falls through the chain the same way a missing row would.
        /// If neither the live store nor the snapshot has anything, the static file is only used
        /// as a last resort for the seeded SuperAdmin (#33) — surfacing it for any other admin
        /// would incorrectly show that admin a different person's resume.
        /// </summary>
        private async Task<(DigitalResumeModel? Resume, bool AllowStaticFallback)> TryGetFeaturedResumeAsync()
        {
            if (_siteConfigStore is null || _resumeStore is null)
            {
                return (null, true);
            }

            var config = await _siteConfigStore.GetAsync();
            var ownerId = config?.PublicResumeOwnerId;
            if (string.IsNullOrWhiteSpace(ownerId))
            {
                return (null, true);
            }

            var featured = await TryFindFeaturedAsync(ownerId!);
            if (featured is not null)
            {
                var live = DeserializeOrNull(featured.PayloadJson);
                if (live is not null)
                {
                    return (live, true);
                }
            }

            if (_resumeSnapshotStore is not null)
            {
                var snapshotJson = await TryGetSnapshotAsync(ownerId!);
                var snapshot = snapshotJson is null ? null : DeserializeOrNull(snapshotJson);
                if (snapshot is not null)
                {
                    return (snapshot, true);
                }
            }

            return (null, IsSuperAdminOwner(ownerId!));
        }

        private bool IsSuperAdminOwner(string ownerId)
        {
            var adminUsername = _configuration?["Auth:AdminUsername"] ?? "admin";
            return string.Equals(ownerId.Trim(), adminUsername.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        private async Task<ResumeEntity?> TryFindFeaturedAsync(string ownerId)
        {
            try
            {
                return await _resumeStore!.FindFeaturedByOwnerAsync(ownerId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Live resume store lookup failed for owner '{Owner}'; falling back to snapshot/static file.", ownerId);
                return null;
            }
        }

        private async Task<string?> TryGetSnapshotAsync(string ownerId)
        {
            try
            {
                return await _resumeSnapshotStore!.GetAsync(ownerId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Fallback snapshot lookup failed for owner '{Owner}'; falling back to static file.", ownerId);
                return null;
            }
        }

        private static DigitalResumeModel? DeserializeOrNull(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            try
            {
                return JsonSerializer.Deserialize<DigitalResumeModel>(json);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        internal static class JsonFileReader
        {
            public static T? Read<T>(string filePath)
            {
                using FileStream stream = File.OpenRead(filePath);
                return JsonSerializer.Deserialize<T>(stream);
            }
        }
    }
}
