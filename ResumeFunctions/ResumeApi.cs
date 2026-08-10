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

        public ResumeApi(
            ILogger<ResumeApi> logger,
            string? resumeDataPath = null,
            ISiteConfigStore? siteConfigStore = null,
            IResumeStore? resumeStore = null)
        {
            _logger = logger;
            _resumeDataPath = resumeDataPath ?? Path.Combine(AppContext.BaseDirectory, "StaticData", "Resumes", "JustinMann_062024.json");
            _siteConfigStore = siteConfigStore;
            _resumeStore = resumeStore;
        }

        [Function("resumes")]
        public async Task<HttpResponseData> GetAllResumes(
            [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req)
        {
            _logger.LogInformation("Getting all resumes...");
            var data = JsonFileReader.Read<DigitalResumeModel[]>(_resumeDataPath);
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
        /// exists yet, no owner is configured, or the configured owner has no featured resume,
        /// in which case GetResume falls back to the pre-#28 static-file behavior.
        /// </summary>
        private async Task<DigitalResumeModel?> TryGetFeaturedResumeAsync()
        {
            if (_siteConfigStore is null || _resumeStore is null)
            {
                return null;
            }

            var config = await _siteConfigStore.GetAsync();
            if (string.IsNullOrWhiteSpace(config?.PublicResumeOwnerId))
            {
                return null;
            }

            var featured = await _resumeStore.FindFeaturedByOwnerAsync(config!.PublicResumeOwnerId!);
            if (featured is null)
            {
                return null;
            }

            try
            {
                return JsonSerializer.Deserialize<DigitalResumeModel>(featured.PayloadJson);
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
