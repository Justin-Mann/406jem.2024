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

        public ResumeApi(ILogger<ResumeApi> logger)
        {
            _logger = logger;
        }

        [Function("resumes")]
        public async Task<HttpResponseData> GetAllResumes(
            [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req)
        {
            _logger.LogInformation("Getting all resumes...");
            var data = JsonFileReader.Read<DigitalResumeModel[]>(Path.Combine("StaticData", "Resumes", "JustinMann_062024.json"));
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(data);
            return response;
        }

        [Function("myResume")]
        public async Task<HttpResponseData> GetResume(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "resumes/myresume")] HttpRequestData req)
        {
            _logger.LogInformation("Getting my resume...");
            var data = JsonFileReader.Read<DigitalResumeModel[]>(Path.Combine("StaticData", "Resumes", "JustinMann_062024.json"));
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
