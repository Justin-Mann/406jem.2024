using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
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
    private readonly FakeResumeBlobStore _blobStore = new();
    private readonly FakeResumeSnapshotStore _snapshotStore = new();
    private readonly ResumeAdminApi _api;

    public ResumeAdminApiTests()
    {
        _api = new ResumeAdminApi(Substitute.For<ILogger<ResumeAdminApi>>(), _store, _blobStore, _snapshotStore);
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

    private static byte[] ValidPdfBytes => Encoding.ASCII.GetBytes("%PDF-1.4\n%%EOF");

    private static (TestHttpResponseData response, TestHttpRequestData request) BuildUploadRequest(
        FunctionContext context, byte[]? body, string? contentType = "application/pdf", string? fileName = "resume.pdf")
    {
        var response = new TestHttpResponseData(context);
        var headers = new HttpHeadersCollection();
        if (contentType is not null)
        {
            headers.Add("Content-Type", contentType);
        }
        if (fileName is not null)
        {
            headers.Add("X-File-Name", fileName);
        }
        var bodyStream = body is null ? Stream.Null : new MemoryStream(body);
        var request = new TestHttpRequestData(context, response, bodyStream, "POST", headers);
        return (response, request);
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
    public async Task Publish_Returns401_WhenNotLoggedIn()
    {
        var context = TestFunctionContextFactory.Create();
        var (_, request) = BuildRequest(context, method: "POST");

        var result = await _api.Publish(request, context, "does-not-exist");

        Assert.Equal(HttpStatusCode.Unauthorized, result.StatusCode);
    }

    [Fact]
    public async Task Publish_Returns404_ForUnknownId()
    {
        var context = TestFunctionContextFactory.Create(TestFunctionContextFactory.CreateUser("jane", AccountRoles.ResumeAdmin));
        var (_, request) = BuildRequest(context, method: "POST");

        var result = await _api.Publish(request, context, "does-not-exist");

        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
    }

    [Fact]
    public async Task Publish_Returns403_WhenNotOwnerAndNotSuperAdmin()
    {
        var janeContext = TestFunctionContextFactory.Create(TestFunctionContextFactory.CreateUser("jane", AccountRoles.ResumeAdmin));
        var (_, uploadRequest) = BuildUploadRequest(janeContext, ValidPdfBytes);
        var created = await ReadBody<ResumeDto>((TestHttpResponseData)await _api.Upload(uploadRequest, janeContext));

        var bobContext = TestFunctionContextFactory.Create(TestFunctionContextFactory.CreateUser("bob", AccountRoles.ResumeAdmin));
        var (_, publishRequest) = BuildRequest(bobContext, method: "POST");

        var result = await _api.Publish(publishRequest, bobContext, created!.Id);

        Assert.Equal(HttpStatusCode.Forbidden, result.StatusCode);
    }

    [Fact]
    public async Task Publish_TransitionsDraftToPublished_ForOwner()
    {
        var context = TestFunctionContextFactory.Create(TestFunctionContextFactory.CreateUser("jane", AccountRoles.ResumeAdmin));
        var (_, uploadRequest) = BuildUploadRequest(context, ValidPdfBytes);
        var created = await ReadBody<ResumeDto>((TestHttpResponseData)await _api.Upload(uploadRequest, context));
        Assert.Equal("Draft", created!.Status);

        var (_, publishRequest) = BuildRequest(context, method: "POST");
        var result = await _api.Publish(publishRequest, context, created.Id);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        var dto = await ReadBody<ResumeDto>((TestHttpResponseData)result);
        Assert.Equal("Published", dto!.Status);

        var stored = await _store.FindByIdAsync(created.Id);
        Assert.Equal(ResumeFunctions.Auth.Models.ResumeEntity.StatusPublished, stored!.Status);
    }

    [Fact]
    public async Task Publish_Returns200_ForSuperAdmin_EvenWhenNotOwner()
    {
        var janeContext = TestFunctionContextFactory.Create(TestFunctionContextFactory.CreateUser("jane", AccountRoles.ResumeAdmin));
        var (_, uploadRequest) = BuildUploadRequest(janeContext, ValidPdfBytes);
        var created = await ReadBody<ResumeDto>((TestHttpResponseData)await _api.Upload(uploadRequest, janeContext));

        var superContext = TestFunctionContextFactory.Create(TestFunctionContextFactory.CreateUser("root", AccountRoles.SuperAdmin));
        var (_, publishRequest) = BuildRequest(superContext, method: "POST");

        var result = await _api.Publish(publishRequest, superContext, created!.Id);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        var dto = await ReadBody<ResumeDto>((TestHttpResponseData)result);
        Assert.Equal("Published", dto!.Status);
    }

    [Fact]
    public async Task Publish_IsNoOp_WhenAlreadyPublished()
    {
        var context = TestFunctionContextFactory.Create(TestFunctionContextFactory.CreateUser("jane", AccountRoles.ResumeAdmin));
        var (createResponse, createRequest) = BuildRequest(context, new { OwnerUserId = (string?)null, IsFeatured = false, Payload = SamplePayload }, "POST");
        var created = await ReadBody<ResumeDto>((TestHttpResponseData)await _api.Create(createRequest, context));
        Assert.Equal("Published", created!.Status);

        var beforePublish = await _store.FindByIdAsync(created.Id);
        var originalUpdatedAt = beforePublish!.UpdatedAtUtc;

        var (_, publishRequest) = BuildRequest(context, method: "POST");
        var result = await _api.Publish(publishRequest, context, created.Id);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        var dto = await ReadBody<ResumeDto>((TestHttpResponseData)result);
        Assert.Equal("Published", dto!.Status);

        var stored = await _store.FindByIdAsync(created.Id);
        Assert.Equal(originalUpdatedAt, stored!.UpdatedAtUtc);
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
    public async Task Delete_RemovesOwnersSnapshot_WhenDeletedResumeWasFeatured()
    {
        var context = TestFunctionContextFactory.Create(TestFunctionContextFactory.CreateUser("jane", AccountRoles.ResumeAdmin));
        var (_, createRequest) = BuildRequest(context, new { OwnerUserId = (string?)null, IsFeatured = true, Payload = SamplePayload }, "POST");
        var created = await ReadBody<ResumeDto>((TestHttpResponseData)await _api.Create(createRequest, context));
        Assert.NotNull(await _snapshotStore.GetAsync("jane"));

        var (_, deleteRequest) = BuildRequest(context, method: "DELETE");
        var result = await _api.Delete(deleteRequest, context, created!.Id);

        Assert.Equal(HttpStatusCode.NoContent, result.StatusCode);
        Assert.Null(await _snapshotStore.GetAsync("jane"));
    }

    [Fact]
    public async Task Delete_LeavesOwnersSnapshotIntact_WhenDeletedResumeWasNotFeatured()
    {
        var context = TestFunctionContextFactory.Create(TestFunctionContextFactory.CreateUser("jane", AccountRoles.ResumeAdmin));
        var (_, featuredRequest) = BuildRequest(context, new { OwnerUserId = (string?)null, IsFeatured = true, Payload = SamplePayload }, "POST");
        await _api.Create(featuredRequest, context);

        var (_, otherRequest) = BuildRequest(context, new { OwnerUserId = (string?)null, IsFeatured = false, Payload = SamplePayload }, "POST");
        var other = await ReadBody<ResumeDto>((TestHttpResponseData)await _api.Create(otherRequest, context));

        var (_, deleteRequest) = BuildRequest(context, method: "DELETE");
        var result = await _api.Delete(deleteRequest, context, other!.Id);

        Assert.Equal(HttpStatusCode.NoContent, result.StatusCode);
        Assert.NotNull(await _snapshotStore.GetAsync("jane"));
    }

    [Fact]
    public async Task Delete_Returns204_EvenWhenSnapshotDeleteFails()
    {
        var context = TestFunctionContextFactory.Create(TestFunctionContextFactory.CreateUser("jane", AccountRoles.ResumeAdmin));
        var (_, createRequest) = BuildRequest(context, new { OwnerUserId = (string?)null, IsFeatured = true, Payload = SamplePayload }, "POST");
        var created = await ReadBody<ResumeDto>((TestHttpResponseData)await _api.Create(createRequest, context));

        _snapshotStore.ThrowOnDelete = true;
        var (_, deleteRequest) = BuildRequest(context, method: "DELETE");
        var result = await _api.Delete(deleteRequest, context, created!.Id);

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

    [Fact]
    public async Task Create_MarkingFeatured_WritesFallbackSnapshotForOwner()
    {
        var context = TestFunctionContextFactory.Create(TestFunctionContextFactory.CreateUser("jane", AccountRoles.ResumeAdmin));
        var (_, request) = BuildRequest(context, new { OwnerUserId = (string?)null, IsFeatured = true, Payload = SamplePayload }, "POST");

        await _api.Create(request, context);

        var snapshot = await _snapshotStore.GetAsync("jane");
        Assert.NotNull(snapshot);
        var payload = JsonSerializer.Deserialize<JsonElement>(snapshot!);
        Assert.Equal("Jane", payload.GetProperty("FName").GetString());
    }

    [Fact]
    public async Task Create_NotFeatured_DoesNotWriteSnapshot()
    {
        var context = TestFunctionContextFactory.Create(TestFunctionContextFactory.CreateUser("jane", AccountRoles.ResumeAdmin));
        var (_, request) = BuildRequest(context, new { OwnerUserId = (string?)null, IsFeatured = false, Payload = SamplePayload }, "POST");

        await _api.Create(request, context);

        Assert.Null(await _snapshotStore.GetAsync("jane"));
    }

    [Fact]
    public async Task Update_MarkingFeatured_RefreshesSnapshotWithLatestContent()
    {
        var context = TestFunctionContextFactory.Create(TestFunctionContextFactory.CreateUser("jane", AccountRoles.ResumeAdmin));
        var (_, createRequest) = BuildRequest(context, new { OwnerUserId = (string?)null, IsFeatured = true, Payload = SamplePayload }, "POST");
        var created = await ReadBody<ResumeDto>((TestHttpResponseData)await _api.Create(createRequest, context));

        var (_, updateRequest) = BuildRequest(context, new { OwnerUserId = (string?)null, IsFeatured = true, Payload = new { FName = "Updated", LName = "Doe" } }, "PUT");
        await _api.Update(updateRequest, context, created!.Id);

        var snapshot = await _snapshotStore.GetAsync("jane");
        Assert.NotNull(snapshot);
        var payload = JsonSerializer.Deserialize<JsonElement>(snapshot!);
        Assert.Equal("Updated", payload.GetProperty("FName").GetString());
    }

    [Fact]
    public async Task Create_MarkingFeatured_Returns201_EvenWhenSnapshotWriteFails()
    {
        _snapshotStore.ThrowOnSave = true;
        var context = TestFunctionContextFactory.Create(TestFunctionContextFactory.CreateUser("jane", AccountRoles.ResumeAdmin));
        var (_, request) = BuildRequest(context, new { OwnerUserId = (string?)null, IsFeatured = true, Payload = SamplePayload }, "POST");

        var result = await _api.Create(request, context);

        Assert.Equal(HttpStatusCode.Created, result.StatusCode);
        var stored = await _store.ListByOwnerAsync("jane");
        Assert.Single(stored);
        Assert.True(stored[0].IsFeatured);
    }

    [Fact]
    public async Task Upload_Returns401_WhenNotLoggedIn()
    {
        var context = TestFunctionContextFactory.Create();
        var (_, request) = BuildUploadRequest(context, ValidPdfBytes);

        var result = await _api.Upload(request, context);

        Assert.Equal(HttpStatusCode.Unauthorized, result.StatusCode);
    }

    [Fact]
    public async Task Upload_Returns403_ForVisitor()
    {
        var context = TestFunctionContextFactory.Create(TestFunctionContextFactory.CreateUser("jane", AccountRoles.Visitor));
        var (_, request) = BuildUploadRequest(context, ValidPdfBytes);

        var result = await _api.Upload(request, context);

        Assert.Equal(HttpStatusCode.Forbidden, result.StatusCode);
    }

    [Fact]
    public async Task Upload_Returns201_StoresBlob_AndCreatesDraftResume_ForResumeAdmin()
    {
        var context = TestFunctionContextFactory.Create(TestFunctionContextFactory.CreateUser("jane", AccountRoles.ResumeAdmin));
        var (_, request) = BuildUploadRequest(context, ValidPdfBytes, fileName: "jane-resume.pdf");

        var result = await _api.Upload(request, context);

        Assert.Equal(HttpStatusCode.Created, result.StatusCode);
        var dto = await ReadBody<ResumeDto>((TestHttpResponseData)result);
        Assert.Equal("jane", dto!.OwnerUserId);
        Assert.Equal("Draft", dto.Status);
        Assert.False(dto.IsFeatured);
        Assert.Equal("jane-resume.pdf", dto.OriginalFileName);
        Assert.Equal("application/pdf", dto.ContentType);
        Assert.Equal(ValidPdfBytes.Length, dto.FileSizeBytes);

        var stored = await _store.FindByIdAsync(dto.Id);
        Assert.NotNull(stored);
        Assert.NotNull(stored!.BlobPath);
        Assert.True(_blobStore.Blobs.ContainsKey(stored.BlobPath!));
        Assert.Equal(ValidPdfBytes, _blobStore.Blobs[stored.BlobPath!].Content);
    }

    [Fact]
    public async Task Upload_Returns201_ForSuperAdmin()
    {
        var context = TestFunctionContextFactory.Create(TestFunctionContextFactory.CreateUser("root", AccountRoles.SuperAdmin));
        var (_, request) = BuildUploadRequest(context, ValidPdfBytes);

        var result = await _api.Upload(request, context);

        Assert.Equal(HttpStatusCode.Created, result.StatusCode);
    }

    [Fact]
    public async Task Upload_Returns400_ForNonPdfContentType()
    {
        var context = TestFunctionContextFactory.Create(TestFunctionContextFactory.CreateUser("jane", AccountRoles.ResumeAdmin));
        var (_, request) = BuildUploadRequest(context, Encoding.UTF8.GetBytes("not a pdf"), contentType: "image/png");

        var result = await _api.Upload(request, context);

        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Empty(_blobStore.Blobs);
    }

    [Fact]
    public async Task Upload_Returns400_WhenContentDoesNotStartWithPdfMagicBytes()
    {
        var context = TestFunctionContextFactory.Create(TestFunctionContextFactory.CreateUser("jane", AccountRoles.ResumeAdmin));
        var (_, request) = BuildUploadRequest(context, Encoding.UTF8.GetBytes("definitely not a pdf"));

        var result = await _api.Upload(request, context);

        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Empty(_blobStore.Blobs);
    }

    [Fact]
    public async Task Upload_Returns400_WhenBodyIsEmpty()
    {
        var context = TestFunctionContextFactory.Create(TestFunctionContextFactory.CreateUser("jane", AccountRoles.ResumeAdmin));
        var (_, request) = BuildUploadRequest(context, Array.Empty<byte>());

        var result = await _api.Upload(request, context);

        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
    }

    [Fact]
    public async Task Upload_Returns400_WhenFileExceedsSizeCap()
    {
        var context = TestFunctionContextFactory.Create(TestFunctionContextFactory.CreateUser("jane", AccountRoles.ResumeAdmin));
        var oversized = new byte[10 * 1024 * 1024 + 1];
        Encoding.ASCII.GetBytes("%PDF-1.4\n").CopyTo(oversized, 0);
        var (_, request) = BuildUploadRequest(context, oversized);

        var result = await _api.Upload(request, context);

        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Empty(_blobStore.Blobs);
    }

    [Fact]
    public async Task Upload_OnlyOwnerCanSeeTheirDraftResume()
    {
        var janeContext = TestFunctionContextFactory.Create(TestFunctionContextFactory.CreateUser("jane", AccountRoles.ResumeAdmin));
        var (_, uploadRequest) = BuildUploadRequest(janeContext, ValidPdfBytes);
        await _api.Upload(uploadRequest, janeContext);

        var bobContext = TestFunctionContextFactory.Create(TestFunctionContextFactory.CreateUser("bob", AccountRoles.ResumeAdmin));
        var (_, listRequest) = BuildRequest(bobContext);
        var bobList = await ReadBody<List<ResumeDto>>((TestHttpResponseData)await _api.ListMine(listRequest, bobContext));

        Assert.Empty(bobList!);

        var (_, janeListRequest) = BuildRequest(janeContext);
        var janeList = await ReadBody<List<ResumeDto>>((TestHttpResponseData)await _api.ListMine(janeListRequest, janeContext));
        Assert.Single(janeList!);
        Assert.Equal("Draft", janeList![0].Status);
    }
}
