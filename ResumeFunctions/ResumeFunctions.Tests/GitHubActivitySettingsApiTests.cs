using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using NSubstitute;
using ResumeFunctions.Auth;
using ResumeFunctions.Auth.Dtos;
using ResumeFunctions.Tests.Helpers;
using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;

namespace ResumeFunctions.Tests;

public class GitHubActivitySettingsApiTests
{
    private readonly FakeGitHubActivitySettingsStore _settingsStore = new();
    private readonly FakeSiteConfigStore _siteConfigStore = new();
    private readonly GitHubActivitySettingsApi _api;

    public GitHubActivitySettingsApiTests()
    {
        _api = new GitHubActivitySettingsApi(
            Substitute.For<ILogger<GitHubActivitySettingsApi>>(), _settingsStore, _siteConfigStore);
    }

    private static (TestHttpResponseData response, TestHttpRequestData request) BuildRequest(
        FunctionContext context, object? body = null, string method = "GET")
    {
        var response = new TestHttpResponseData(context);
        Stream? bodyStream = body is null
            ? null
            : new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(body)));
        var request = new TestHttpRequestData(context, response, bodyStream, method);
        return (response, request);
    }

    private static async Task<T?> ReadBody<T>(TestHttpResponseData response)
    {
        response.Body.Position = 0;
        return await JsonSerializer.DeserializeAsync<T>(response.Body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    [Fact]
    public async Task GetMine_Returns401_WhenNotLoggedIn()
    {
        var context = TestFunctionContextFactory.Create();
        var (_, request) = BuildRequest(context);

        var result = await _api.GetMine(request, context);

        Assert.Equal(HttpStatusCode.Unauthorized, result.StatusCode);
    }

    [Fact]
    public async Task GetMine_Returns403_ForVisitor()
    {
        var context = TestFunctionContextFactory.Create(TestFunctionContextFactory.CreateUser("jane", AccountRoles.Visitor));
        var (_, request) = BuildRequest(context);

        var result = await _api.GetMine(request, context);

        Assert.Equal(HttpStatusCode.Forbidden, result.StatusCode);
    }

    [Fact]
    public async Task GetMine_Returns200_WithDefaults_WhenUnconfigured()
    {
        var context = TestFunctionContextFactory.Create(TestFunctionContextFactory.CreateUser("jane", AccountRoles.ResumeAdmin));
        var (response, request) = BuildRequest(context);

        var result = await _api.GetMine(request, context);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        var dto = await ReadBody<GitHubActivitySettingsDto>(response);
        Assert.False(dto!.Enabled);
        Assert.Null(dto.GitHubUsername);
        Assert.Equal(GitHubActivitySettingsApi.DefaultRepoCount, dto.RepoCount);
        Assert.Empty(dto.PinnedRepoNames);
    }

    [Fact]
    public async Task GetMine_Returns200_WithPersistedSettings()
    {
        await _settingsStore.SetAsync("jane", true, "janedoe", 8, new List<string> { "repo-a", "repo-b" });
        var context = TestFunctionContextFactory.Create(TestFunctionContextFactory.CreateUser("jane", AccountRoles.ResumeAdmin));
        var (response, request) = BuildRequest(context);

        var result = await _api.GetMine(request, context);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        var dto = await ReadBody<GitHubActivitySettingsDto>(response);
        Assert.True(dto!.Enabled);
        Assert.Equal("janedoe", dto.GitHubUsername);
        Assert.Equal(8, dto.RepoCount);
        Assert.Equal(new[] { "repo-a", "repo-b" }, dto.PinnedRepoNames);
    }

    [Fact]
    public async Task UpdateMine_Returns401_WhenNotLoggedIn()
    {
        var context = TestFunctionContextFactory.Create();
        var (_, request) = BuildRequest(context, new { Enabled = true, GitHubUsername = "janedoe", RepoCount = 5, PinnedRepoNames = new string[0] }, "PUT");

        var result = await _api.UpdateMine(request, context);

        Assert.Equal(HttpStatusCode.Unauthorized, result.StatusCode);
    }

    [Fact]
    public async Task UpdateMine_Returns403_ForVisitor()
    {
        var context = TestFunctionContextFactory.Create(TestFunctionContextFactory.CreateUser("jane", AccountRoles.Visitor));
        var (_, request) = BuildRequest(context, new { Enabled = true, GitHubUsername = "janedoe", RepoCount = 5, PinnedRepoNames = new string[0] }, "PUT");

        var result = await _api.UpdateMine(request, context);

        Assert.Equal(HttpStatusCode.Forbidden, result.StatusCode);
    }

    [Fact]
    public async Task UpdateMine_Returns200_AndPersists_ForOwnUsername()
    {
        var context = TestFunctionContextFactory.Create(TestFunctionContextFactory.CreateUser("jane", AccountRoles.ResumeAdmin));
        var (response, request) = BuildRequest(
            context,
            new { Enabled = true, GitHubUsername = "janedoe", RepoCount = 3, PinnedRepoNames = new[] { "repo-a" } },
            "PUT");

        var result = await _api.UpdateMine(request, context);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        var dto = await ReadBody<GitHubActivitySettingsDto>(response);
        Assert.True(dto!.Enabled);
        Assert.Equal("janedoe", dto.GitHubUsername);
        Assert.Equal(3, dto.RepoCount);
        Assert.Equal(new[] { "repo-a" }, dto.PinnedRepoNames);

        var persisted = await _settingsStore.GetByOwnerAsync("jane");
        Assert.NotNull(persisted);
        Assert.True(persisted!.Enabled);
    }

    [Fact]
    public async Task UpdateMine_FallsBackToDefaultRepoCount_WhenNonPositive()
    {
        var context = TestFunctionContextFactory.Create(TestFunctionContextFactory.CreateUser("jane", AccountRoles.ResumeAdmin));
        var (response, request) = BuildRequest(
            context,
            new { Enabled = true, GitHubUsername = "janedoe", RepoCount = 0, PinnedRepoNames = new string[0] },
            "PUT");

        var result = await _api.UpdateMine(request, context);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        var dto = await ReadBody<GitHubActivitySettingsDto>(response);
        Assert.Equal(GitHubActivitySettingsApi.DefaultRepoCount, dto!.RepoCount);
    }

    [Fact]
    public async Task GetPublic_Returns404_WhenNoSiteConfig()
    {
        var context = TestFunctionContextFactory.Create();
        var (_, request) = BuildRequest(context);

        var result = await _api.GetPublic(request);

        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
    }

    [Fact]
    public async Task GetPublic_Returns404_WhenSiteConfigOwnerHasNoSettingsRow()
    {
        await _siteConfigStore.SetAsync(publicResumeOwnerId: null, publicProjectsOwnerId: "jane");
        var context = TestFunctionContextFactory.Create();
        var (_, request) = BuildRequest(context);

        var result = await _api.GetPublic(request);

        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
    }

    [Fact]
    public async Task GetPublic_Returns404_WhenSettingsExistButDisabled()
    {
        await _siteConfigStore.SetAsync(publicResumeOwnerId: null, publicProjectsOwnerId: "jane");
        await _settingsStore.SetAsync("jane", false, "janedoe", 5, new List<string>());
        var context = TestFunctionContextFactory.Create();
        var (_, request) = BuildRequest(context);

        var result = await _api.GetPublic(request);

        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
    }

    [Fact]
    public async Task GetPublic_Returns200_WithSettings_WhenEnabled()
    {
        await _siteConfigStore.SetAsync(publicResumeOwnerId: null, publicProjectsOwnerId: "jane");
        await _settingsStore.SetAsync("jane", true, "janedoe", 6, new List<string> { "repo-a" });
        var context = TestFunctionContextFactory.Create();
        var (response, request) = BuildRequest(context);

        var result = await _api.GetPublic(request);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        var dto = await ReadBody<GitHubActivitySettingsDto>(response);
        Assert.True(dto!.Enabled);
        Assert.Equal("janedoe", dto.GitHubUsername);
        Assert.Equal(6, dto.RepoCount);
        Assert.Equal(new[] { "repo-a" }, dto.PinnedRepoNames);
    }
}
