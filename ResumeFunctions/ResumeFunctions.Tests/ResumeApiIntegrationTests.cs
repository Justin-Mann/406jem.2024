using System.Net;
using System.Text.Json;
using Xunit;

namespace ResumeFunctions.Tests;

[Trait("Category", "Integration")]
public class ResumeApiIntegrationTests : IDisposable
{
    private readonly HttpClient _client;
    private readonly string _baseUrl;

    public ResumeApiIntegrationTests()
    {
        _baseUrl = (Environment.GetEnvironmentVariable("FUNCTIONS_STAGING_URL") ?? string.Empty)
            .TrimEnd('/');

        if (string.IsNullOrWhiteSpace(_baseUrl))
            throw new InvalidOperationException(
                "FUNCTIONS_STAGING_URL environment variable must be set for integration tests.");

        _client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
    }

    [Fact]
    public async Task GetMyResume_Returns200()
    {
        var response = await _client.GetAsync($"{_baseUrl}/api/resumes/myresume");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetMyResume_ContentTypeIsJson()
    {
        var response = await _client.GetAsync($"{_baseUrl}/api/resumes/myresume");
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task GetMyResume_ReturnsObjectNotArray()
    {
        var response = await _client.GetAsync($"{_baseUrl}/api/resumes/myresume");
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        Assert.NotEqual('[', content.TrimStart()[0]);
    }

    [Fact]
    public async Task GetMyResume_HasExpectedFields()
    {
        var response = await _client.GetAsync($"{_baseUrl}/api/resumes/myresume");
        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = doc.RootElement;

        Assert.Equal(JsonValueKind.Object, root.ValueKind);
        Assert.True(
            root.TryGetProperty("fName", out _) || root.TryGetProperty("FName", out _),
            "Response must contain fName");
        Assert.True(
            root.TryGetProperty("lName", out _) || root.TryGetProperty("LName", out _),
            "Response must contain lName");
    }

    [Fact]
    public async Task GetMyResume_WorkExperienceIsPresent()
    {
        var response = await _client.GetAsync($"{_baseUrl}/api/resumes/myresume");
        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = doc.RootElement;

        var found = root.TryGetProperty("workExperience", out var wx)
                 || root.TryGetProperty("WorkExperience", out wx);

        Assert.True(found, "Response must contain workExperience");
        Assert.Equal(JsonValueKind.Array, wx.ValueKind);
        Assert.True(wx.GetArrayLength() > 0, "workExperience must not be empty");
    }

    public void Dispose() => _client.Dispose();
}
