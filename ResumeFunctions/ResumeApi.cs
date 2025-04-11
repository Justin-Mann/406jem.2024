using Azure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using ResumeFunctions.Models;
using System.Collections.Generic;
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
        public IActionResult GetAllResumes([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req)
        {
            var resumeJsonResponse = JsonFileReader.Read<DigitalResumeModel[]>(@".\StaticData\Resumes\JustinMann_062024.json");
            return new OkObjectResult(resumeJsonResponse ?? null);
        }

        [Function("myResume")]
        public IActionResult GetResume([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req)
        {

            var resumeJsonResponse = JsonFileReader.Read<DigitalResumeModel[]>(@".\StaticData\Resumes\JustinMann_062024.json");
            return new OkObjectResult(resumeJsonResponse.FirstOrDefault<DigitalResumeModel>());
        }

        internal static class JsonFileReader
        {
            public static T Read<T>(string filePath)
            {
                using FileStream stream = File.OpenRead(filePath);
                return JsonSerializer.Deserialize<T>(stream);
            }
        }
    }
}
