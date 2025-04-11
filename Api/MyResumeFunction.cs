using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Api.Models;
using System.Text.Json;
using System.Net;

namespace Api
{
    public class HttpTrigger
    {
        private readonly ILogger _logger;

        public HttpTrigger(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<HttpTrigger>();
        }

        [Function("MyResume")]
        public HttpResponseData Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "myResume")] HttpRequestData req)
        {
            var resumeResponse = JsonFileReader.Read<DigitalResumeModel>(@".\StaticData\Resumes\JustinMann_062024.json");
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.WriteAsJsonAsync(resumeResponse);
            return response;
        }

        public static class JsonFileReader
        {
            public static T Read<T>(string filePath)
            {
                using FileStream stream = File.OpenRead(filePath);
                return JsonSerializer.Deserialize<T>(stream);
            }
        }
    }
}