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

public class ResumeAdminApiTests
{
    private readonly FakeResumeStore _store = new();
    private readonly ResumeAdminApi _api;

    public ResumeAdminApiTests()
    {
        _api = new ResumeAdminApi(Substitute.For<ILogger<ResumeAdminApi>>(), _store);
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

    private static object SamplePayload => new { FName = "Jane", LName = "Doe" };

    [Fact]
    public async Task ListMine_Returns401_WhenNotLoggedIn()
    {
        var context = TestFunctionContextFactory.Create();
        var (_, request) = BuildRequest(context);

        var result = await _api.ListMine(request, context);

        Assert.Equal(HttpStatusCode.Unauthorized, result.StatusCode);
    }

    [Fact]
    public async Task ListMine_Returns403_ForVisitor()
    {
        var context = TestFunctionContextFactory.Create(TestFunctionContextFactory.CreateUser("jane", AccountRoles.Visitor));
        var (_, request) = BuildRequest(context);

        var result = await _api.ListMine(request, context);

        Assert.Equal(HttpStatusCode.Forbidden, result.StatusCode);
    }

    [Fact]
    public async Task ListMine_OnlyReturnsCallersOwnResumes()
    {
        var janeContext = TestFunctionContextFactory.Create(TestFunctionContextFactory.CreateUser("jane", AccountRoles.ResumeAdmin));
        var (janeCreateResponse, janeCreateRequest) = BuildRequest(janeContext, new { OwnerUserId = (string?)null, IsFeatured = false, Payload = SamplePayload }, "POST");
        await _api.Create(janeCreateRequest, janeContext);

        var bobContext = TestFunctionContextFactory.Create(TestFunctionContextFactory.CreateUser("bob", AccountRoles.ResumeAdmin));
        var (bobCreateResponse, bobCreateRequest) = BuildRequest(bobContext, new { OwnerUserId = (string?)null, IsFeatured = false, Payload = SamplePayload }, "POST");
        await _api.Create(bobCreateRequest, bobContext);

        var (listResponse, listRequest) = BuildRequest(janeContext);
        var result = await _api.ListMine(listRequest, janeContext);
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);

        var resumes = await ReadBody<List<ResumeDto>>((TestHttpResponseData)result);
        Assert.NotNull(resumes);
        Assert.Single(resumes!);
        Assert.Equal("jane", resumes![0].OwnerUserId);
    }

    [Fact]
    public async Task Create_Returns401_WhenNotLoggedIn()
    {
        var context = TestFunctionContextFactory.Create();
        var (_, request) = BuildRequest(context, new { OwnerUserId = (string?)null, IsFeatured = false, Payload = SamplePayload }, "POST");

        var result = await _api.Create(request, context);

        Assert.Equal(HttpStatusCode.Unauthorized, result.StatusCode);
    }

    [Fact]
    public async Task Create_Returns201_AndOwnsCaller_ForResumeAdmin()
    {
        var context = TestFunctionContextFactory.Create(TestFunctionContextFactory.CreateUser("jane", AccountRoles.ResumeAdmin));
        var (response, request) = BuildRequest(context, new { OwnerUserId = (string?)null, IsFeatured = false, Payload = SamplePayload }, "POST");

        var result = await _api.Create(request, context);

        Assert.Equal(HttpStatusCode.Created, result.StatusCode);
        var dto = await ReadBody<ResumeDto>((TestHttpResponseData)result);
        Assert.Equal("jane", dto!.OwnerUserId);
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
    public async Task Create_HonorsRequestedOwner_ForSuperAdmin()
    {
        var context = TestFunctionContextFactory.Create(TestFunctionContextFactory.CreateUser("root", AccountRoles.SuperAdmin));
        var (response, request) = BuildRequest(context, new { OwnerUserId = "bob", IsFeatured = false, Payload = SamplePayload }, "POST");

        var result = await _api.Create(request, context);

        Assert.Equal(HttpStatusCode.Created, result.StatusCode);
        var dto = await ReadBody<ResumeDto>((TestHttpResponseData)result);
        Assert.Equal("bob", dto!.OwnerUserId);
    }

    [Fact]
    public async Task Update_Returns403_WhenNotOwnerAndNotSuperAdmin()
    {
        var janeContext = TestFunctionContextFactory.Create(TestFunctionContextFactory.CreateUser("jane", AccountRoles.ResumeAdmin));
        var (createResponse, createRequest) = BuildRequest(janeContext, new { OwnerUserId = (string?)null, IsFeatured = false, Payload = SamplePayload }, "POST");
        var created = await ReadBody<ResumeDto>((TestHttpResponseData)await _api.Create(createRequest, janeContext));

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
        var created = await ReadBody<ResumeDto>((TestHttpResponseData)await _api.Create(createRequest, janeContext));

        var superContext = TestFunctionContextFactory.Create(TestFunctionContextFactory.CreateUser("root", AccountRoles.SuperAdmin));
        var (_, updateRequest) = BuildRequest(superContext, new { OwnerUserId = (string?)null, IsFeatured = false, Payload = new { FName = "Updated" } }, "PUT");

        var result = await _api.Update(updateRequest, superContext, created!.Id);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
    }

    [Fact]
    public async Task Update_Returns404_ForUnknownId()
    {
        var context = TestFunctionContextFactory.Create(TestFunctionContextFactory.CreateUser("jane", AccountRoles.ResumeAdmin));
        var (_, request) = BuildRequest(context, new { OwnerUserId = (string?)null, IsFeatured = false, Payload = SamplePayload }, "PUT");

        var result = await _api.Update(request, context, "does-not-exist");

        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
    }

    [Fact]
    public async Task Delete_Returns403_WhenNotOwnerAndNotSuperAdmin()
    {
        var janeContext = TestFunctionContextFactory.Create(TestFunctionContextFactory.CreateUser("jane", AccountRoles.ResumeAdmin));
        var (createResponse, createRequest) = BuildRequest(janeContext, new { OwnerUserId = (string?)null, IsFeatured = false, Payload = SamplePayload }, "POST");
        var created = await ReadBody<ResumeDto>((TestHttpResponseData)await _api.Create(createRequest, janeContext));

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
        var created = await ReadBody<ResumeDto>((TestHttpResponseData)await _api.Create(createRequest, janeContext));

        var (_, deleteRequest) = BuildRequest(janeContext, method: "DELETE");

        var result = await _api.Delete(deleteRequest, janeContext, created!.Id);

        Assert.Equal(HttpStatusCode.NoContent, result.StatusCode);
    }

    [Fact]
    public async Task Create_MarkingFeatured_UnsetsPreviouslyFeaturedResumeForSameOwner()
    {
        var context = TestFunctionContextFactory.Create(TestFunctionContextFactory.CreateUser("jane", AccountRoles.ResumeAdmin));
        var (firstResponse, firstRequest) = BuildRequest(context, new { OwnerUserId = (string?)null, IsFeatured = true, Payload = SamplePayload }, "POST");
        var first = await ReadBody<ResumeDto>((TestHttpResponseData)await _api.Create(firstRequest, context));
        Assert.True(first!.IsFeatured);

        var (secondResponse, secondRequest) = BuildRequest(context, new { OwnerUserId = (string?)null, IsFeatured = true, Payload = SamplePayload }, "POST");
        var second = await ReadBody<ResumeDto>((TestHttpResponseData)await _api.Create(secondRequest, context));
        Assert.True(second!.IsFeatured);

        var stillFeatured = await _store.FindByIdAsync(first.Id);
        Assert.False(stillFeatured!.IsFeatured);
    }
}
