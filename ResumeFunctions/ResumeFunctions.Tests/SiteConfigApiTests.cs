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

public class SiteConfigApiTests
{
    private readonly FakeSiteConfigStore _store = new();
    private readonly SiteConfigApi _api;

    public SiteConfigApiTests()
    {
        _api = new SiteConfigApi(Substitute.For<ILogger<SiteConfigApi>>(), _store);
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
    public async Task Get_Returns401_WhenNotLoggedIn()
    {
        var context = TestFunctionContextFactory.Create();
        var (_, request) = BuildRequest(context);

        var result = await _api.Get(request, context);

        Assert.Equal(HttpStatusCode.Unauthorized, result.StatusCode);
    }

    [Fact]
    public async Task Get_Returns403_ForVisitor()
    {
        var context = TestFunctionContextFactory.Create(TestFunctionContextFactory.CreateUser("jane", AccountRoles.Visitor));
        var (_, request) = BuildRequest(context);

        var result = await _api.Get(request, context);

        Assert.Equal(HttpStatusCode.Forbidden, result.StatusCode);
    }

    [Fact]
    public async Task Get_Returns200_ForResumeAdmin_WithNullFields_WhenUnconfigured()
    {
        var context = TestFunctionContextFactory.Create(TestFunctionContextFactory.CreateUser("jane", AccountRoles.ResumeAdmin));
        var (response, request) = BuildRequest(context);

        var result = await _api.Get(request, context);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        var dto = await ReadBody<SiteConfigDto>(response);
        Assert.Null(dto!.PublicResumeOwnerId);
        Assert.Null(dto.PublicProjectsOwnerId);
    }

    [Fact]
    public async Task Update_Returns401_WhenNotLoggedIn()
    {
        var context = TestFunctionContextFactory.Create();
        var (_, request) = BuildRequest(context, new { PublicResumeOwnerId = "jane", PublicProjectsOwnerId = "jane" }, "PUT");

        var result = await _api.Update(request, context);

        Assert.Equal(HttpStatusCode.Unauthorized, result.StatusCode);
    }

    [Fact]
    public async Task Update_Returns403_ForResumeAdmin_NotSuperAdmin()
    {
        var context = TestFunctionContextFactory.Create(TestFunctionContextFactory.CreateUser("jane", AccountRoles.ResumeAdmin));
        var (_, request) = BuildRequest(context, new { PublicResumeOwnerId = "jane", PublicProjectsOwnerId = "jane" }, "PUT");

        var result = await _api.Update(request, context);

        Assert.Equal(HttpStatusCode.Forbidden, result.StatusCode);
    }

    [Fact]
    public async Task Update_Returns200_ForSuperAdmin_AndPersists()
    {
        var context = TestFunctionContextFactory.Create(TestFunctionContextFactory.CreateUser("root", AccountRoles.SuperAdmin));
        var (response, request) = BuildRequest(context, new { PublicResumeOwnerId = "jane", PublicProjectsOwnerId = "bob" }, "PUT");

        var result = await _api.Update(request, context);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        var dto = await ReadBody<SiteConfigDto>(response);
        Assert.Equal("jane", dto!.PublicResumeOwnerId);
        Assert.Equal("bob", dto.PublicProjectsOwnerId);

        var persisted = await _store.GetAsync();
        Assert.Equal("jane", persisted!.PublicResumeOwnerId);
    }
}
