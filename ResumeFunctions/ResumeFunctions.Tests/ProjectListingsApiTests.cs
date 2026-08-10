using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using NSubstitute;
using ResumeFunctions.Auth;
using ResumeFunctions.Auth.Dtos;
using ResumeFunctions.Models;
using ResumeFunctions.Tests.Helpers;
using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;

namespace ResumeFunctions.Tests;

public class ProjectListingsApiTests
{
    private readonly FakeProjectListingStore _listingStore = new();
    private readonly FakeSiteConfigStore _siteConfigStore = new();
    private readonly ProjectListingsApi _api;

    public ProjectListingsApiTests()
    {
        _api = new ProjectListingsApi(Substitute.For<ILogger<ProjectListingsApi>>(), _listingStore, _siteConfigStore);
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

    private static object SamplePayload => new { Title = "My Projects", Sections = Array.Empty<object>() };

    [Fact]
    public async Task GetPublic_Returns200_WithoutRequiringAuth()
    {
        var context = TestFunctionContextFactory.Create();
        var (_, request) = BuildRequest(context);

        var result = await _api.GetPublic(request);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
    }

    [Fact]
    public async Task GetPublic_FallsBackToDefault_WhenNoSiteConfigured()
    {
        var context = TestFunctionContextFactory.Create();
        var (response, request) = BuildRequest(context);

        await _api.GetPublic(request);

        var listing = await ReadBody<ProjectListingModel>(response);
        Assert.NotNull(listing);
        Assert.NotNull(listing!.Sections);
        Assert.NotEmpty(listing.Sections!);
    }

    [Fact]
    public async Task GetPublic_ReturnsConfiguredOwnersFeaturedListing_WhenSet()
    {
        var ownerContext = TestFunctionContextFactory.Create(TestFunctionContextFactory.CreateUser("jane", AccountRoles.ResumeAdmin));
        var (createResponse, createRequest) = BuildRequest(ownerContext, new { OwnerUserId = (string?)null, IsFeatured = true, Payload = new { Title = "Jane's Projects", Sections = Array.Empty<object>() } }, "POST");
        await _api.Create(createRequest, ownerContext);

        await _siteConfigStore.SetAsync(null, "jane");

        var (publicResponse, publicRequest) = BuildRequest(TestFunctionContextFactory.Create());
        await _api.GetPublic(publicRequest);

        var listing = await ReadBody<ProjectListingModel>(publicResponse);
        Assert.Equal("Jane's Projects", listing!.Title);
    }

    [Fact]
    public async Task ListMine_Returns401_WhenNotLoggedIn()
    {
        var context = TestFunctionContextFactory.Create();
        var (_, request) = BuildRequest(context);

        var result = await _api.ListMine(request, context);

        Assert.Equal(HttpStatusCode.Unauthorized, result.StatusCode);
    }

    [Fact]
    public async Task Create_Returns403_WhenResumeAdminTriesToSetAnotherOwner()
    {
        var context = TestFunctionContextFactory.Create(TestFunctionContextFactory.CreateUser("jane", AccountRoles.ResumeAdmin));
        var (_, request) = BuildRequest(context, new { OwnerUserId = "bob", IsFeatured = false, Payload = SamplePayload }, "POST");

        var result = await _api.Create(request, context);

        Assert.Equal(HttpStatusCode.Forbidden, result.StatusCode);
    }

    [Fact]
    public async Task Update_Returns403_WhenNotOwnerAndNotSuperAdmin()
    {
        var janeContext = TestFunctionContextFactory.Create(TestFunctionContextFactory.CreateUser("jane", AccountRoles.ResumeAdmin));
        var (createResponse, createRequest) = BuildRequest(janeContext, new { OwnerUserId = (string?)null, IsFeatured = false, Payload = SamplePayload }, "POST");
        var created = await ReadBody<ProjectListingDto>((TestHttpResponseData)await _api.Create(createRequest, janeContext));

        var bobContext = TestFunctionContextFactory.Create(TestFunctionContextFactory.CreateUser("bob", AccountRoles.ResumeAdmin));
        var (_, updateRequest) = BuildRequest(bobContext, new { OwnerUserId = (string?)null, IsFeatured = false, Payload = SamplePayload }, "PUT");

        var result = await _api.Update(updateRequest, bobContext, created!.Id);

        Assert.Equal(HttpStatusCode.Forbidden, result.StatusCode);
    }

    [Fact]
    public async Task Update_Returns200_ForSuperAdmin_EvenWhenNotOwner()
    {
        var janeContext = TestFunctionContextFactory.Create(TestFunctionContextFactory.CreateUser("jane", AccountRoles.ResumeAdmin));
        var (createResponse, createRequest) = BuildRequest(janeContext, new { OwnerUserId = (string?)null, IsFeatured = false, Payload = SamplePayload }, "POST");
        var created = await ReadBody<ProjectListingDto>((TestHttpResponseData)await _api.Create(createRequest, janeContext));

        var superContext = TestFunctionContextFactory.Create(TestFunctionContextFactory.CreateUser("root", AccountRoles.SuperAdmin));
        var (_, updateRequest) = BuildRequest(superContext, new { OwnerUserId = (string?)null, IsFeatured = false, Payload = SamplePayload }, "PUT");

        var result = await _api.Update(updateRequest, superContext, created!.Id);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
    }

    [Fact]
    public async Task Delete_Returns403_WhenNotOwnerAndNotSuperAdmin()
    {
        var janeContext = TestFunctionContextFactory.Create(TestFunctionContextFactory.CreateUser("jane", AccountRoles.ResumeAdmin));
        var (createResponse, createRequest) = BuildRequest(janeContext, new { OwnerUserId = (string?)null, IsFeatured = false, Payload = SamplePayload }, "POST");
        var created = await ReadBody<ProjectListingDto>((TestHttpResponseData)await _api.Create(createRequest, janeContext));

        var bobContext = TestFunctionContextFactory.Create(TestFunctionContextFactory.CreateUser("bob", AccountRoles.ResumeAdmin));
        var (_, deleteRequest) = BuildRequest(bobContext, method: "DELETE");

        var result = await _api.Delete(deleteRequest, bobContext, created!.Id);

        Assert.Equal(HttpStatusCode.Forbidden, result.StatusCode);
    }

    [Fact]
    public async Task Delete_Returns204_WhenOwner()
    {
        var janeContext = TestFunctionContextFactory.Create(TestFunctionContextFactory.CreateUser("jane", AccountRoles.ResumeAdmin));
        var (createResponse, createRequest) = BuildRequest(janeContext, new { OwnerUserId = (string?)null, IsFeatured = false, Payload = SamplePayload }, "POST");
        var created = await ReadBody<ProjectListingDto>((TestHttpResponseData)await _api.Create(createRequest, janeContext));

        var (_, deleteRequest) = BuildRequest(janeContext, method: "DELETE");

        var result = await _api.Delete(deleteRequest, janeContext, created!.Id);

        Assert.Equal(HttpStatusCode.NoContent, result.StatusCode);
    }
}
