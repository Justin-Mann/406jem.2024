using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using ResumeFunctions.Auth;
using ResumeFunctions.Auth.Dtos;
using ResumeFunctions.Auth.Middleware;
using ResumeFunctions.Auth.Models;
using ResumeFunctions.Auth.Parsing;
using ResumeFunctions.Auth.Storage;
using ResumeFunctions.Models;

namespace ResumeFunctions
{
    /// <summary>
    /// LLM-based extraction (#30) of structured resume data from a Draft resume's uploaded PDF
    /// (#29). Populates the resume's Payload but never changes its Status away from Draft — a
    /// separate admin-edit-UI issue is where a human reviews/corrects the result before
    /// publishing. Never auto-publishes LLM output.
    /// </summary>
    public class ResumeParsingApi
    {
        private static readonly JsonSerializerOptions PayloadDeserializeOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() },
        };

        private readonly ILogger<ResumeParsingApi> _logger;
        private readonly IResumeStore _resumeStore;
        private readonly IResumeBlobStore _resumeBlobStore;
        private readonly IPdfTextExtractor _pdfTextExtractor;
        private readonly IResumeAiClient _resumeAiClient;
        private readonly IResumeSnapshotStore _resumeSnapshotStore;

        public ResumeParsingApi(
            ILogger<ResumeParsingApi> logger,
            IResumeStore resumeStore,
            IResumeBlobStore resumeBlobStore,
            IPdfTextExtractor pdfTextExtractor,
            IResumeAiClient resumeAiClient,
            IResumeSnapshotStore resumeSnapshotStore)
        {
            _logger = logger;
            _resumeStore = resumeStore;
            _resumeBlobStore = resumeBlobStore;
            _pdfTextExtractor = pdfTextExtractor;
            _resumeAiClient = resumeAiClient;
            _resumeSnapshotStore = resumeSnapshotStore;
        }

        [Function("parseResume")]
        public async Task<HttpResponseData> Parse(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "resumes/{id}/parse")] HttpRequestData req,
            FunctionContext context,
            string id)
        {
            var user = context.GetAuthenticatedUser();
            if (user is null || !context.IsInRoleOrHigher(AccountRoles.ResumeAdmin))
            {
                return await HttpResponseHelpers.Forbidden(req, user, "Resume Admin role required.");
            }

            var resume = await _resumeStore.FindByIdAsync(id);
            if (resume is null)
            {
                return await HttpResponseHelpers.ErrorResponse(req, HttpStatusCode.NotFound, "Resume not found.");
            }

            if (!context.IsOwnerOrSuperAdmin(resume.OwnerUserId))
            {
                return await HttpResponseHelpers.ErrorResponse(req, HttpStatusCode.Forbidden, "You can only manage your own resumes.");
            }

            if (resume.Status != ResumeEntity.StatusDraft)
            {
                return await HttpResponseHelpers.ErrorResponse(req, HttpStatusCode.BadRequest, "Only draft resumes can be parsed.");
            }

            if (string.IsNullOrWhiteSpace(resume.BlobPath))
            {
                return await HttpResponseHelpers.ErrorResponse(req, HttpStatusCode.BadRequest, "This resume has no uploaded PDF to parse.");
            }

            var (parseSucceeded, message) = await TryParseAsync(resume);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new ParseResumeResponse(ResumeDtoMapper.ToDto(resume), parseSucceeded, message));
            return response;
        }

        /// <summary>Extracts text, calls the AI, and — on success — updates and persists
        /// <paramref name="resume"/> in place, refreshing its owner's fallback snapshot (#39) if
        /// this resume is the one currently featured. On any failure the resume is left exactly
        /// as it was (still Draft, Payload untouched) and a human-readable reason is returned;
        /// nothing here throws out to the caller.</summary>
        private async Task<(bool ParseSucceeded, string? Message)> TryParseAsync(ResumeEntity resume)
        {
            string text;
            try
            {
                await using var blob = await _resumeBlobStore.DownloadAsync(resume.BlobPath!);
                text = _pdfTextExtractor.ExtractText(blob);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to extract text from resume '{Id}' PDF.", resume.RowKey);
                return (false, "Could not read the uploaded PDF. You can enter the resume details manually.");
            }

            if (string.IsNullOrWhiteSpace(text))
            {
                _logger.LogWarning("Resume '{Id}' PDF contained no extractable text.", resume.RowKey);
                return (false, "No text could be found in the PDF (it may be a scanned image). You can enter the resume details manually.");
            }

            var aiResult = await _resumeAiClient.ExtractResumeJsonAsync(text);
            if (!aiResult.Succeeded || string.IsNullOrWhiteSpace(aiResult.Json))
            {
                _logger.LogWarning("AI extraction failed for resume '{Id}': {Reason}", resume.RowKey, aiResult.ErrorMessage);
                return (false, aiResult.ErrorMessage ?? "Automatic parsing failed. You can enter the resume details manually.");
            }

            DigitalResumeModel? payload;
            try
            {
                payload = JsonSerializer.Deserialize<DigitalResumeModel>(aiResult.Json, PayloadDeserializeOptions);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "AI returned malformed JSON for resume '{Id}'.", resume.RowKey);
                return (false, "The AI returned data that couldn't be understood. You can enter the resume details manually.");
            }

            if (payload is null)
            {
                return (false, "The AI returned no usable data. You can enter the resume details manually.");
            }

            resume.PayloadJson = JsonSerializer.Serialize(payload);
            resume.UpdatedAtUtc = DateTimeOffset.UtcNow;
            await _resumeStore.UpdateAsync(resume);
            await ResumeSnapshotHelper.TrySaveSnapshotAsync(_resumeSnapshotStore, _logger, resume);

            _logger.LogInformation("Resume '{Id}' parsed successfully via AI extraction.", resume.RowKey);
            return (true, null);
        }
    }
}
