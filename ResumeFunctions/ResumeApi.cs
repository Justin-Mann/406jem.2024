using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using ResumeFunctions.Models;
using System.Net;
using System.Text.Json;

namespace ResumeFunctions
{
    public class ResumeApi
    {
        private readonly ILogger<ResumeApi> _logger;
        private readonly string _resumeDataPath;

        public ResumeApi(ILogger<ResumeApi> logger, string? resumeDataPath = null)
        {
            _logger = logger;
            _resumeDataPath = resumeDataPath ?? Path.Combine(AppContext.BaseDirectory, "StaticData", "Resumes", "JustinMann_062024.json");
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
            var data = JsonFileReader.Read<DigitalResumeModel[]>(_resumeDataPath);
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(data?.FirstOrDefault());
            return response;
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
