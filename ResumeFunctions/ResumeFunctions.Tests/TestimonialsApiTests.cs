using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using NSubstitute;
using ResumeFunctions.Auth;
using ResumeFunctions.Auth.Models;
using ResumeFunctions.Auth.Storage;
using ResumeFunctions.Tests.Helpers;
using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;

namespace ResumeFunctions.Tests;

public class TestimonialsApiTests
{
    private readonly ITestimonialStore _store = Substitute.For<ITestimonialStore>();
    private readonly TestimonialsApi _api;

    public TestimonialsApiTests()
    {
        _api = new TestimonialsApi(Substitute.For<ILogger<TestimonialsApi>>(), _store);
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

    [Fact]
    public async Task List_Returns200_WithoutRequiringAuth()
    {
        _store.ListAsync().Returns(new List<TestimonialEntity>());
        var context = TestFunctionContextFactory.Create();
        var (response, request) = BuildRequest(context);

        var result = await _api.List(request);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
    }

    [Fact]
    public async Task Create_Returns401_WhenNotLoggedIn()
    {
        var context = TestFunctionContextFactory.Create();
        var (response, request) = BuildRequest(context, new { Message = "Great site!" }, "POST");

        var result = await _api.Create(request, context);

        Assert.Equal(HttpStatusCode.Unauthorized, result.StatusCode);
    }

    [Fact]
    public async Task Create_Returns201_WhenLoggedIn()
    {
        var context = TestFunctionContextFactory.Create(TestFunctionContextFactory.CreateUser("jane", AccountRoles.Visitor));
        var (response, request) = BuildRequest(context, new { Message = "Great site!" }, "POST");

        var result = await _api.Create(request, context);

        Assert.Equal(HttpStatusCode.Created, result.StatusCode);
        await _store.Received(1).AddAsync(Arg.Is<TestimonialEntity>(t => t.AuthorUsername == "jane" && t.Message == "Great site!"));
    }

    [Fact]
    public async Task Create_Returns400_ForEmptyMessage()
    {
        var context = TestFunctionContextFactory.Create(TestFunctionContextFactory.CreateUser("jane", AccountRoles.Visitor));
        var (response, request) = BuildRequest(context, new { Message = "   " }, "POST");

        var result = await _api.Create(request, context);

        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
    }

    [Fact]
    public async Task Delete_Returns401_WhenNotLoggedIn()
    {
        var context = TestFunctionContextFactory.Create();
        var (response, request) = BuildRequest(context, method: "DELETE");

        var result = await _api.Delete(request, context, "some-id");

        Assert.Equal(HttpStatusCode.Unauthorized, result.StatusCode);
    }

    [Fact]
    public async Task Delete_Returns403_WhenLoggedInAsVisitorNotAdmin()
    {
        var context = TestFunctionContextFactory.Create(TestFunctionContextFactory.CreateUser("jane", AccountRoles.Visitor));
        var (response, request) = BuildRequest(context, method: "DELETE");

        var result = await _api.Delete(request, context, "some-id");

        Assert.Equal(HttpStatusCode.Forbidden, result.StatusCode);
    }

    [Fact]
    public async Task Delete_Returns204_WhenLoggedInAsAdmin()
    {
        _store.DeleteAsync("some-id").Returns(true);
        var context = TestFunctionContextFactory.Create(TestFunctionContextFactory.CreateUser("admin", AccountRoles.Admin));
        var (response, request) = BuildRequest(context, method: "DELETE");

        var result = await _api.Delete(request, context, "some-id");

        Assert.Equal(HttpStatusCode.NoContent, result.StatusCode);
    }
}
