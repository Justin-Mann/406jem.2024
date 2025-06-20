using Microsoft.AspNetCore.Mvc;
using ResumeFunctions.Models;
using System.Text.Json;

namespace MyResumeApi.Controllers
{
    [ApiController]
    [Route("resumes")]
    public class ResumeController : Controller
    {
        private readonly ILogger<ResumeController> _logger;

        public ResumeController(ILogger<ResumeController> logger)
        {
            _logger = logger;
        }

        [HttpGet(Name = "GetMyResume")]
        [Route("myresume")]
        public IActionResult Get()
        {
            _logger.Log(LogLevel.Information, "Getting my resume...");
            var resumeJsonResponse = JsonFileReader.Read<DigitalResumeModel[]>(@".\ResumeDataFile\JustinMann_062024.json");
            return new OkObjectResult(resumeJsonResponse.FirstOrDefault<DigitalResumeModel>());
        }

        [HttpGet(Name = "GetResumeById")]
        [Route("resume/{id:int}")]
        public IActionResult GetById(int id)
        {
            if (id <= 0)
            {
                _logger.Log(LogLevel.Warning, "Invalid resume ID provided: " + id);
                return BadRequest("Invalid resume ID.");
            }

            _logger.Log(LogLevel.Information, "Getting resume By Id (" + id + ")...");
            var resumeJsonResponse = JsonFileReader.Read<DigitalResumeModel[]>(@".\ResumeDataFile\JustinMann_062024.json");
            return new OkObjectResult(resumeJsonResponse.Where(r=>r.Id.Equals(id)).FirstOrDefault<DigitalResumeModel>());
        }

        private static class JsonFileReader
        {
            public static T Read<T>(string filePath)
            {
                using FileStream stream = System.IO.File.OpenRead(filePath);
                return JsonSerializer.Deserialize<T>(stream);
            }
        }
    }
}