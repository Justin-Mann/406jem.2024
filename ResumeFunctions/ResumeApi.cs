using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
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

        public ResumeApi(
            ILogger<ResumeApi> logger,
            string? resumeDataPath = null,
            ISiteConfigStore? siteConfigStore = null,
            IResumeStore? resumeStore = null,
            IResumeSnapshotStore? resumeSnapshotStore = null)
        {
            _logger = logger;
            _resumeDataPath = resumeDataPath ?? Path.Combine(AppContext.BaseDirectory, "StaticData", "Resumes", "JustinMann_062024.json");
            _siteConfigStore = siteConfigStore;
            _resumeStore = resumeStore;
            _resumeSnapshotStore = resumeSnapshotStore;
        }

        [Function("resumes")]
        public async Task<HttpResponseData> GetAllResumes(
            [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req)
        {
            _logger.LogInformation("Getting all resumes...");
            var featured = await TryGetFeaturedResumeAsync();
            var data = featured is not null
                ? new[] { featured }
                : JsonFileReader.Read<DigitalResumeModel[]>(_resumeDataPath);
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(data);
            return response;
        }

        [Function("myResume")]
        public async Task<HttpResponseData> GetResume(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "resumes/myresume")] HttpRequestData req)
        {
            _logger.LogInformation("Getting my resume...");
            var payload = await TryGetFeaturedResumeAsync()
                ?? JsonFileReader.Read<DigitalResumeModel[]>(_resumeDataPath)?.FirstOrDefault();
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(payload);
            return response;
        }

        /// <summary>
        /// Resolves the public resume through SiteConfig (#28) — null if no SiteConfig row
        /// exists yet or no owner is configured, in which case GetResume and GetAllResumes both
        /// fall back to the pre-#28 static-file behavior. Once an owner is resolved, prefers the
        /// live Table Storage lookup; if that can't resolve a featured resume (store unhealthy,
        /// or none marked featured yet), falls back to that owner's durable snapshot (#39)
        /// before the caller falls back to the static file.
        /// </summary>
        private async Task<DigitalResumeModel?> TryGetFeaturedResumeAsync()
        {
            if (_siteConfigStore is null || _resumeStore is null)
            {
                return null;
            }

            var config = await _siteConfigStore.GetAsync();
            var ownerId = config?.PublicResumeOwnerId;
            if (string.IsNullOrWhiteSpace(ownerId))
            {
                return null;
            }

            var featured = await _resumeStore.FindFeaturedByOwnerAsync(ownerId!);
            if (featured is not null)
            {
                var live = DeserializeOrNull(featured.PayloadJson);
                if (live is not null)
                {
                    return live;
                }
            }

            if (_resumeSnapshotStore is null)
            {
                return null;
            }

            var snapshotJson = await _resumeSnapshotStore.GetAsync(ownerId!);
            return snapshotJson is null ? null : DeserializeOrNull(snapshotJson);
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
