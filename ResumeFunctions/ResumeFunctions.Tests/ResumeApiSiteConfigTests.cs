using Azure.Core.Serialization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using ResumeFunctions.Auth.Models;
using ResumeFunctions.Models;
using ResumeFunctions.Tests.Helpers;
using System.Net;
using System.Text.Json;
using Xunit;

namespace ResumeFunctions.Tests;

/// <summary>
/// Covers #28's "public endpoints resolve through SiteConfig, falling back to the static file"
/// behavior — kept separate from ResumeApiTests (which exercises the no-SiteConfig-injected
/// constructor overload and must keep passing unchanged).
/// </summary>
public class ResumeApiSiteConfigTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _dataPath;
    private readonly FakeSiteConfigStore _siteConfigStore = new();
    private readonly FakeResumeStore _resumeStore = new();
    private readonly FakeResumeSnapshotStore _snapshotStore = new();
    private readonly ResumeApi _api;
    private readonly FunctionContext _functionContext;

    public ResumeApiSiteConfigTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
        _dataPath = Path.Combine(_tempDir, "JustinMann_062024.json");
        File.WriteAllText(_dataPath, TestData.ResumeJson);

        var services = new ServiceCollection();
        services.Configure<WorkerOptions>(opts => opts.Serializer = new JsonObjectSerializer());
        _functionContext = Substitute.For<FunctionContext>();
        _functionContext.InstanceServices.Returns(services.BuildServiceProvider());

        _api = new ResumeApi(Substitute.For<ILogger<ResumeApi>>(), _dataPath, _siteConfigStore, _resumeStore, _snapshotStore, BuildConfiguration());
    }

    private static IConfiguration BuildConfiguration(string adminUsername = "admin") =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Auth:AdminUsername"] = adminUsername })
            .Build();

    private (TestHttpResponseData response, TestHttpRequestData request) BuildRequest()
    {
        var response = new TestHttpResponseData(_functionContext);
        var request = new TestHttpRequestData(_functionContext, response);
        return (response, request);
    }

    [Fact]
    public async Task GetResume_FallsBackToStaticFile_WhenNoSiteConfigRow()
    {
        var (response, request) = BuildRequest();

        await _api.GetResume(request);

        response.Body.Position = 0;
        var resume = await JsonSerializer.DeserializeAsync<DigitalResumeModel>(response.Body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.Equal("Jane", resume!.FName);
    }

    [Fact]
    public async Task GetResume_FallsBackToStaticFile_WhenConfiguredOwnerIsSuperAdminWithNoFeaturedResume()
    {
        // "admin" matches the default Auth:AdminUsername (#39 round-4 static-file fallback is
        // restricted to the SuperAdmin owner) — see the "nobody"-equivalent, non-admin variant
        // below for the case that must NOT fall back.
        await _siteConfigStore.SetAsync("admin", null);
        var (response, request) = BuildRequest();

        await _api.GetResume(request);

        response.Body.Position = 0;
        var resume = await JsonSerializer.DeserializeAsync<DigitalResumeModel>(response.Body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.Equal("Jane", resume!.FName);
    }

    [Fact]
    public async Task GetResume_DoesNotFallBackToStaticFile_WhenConfiguredOwnerIsNotSuperAdminAndHasNoFeaturedResume()
    {
        await _siteConfigStore.SetAsync("nobody", null);
        var (response, request) = BuildRequest();

        await _api.GetResume(request);

        response.Body.Position = 0;
        var resume = await JsonSerializer.DeserializeAsync<DigitalResumeModel?>(response.Body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.Null(resume);
    }

    [Fact]
    public async Task GetResume_ReturnsConfiguredOwnersFeaturedResume_WhenSet()
    {
        await _resumeStore.AddAsync(new ResumeEntity
        {
            OwnerUserId = "alice",
            IsFeatured = true,
            PayloadJson = JsonSerializer.Serialize(new DigitalResumeModel { FName = "Alice", LName = "Admin" }),
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
        });
        await _siteConfigStore.SetAsync("alice", null);

        var (response, request) = BuildRequest();
        await _api.GetResume(request);

        response.Body.Position = 0;
        var resume = await JsonSerializer.DeserializeAsync<DigitalResumeModel>(response.Body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.Equal("Alice", resume!.FName);
    }

    [Fact]
    public async Task GetResume_Returns200_WithFeaturedOwnerConfigured()
    {
        await _resumeStore.AddAsync(new ResumeEntity
        {
            OwnerUserId = "alice",
            IsFeatured = true,
            PayloadJson = JsonSerializer.Serialize(new DigitalResumeModel { FName = "Alice" }),
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
        });
        await _siteConfigStore.SetAsync("alice", null);

        var (response, request) = BuildRequest();
        var result = await _api.GetResume(request);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
    }

    [Fact]
    public async Task GetAllResumes_FallsBackToStaticFile_WhenNoSiteConfigRow()
    {
        var (response, request) = BuildRequest();

        await _api.GetAllResumes(request);

        response.Body.Position = 0;
        var resumes = await JsonSerializer.DeserializeAsync<DigitalResumeModel[]>(response.Body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.Single(resumes!);
        Assert.Equal("Jane", resumes![0].FName);
    }

    [Fact]
    public async Task GetAllResumes_ReturnsConfiguredOwnersFeaturedResume_WrappedInArray_WhenSet()
    {
        await _resumeStore.AddAsync(new ResumeEntity
        {
            OwnerUserId = "alice",
            IsFeatured = true,
            PayloadJson = JsonSerializer.Serialize(new DigitalResumeModel { FName = "Alice", LName = "Admin" }),
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
        });
        await _siteConfigStore.SetAsync("alice", null);

        var (response, request) = BuildRequest();
        await _api.GetAllResumes(request);

        response.Body.Position = 0;
        var resumes = await JsonSerializer.DeserializeAsync<DigitalResumeModel[]>(response.Body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.Single(resumes!);
        Assert.Equal("Alice", resumes![0].FName);
    }

    [Fact]
    public async Task GetResume_FallsBackToOwnersSnapshot_WhenConfiguredOwnerHasNoLiveFeaturedResume()
    {
        await _snapshotStore.SaveAsync("alice", JsonSerializer.Serialize(new DigitalResumeModel { FName = "SnapshotAlice" }));
        await _siteConfigStore.SetAsync("alice", null);

        var (response, request) = BuildRequest();
        await _api.GetResume(request);

        response.Body.Position = 0;
        var resume = await JsonSerializer.DeserializeAsync<DigitalResumeModel>(response.Body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.Equal("SnapshotAlice", resume!.FName);
    }

    [Fact]
    public async Task GetResume_PrefersLiveFeaturedResume_OverOwnersSnapshot()
    {
        await _resumeStore.AddAsync(new ResumeEntity
        {
            OwnerUserId = "alice",
            IsFeatured = true,
            PayloadJson = JsonSerializer.Serialize(new DigitalResumeModel { FName = "LiveAlice" }),
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
        });
        await _snapshotStore.SaveAsync("alice", JsonSerializer.Serialize(new DigitalResumeModel { FName = "SnapshotAlice" }));
        await _siteConfigStore.SetAsync("alice", null);

        var (response, request) = BuildRequest();
        await _api.GetResume(request);

        response.Body.Position = 0;
        var resume = await JsonSerializer.DeserializeAsync<DigitalResumeModel>(response.Body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.Equal("LiveAlice", resume!.FName);
    }

    [Fact]
    public async Task GetResume_DoesNotFallBackToStaticFile_WhenNonSuperAdminOwnerHasNoLiveResumeAndNoSnapshot()
    {
        // "alice" is not the configured SuperAdmin ("admin") — surfacing the static file here
        // would incorrectly show alice's page the SuperAdmin's resume content (#39 round-4 finding).
        await _siteConfigStore.SetAsync("alice", null);

        var (response, request) = BuildRequest();
        await _api.GetResume(request);

        response.Body.Position = 0;
        var resume = await JsonSerializer.DeserializeAsync<DigitalResumeModel?>(response.Body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.Null(resume);
    }

    [Fact]
    public async Task GetResume_FallsBackToStaticFile_WhenConfiguredOwnerIsSuperAdminWithNoLiveResumeAndNoSnapshot()
    {
        await _siteConfigStore.SetAsync("admin", null);

        var (response, request) = BuildRequest();
        await _api.GetResume(request);

        response.Body.Position = 0;
        var resume = await JsonSerializer.DeserializeAsync<DigitalResumeModel>(response.Body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.Equal("Jane", resume!.FName);
    }

    [Fact]
    public async Task GetAllResumes_ReturnsEmptyArray_WhenNonSuperAdminOwnerHasNoLiveResumeAndNoSnapshot()
    {
        await _siteConfigStore.SetAsync("alice", null);

        var (response, request) = BuildRequest();
        await _api.GetAllResumes(request);

        response.Body.Position = 0;
        var resumes = await JsonSerializer.DeserializeAsync<DigitalResumeModel[]>(response.Body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.Empty(resumes!);
    }

    [Fact]
    public async Task GetResume_FallsBackToOwnersSnapshot_WhenLiveStoreLookupThrows()
    {
        _resumeStore.ThrowOnFindFeatured = true;
        await _snapshotStore.SaveAsync("alice", JsonSerializer.Serialize(new DigitalResumeModel { FName = "SnapshotAlice" }));
        await _siteConfigStore.SetAsync("alice", null);

        var (response, request) = BuildRequest();
        await _api.GetResume(request);

        response.Body.Position = 0;
        var resume = await JsonSerializer.DeserializeAsync<DigitalResumeModel>(response.Body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.Equal("SnapshotAlice", resume!.FName);
    }

    [Fact]
    public async Task GetResume_FallsBackToStaticFile_WhenLiveStoreAndSnapshotLookupBothThrow_ForSuperAdminOwner()
    {
        _resumeStore.ThrowOnFindFeatured = true;
        _snapshotStore.ThrowOnGet = true;
        await _siteConfigStore.SetAsync("admin", null);

        var (response, request) = BuildRequest();
        var result = await _api.GetResume(request);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        response.Body.Position = 0;
        var resume = await JsonSerializer.DeserializeAsync<DigitalResumeModel>(response.Body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.Equal("Jane", resume!.FName);
    }

    [Fact]
    public async Task GetResume_DoesNotFallBackToStaticFile_WhenLiveStoreAndSnapshotLookupBothThrow_ForNonSuperAdminOwner()
    {
        _resumeStore.ThrowOnFindFeatured = true;
        _snapshotStore.ThrowOnGet = true;
        await _siteConfigStore.SetAsync("alice", null);

        var (response, request) = BuildRequest();
        var result = await _api.GetResume(request);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        response.Body.Position = 0;
        var resume = await JsonSerializer.DeserializeAsync<DigitalResumeModel?>(response.Body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.Null(resume);
    }

    [Fact]
    public async Task GetAllResumes_FallsBackToOwnersSnapshot_WrappedInArray_WhenConfiguredOwnerHasNoLiveFeaturedResume()
    {
        await _snapshotStore.SaveAsync("alice", JsonSerializer.Serialize(new DigitalResumeModel { FName = "SnapshotAlice" }));
        await _siteConfigStore.SetAsync("alice", null);

        var (response, request) = BuildRequest();
        await _api.GetAllResumes(request);

        response.Body.Position = 0;
        var resumes = await JsonSerializer.DeserializeAsync<DigitalResumeModel[]>(response.Body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.Single(resumes!);
        Assert.Equal("SnapshotAlice", resumes![0].FName);
    }

    public void Dispose()
    {
        Directory.Delete(_tempDir, recursive: true);
    }
}
