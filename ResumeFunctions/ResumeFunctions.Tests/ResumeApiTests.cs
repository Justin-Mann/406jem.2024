using Azure.Core.Serialization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using ResumeFunctions.Models;
using ResumeFunctions.Tests.Helpers;
using System.Net;
using System.Text.Json;
using Xunit;

namespace ResumeFunctions.Tests;

public class ResumeApiTests : IDisposable
{
    private readonly string _originalDir = Environment.CurrentDirectory;
    private readonly string _tempDir;
    private readonly ResumeApi _api;
    private readonly FunctionContext _functionContext;

    public ResumeApiTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var dataDir = Path.Combine(_tempDir, "StaticData", "Resumes");
        Directory.CreateDirectory(dataDir);
        File.WriteAllText(Path.Combine(dataDir, "JustinMann_062024.json"), TestData.ResumeJson);
        Environment.CurrentDirectory = _tempDir;

        var services = new ServiceCollection();
        services.Configure<WorkerOptions>(opts => opts.Serializer = new JsonObjectSerializer());
        _functionContext = Substitute.For<FunctionContext>();
        _functionContext.InstanceServices.Returns(services.BuildServiceProvider());

        _api = new ResumeApi(Substitute.For<ILogger<ResumeApi>>());
    }

    private (TestHttpResponseData response, TestHttpRequestData request) BuildRequest()
    {
        var response = new TestHttpResponseData(_functionContext);
        var request = new TestHttpRequestData(_functionContext, response);
        return (response, request);
    }

    [Fact]
    public async Task GetResume_Returns200()
    {
        var (response, request) = BuildRequest();

        var result = await _api.GetResume(request);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
    }

    [Fact]
    public async Task GetResume_ReturnsFirstResumeEntry()
    {
        var (response, request) = BuildRequest();

        await _api.GetResume(request);

        response.Body.Position = 0;
        var resume = await JsonSerializer.DeserializeAsync<DigitalResumeModel>(
            response.Body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(resume);
        Assert.Equal("Jane", resume.FName);
        Assert.Equal("Doe", resume.LName);
    }

    [Fact]
    public async Task GetResume_ReturnsSingleObject_NotArray()
    {
        var (response, request) = BuildRequest();

        await _api.GetResume(request);

        response.Body.Position = 0;
        var json = await new StreamReader(response.Body).ReadToEndAsync();
        Assert.DoesNotContain("[", json[..1]); // not an array
    }

    [Fact]
    public async Task GetAllResumes_Returns200()
    {
        var (response, request) = BuildRequest();

        var result = await _api.GetAllResumes(request);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
    }

    [Fact]
    public async Task GetAllResumes_ReturnsArray()
    {
        var (response, request) = BuildRequest();

        await _api.GetAllResumes(request);

        response.Body.Position = 0;
        var resumes = await JsonSerializer.DeserializeAsync<DigitalResumeModel[]>(
            response.Body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(resumes);
        Assert.Single(resumes);
        Assert.Equal("Jane", resumes[0].FName);
    }

    [Fact]
    public async Task GetAllResumes_ResponseContainsWorkExperience()
    {
        var (response, request) = BuildRequest();

        await _api.GetAllResumes(request);

        response.Body.Position = 0;
        var resumes = await JsonSerializer.DeserializeAsync<DigitalResumeModel[]>(
            response.Body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(resumes);
        Assert.Single(resumes[0].WorkExperience!);
        Assert.Equal("Acme Corp", resumes[0].WorkExperience!.First().CompanyName);
    }

    public void Dispose()
    {
        Environment.CurrentDirectory = _originalDir;
        Directory.Delete(_tempDir, recursive: true);
    }
}
