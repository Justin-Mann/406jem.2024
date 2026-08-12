using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using NSubstitute;
using ResumeFunctions.Auth;
using ResumeFunctions.Auth.Dtos;
using ResumeFunctions.Auth.Models;
using ResumeFunctions.Auth.Storage;
using ResumeFunctions.Tests.Helpers;
using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;

namespace ResumeFunctions.Tests;

public class ResumePostersApiTests
{
    private readonly IUserStore _userStore = Substitute.For<IUserStore>();
    private readonly ResumePostersApi _api;

    public ResumePostersApiTests()
    {
        _api = new ResumePostersApi(Substitute.For<ILogger<ResumePostersApi>>(), _userStore);
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

    private static UserAccountEntity MakeUser(string username, string role, string email, string displayName = "") => new()
    {
        Username = username,
        RowKey = username,
        Role = role,
        Email = email,
        DisplayName = displayName,
        PasswordHash = "irrelevant",
        CreatedAtUtc = DateTimeOffset.UtcNow,
    };

    [Fact]
    public async Task List_Returns200_WithoutRequiringAuth()
    {
        _userStore.ListAsync().Returns(new List<UserAccountEntity>());
        var context = TestFunctionContextFactory.Create();
        var (_, request) = BuildRequest(context);

        var result = await _api.List(request);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
    }

    [Fact]
    public async Task List_OnlyIncludesResumeAdminsAndSuperAdmins_UsingDisplayNameWithUsernameFallback()
    {
        _userStore.ListAsync().Returns(new List<UserAccountEntity>
        {
            MakeUser("jane", AccountRoles.ResumeAdmin, "jane@example.com", "Jane Doe"),
            MakeUser("root", AccountRoles.SuperAdmin, "root@example.com"), // no DisplayName set
            MakeUser("visitor1", AccountRoles.Visitor, "visitor1@example.com", "Should Not Appear"),
        });
        var context = TestFunctionContextFactory.Create();
        var (response, request) = BuildRequest(context);

        await _api.List(request);
        response.Body.Position = 0;
        var body = await JsonSerializer.DeserializeAsync<List<ResumePosterDto>>(
            response.Body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(body);
        Assert.Equal(2, body!.Count);
        var displayNames = body.Select(e => e.DisplayName).ToList();
        Assert.Contains("Jane Doe", displayNames);
        Assert.Contains("root", displayNames); // fell back to Username
        Assert.DoesNotContain("Should Not Appear", displayNames);

        // Never exposes an email address.
        response.Body.Position = 0;
        using var reader = new StreamReader(response.Body);
        var raw = await reader.ReadToEndAsync();
        Assert.DoesNotContain("@example.com", raw);
    }
}
