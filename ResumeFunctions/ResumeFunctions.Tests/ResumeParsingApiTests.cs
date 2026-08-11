using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using NSubstitute;
using ResumeFunctions.Auth;
using ResumeFunctions.Auth.Dtos;
using ResumeFunctions.Auth.Models;
using ResumeFunctions.Auth.Parsing;
using ResumeFunctions.Tests.Helpers;
using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;

namespace ResumeFunctions.Tests;

public class ResumeParsingApiTests
{
    private static readonly string FixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "sample-resume.pdf");

    private const string WellFormedAiJson = """
        {
          "fName": "Jane",
          "mName": null,
          "lName": "Doe",
          "position": "Software Engineer",
          "subtitle": "Full-stack Developer",
          "simpleGoal": "Build great software",
          "profile": ["Experienced engineer", "Team player"],
          "workExperience": [
            {
              "companyName": "Acme Corp",
              "position": "Senior Engineer",
              "startDate": "2020",
              "endDate": "Present",
              "bulletList": ["Built things", "Shipped things"],
              "note": null
            }
          ],
          "contact": [
            { "type": "Email", "displayValue": null, "url": null, "mailTo": "jane@example.com" }
          ],
          "education": [
            { "name": "State University", "degree": true, "degreeName": "BS Computer Science", "degreeYear": "2018", "areasOfStudy": ["CS"] }
          ],
          "customSections": [
            { "name": "Languages", "customItems": [ { "value": "C#", "type": "Lang" } ] }
          ]
        }
        """;

    private readonly FakeResumeStore _resumeStore = new();
    private readonly FakeResumeBlobStore _blobStore = new();

    private static (TestHttpResponseData response, TestHttpRequestData request) BuildRequest(FunctionContext context, string method = "POST")
    {
        var response = new TestHttpResponseData(context);
        var request = new TestHttpRequestData(context, response, Stream.Null, method);
        return (response, request);
    }

    private static async Task<T?> ReadBody<T>(TestHttpResponseData response)
    {
        response.Body.Position = 0;
        return await JsonSerializer.DeserializeAsync<T>(response.Body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    private ResumeParsingApi BuildApi(IResumeAiClient aiClient, IPdfTextExtractor? pdfTextExtractor = null) =>
        new(Substitute.For<ILogger<ResumeParsingApi>>(), _resumeStore, _blobStore, pdfTextExtractor ?? new PdfPigTextExtractor(), aiClient);

    private async Task<ResumeEntity> SeedDraftResumeAsync(string owner)
    {
        var pdfBytes = await File.ReadAllBytesAsync(FixturePath);
        var blobName = $"{owner}/{Guid.NewGuid()}.pdf";
        await _blobStore.UploadAsync(blobName, new MemoryStream(pdfBytes), "application/pdf");

        var resume = new ResumeEntity
        {
            OwnerUserId = owner,
            Status = ResumeEntity.StatusDraft,
            PayloadJson = string.Empty,
            BlobPath = blobName,
            OriginalFileName = "resume.pdf",
            ContentType = "application/pdf",
            FileSizeBytes = pdfBytes.Length,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
        };
        await _resumeStore.AddAsync(resume);
        return resume;
    }

    [Fact]
    public async Task Parse_Returns401_WhenNotLoggedIn()
    {
        var context = TestFunctionContextFactory.Create();
        var resume = await SeedDraftResumeAsync("jane");
        var (_, request) = BuildRequest(context);
        var api = BuildApi(FakeResumeAiClient.ReturningJson(WellFormedAiJson));

        var result = await api.Parse(request, context, resume.RowKey);

        Assert.Equal(HttpStatusCode.Unauthorized, result.StatusCode);
    }

    [Fact]
    public async Task Parse_Returns403_ForVisitor()
    {
        var context = TestFunctionContextFactory.Create(TestFunctionContextFactory.CreateUser("jane", AccountRoles.Visitor));
        var resume = await SeedDraftResumeAsync("jane");
        var (_, request) = BuildRequest(context);
        var api = BuildApi(FakeResumeAiClient.ReturningJson(WellFormedAiJson));

        var result = await api.Parse(request, context, resume.RowKey);

        Assert.Equal(HttpStatusCode.Forbidden, result.StatusCode);
    }

    [Fact]
    public async Task Parse_Returns404_ForUnknownId()
    {
        var context = TestFunctionContextFactory.Create(TestFunctionContextFactory.CreateUser("jane", AccountRoles.ResumeAdmin));
        var (_, request) = BuildRequest(context);
        var api = BuildApi(FakeResumeAiClient.ReturningJson(WellFormedAiJson));

        var result = await api.Parse(request, context, "does-not-exist");

        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
    }

    [Fact]
    public async Task Parse_Returns403_WhenNotOwnerAndNotSuperAdmin()
    {
        var resume = await SeedDraftResumeAsync("jane");
        var bobContext = TestFunctionContextFactory.Create(TestFunctionContextFactory.CreateUser("bob", AccountRoles.ResumeAdmin));
        var (_, request) = BuildRequest(bobContext);
        var api = BuildApi(FakeResumeAiClient.ReturningJson(WellFormedAiJson));

        var result = await api.Parse(request, bobContext, resume.RowKey);

        Assert.Equal(HttpStatusCode.Forbidden, result.StatusCode);
    }

    [Fact]
    public async Task Parse_Returns400_WhenResumeIsNotDraft()
    {
        var context = TestFunctionContextFactory.Create(TestFunctionContextFactory.CreateUser("jane", AccountRoles.ResumeAdmin));
        var resume = await SeedDraftResumeAsync("jane");
        resume.Status = ResumeEntity.StatusPublished;
        await _resumeStore.UpdateAsync(resume);
        var (_, request) = BuildRequest(context);
        var api = BuildApi(FakeResumeAiClient.ReturningJson(WellFormedAiJson));

        var result = await api.Parse(request, context, resume.RowKey);

        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
    }

    [Fact]
    public async Task Parse_Returns400_WhenResumeHasNoUploadedPdf()
    {
        var context = TestFunctionContextFactory.Create(TestFunctionContextFactory.CreateUser("jane", AccountRoles.ResumeAdmin));
        var resume = new ResumeEntity
        {
            OwnerUserId = "jane",
            Status = ResumeEntity.StatusDraft,
            PayloadJson = string.Empty,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
        };
        await _resumeStore.AddAsync(resume);
        var (_, request) = BuildRequest(context);
        var api = BuildApi(FakeResumeAiClient.ReturningJson(WellFormedAiJson));

        var result = await api.Parse(request, context, resume.RowKey);

        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
    }

    [Fact]
    public async Task Parse_Returns200_AndPopulatesPayload_OnSuccess()
    {
        var context = TestFunctionContextFactory.Create(TestFunctionContextFactory.CreateUser("jane", AccountRoles.ResumeAdmin));
        var resume = await SeedDraftResumeAsync("jane");
        var aiClient = FakeResumeAiClient.ReturningJson(WellFormedAiJson);
        var (_, request) = BuildRequest(context);
        var api = BuildApi(aiClient);

        var result = await api.Parse(request, context, resume.RowKey);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        var dto = await ReadBody<ParseResumeResponse>((TestHttpResponseData)result);
        Assert.True(dto!.ParseSucceeded);
        Assert.Null(dto.Message);
        Assert.NotNull(dto.Resume.Payload);
        Assert.Equal("Jane", dto.Resume.Payload!.FName);
        Assert.Equal("Doe", dto.Resume.Payload.LName);
        Assert.Equal("Draft", dto.Resume.Status); // never auto-publishes

        // The extractor actually ran against the fixture PDF and handed real text to the AI client.
        Assert.False(string.IsNullOrWhiteSpace(aiClient.LastResumeTextReceived));

        var stored = await _resumeStore.FindByIdAsync(resume.RowKey);
        Assert.NotEqual(string.Empty, stored!.PayloadJson);
        Assert.Equal(ResumeEntity.StatusDraft, stored.Status);
    }

    [Fact]
    public async Task Parse_Returns200_ForSuperAdmin_EvenWhenNotOwner()
    {
        var resume = await SeedDraftResumeAsync("jane");
        var superContext = TestFunctionContextFactory.Create(TestFunctionContextFactory.CreateUser("root", AccountRoles.SuperAdmin));
        var (_, request) = BuildRequest(superContext);
        var api = BuildApi(FakeResumeAiClient.ReturningJson(WellFormedAiJson));

        var result = await api.Parse(request, superContext, resume.RowKey);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
    }

    [Fact]
    public async Task Parse_LeavesResumeInDraftWithEmptyPayload_WhenAiCallFails()
    {
        var context = TestFunctionContextFactory.Create(TestFunctionContextFactory.CreateUser("jane", AccountRoles.ResumeAdmin));
        var resume = await SeedDraftResumeAsync("jane");
        var (_, request) = BuildRequest(context);
        var api = BuildApi(FakeResumeAiClient.Failing("AI extraction failed. Try again later."));

        var result = await api.Parse(request, context, resume.RowKey);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode); // a parse attempt that fails is not a request error
        var dto = await ReadBody<ParseResumeResponse>((TestHttpResponseData)result);
        Assert.False(dto!.ParseSucceeded);
        Assert.False(string.IsNullOrWhiteSpace(dto.Message));
        Assert.Equal("Draft", dto.Resume.Status);
        Assert.Null(dto.Resume.Payload);

        var stored = await _resumeStore.FindByIdAsync(resume.RowKey);
        Assert.Equal(string.Empty, stored!.PayloadJson);
        Assert.Equal(ResumeEntity.StatusDraft, stored.Status);
    }

    [Fact]
    public async Task Parse_LeavesResumeInDraftWithEmptyPayload_WhenAiReturnsMalformedJson()
    {
        var context = TestFunctionContextFactory.Create(TestFunctionContextFactory.CreateUser("jane", AccountRoles.ResumeAdmin));
        var resume = await SeedDraftResumeAsync("jane");
        var (_, request) = BuildRequest(context);
        var api = BuildApi(FakeResumeAiClient.ReturningJson("{ this is not valid json"));

        var result = await api.Parse(request, context, resume.RowKey);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        var dto = await ReadBody<ParseResumeResponse>((TestHttpResponseData)result);
        Assert.False(dto!.ParseSucceeded);
        Assert.False(string.IsNullOrWhiteSpace(dto.Message));
        Assert.Equal("Draft", dto.Resume.Status);

        var stored = await _resumeStore.FindByIdAsync(resume.RowKey);
        Assert.Equal(string.Empty, stored!.PayloadJson);
    }

    [Fact]
    public async Task Parse_LeavesResumeInDraftWithEmptyPayload_WhenPdfTextExtractionFails()
    {
        var context = TestFunctionContextFactory.Create(TestFunctionContextFactory.CreateUser("jane", AccountRoles.ResumeAdmin));

        // Seed a resume whose "PDF" blob is not actually a valid PDF, so the real
        // PdfPigTextExtractor throws — exercising the extraction-failure fallback path.
        var blobName = "jane/not-a-real-pdf.pdf";
        await _blobStore.UploadAsync(blobName, new MemoryStream(Encoding.UTF8.GetBytes("definitely not a pdf")), "application/pdf");
        var resume = new ResumeEntity
        {
            OwnerUserId = "jane",
            Status = ResumeEntity.StatusDraft,
            PayloadJson = string.Empty,
            BlobPath = blobName,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
        };
        await _resumeStore.AddAsync(resume);

        var (_, request) = BuildRequest(context);
        var api = BuildApi(FakeResumeAiClient.ReturningJson(WellFormedAiJson));

        var result = await api.Parse(request, context, resume.RowKey);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        var dto = await ReadBody<ParseResumeResponse>((TestHttpResponseData)result);
        Assert.False(dto!.ParseSucceeded);
        Assert.False(string.IsNullOrWhiteSpace(dto.Message));

        var stored = await _resumeStore.FindByIdAsync(resume.RowKey);
        Assert.Equal(string.Empty, stored!.PayloadJson);
    }
}
