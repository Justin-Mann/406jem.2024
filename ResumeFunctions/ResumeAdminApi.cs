using System.Net;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using ResumeFunctions.Auth;
using ResumeFunctions.Auth.Dtos;
using ResumeFunctions.Auth.Middleware;
using ResumeFunctions.Auth.Models;
using ResumeFunctions.Auth.Storage;
using ResumeFunctions.Models;

namespace ResumeFunctions
{
    /// <summary>
    /// Authenticated CRUD over a Resume Admin's own resumes (SuperAdmin may act on any owner's).
    /// Distinct from ResumeApi's public/anonymous endpoints (myResume, resumes) and the
    /// "resumes" Function-key route, which are unrelated to this JWT/role-based surface.
    /// </summary>
    public class ResumeAdminApi
    {
        private const long MaxUploadSizeBytes = 10 * 1024 * 1024; // 10MB, per #29's "reasonable size cap"
        private const string PdfContentType = "application/pdf";
        private static readonly byte[] PdfMagicBytes = "%PDF-"u8.ToArray();

        private readonly ILogger<ResumeAdminApi> _logger;
        private readonly IResumeStore _resumeStore;
        private readonly IResumeBlobStore _resumeBlobStore;

        public ResumeAdminApi(ILogger<ResumeAdminApi> logger, IResumeStore resumeStore, IResumeBlobStore resumeBlobStore)
        {
            _logger = logger;
            _resumeStore = resumeStore;
            _resumeBlobStore = resumeBlobStore;
        }

        [Function("listMyResumes")]
        public async Task<HttpResponseData> ListMine(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "resumes/mine")] HttpRequestData req,
            FunctionContext context)
        {
            var user = context.GetAuthenticatedUser();
            if (user is null || !context.IsInRoleOrHigher(AccountRoles.ResumeAdmin))
            {
                return await Forbidden(req, user);
            }

            var owner = NormalizedUsername(user);
            var resumes = await _resumeStore.ListByOwnerAsync(owner);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(resumes.Select(ToDto));
            return response;
        }

        [Function("createResume")]
        public async Task<HttpResponseData> Create(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "resumes")] HttpRequestData req,
            FunctionContext context)
        {
            var user = context.GetAuthenticatedUser();
            if (user is null || !context.IsInRoleOrHigher(AccountRoles.ResumeAdmin))
            {
                return await Forbidden(req, user);
            }

            var payload = await req.ReadFromJsonAsync<CreateOrUpdateResumeRequest>();
            if (payload?.Payload is null)
            {
                return await BadRequest(req, "Payload is required.");
            }

            var owner = NormalizedUsername(user);
            var isSuperAdmin = user.IsInRole(AccountRoles.SuperAdmin);
            var requestedOwner = string.IsNullOrWhiteSpace(payload.OwnerUserId)
                ? owner
                : payload.OwnerUserId.Trim().ToLowerInvariant();

            if (requestedOwner != owner && !isSuperAdmin)
            {
                return await ErrorResponse(req, HttpStatusCode.Forbidden, "You can only manage your own resumes.");
            }

            var now = DateTimeOffset.UtcNow;
            var resume = new ResumeEntity
            {
                OwnerUserId = requestedOwner,
                IsFeatured = payload.IsFeatured,
                PayloadJson = JsonSerializer.Serialize(payload.Payload),
                Status = ResumeEntity.StatusPublished,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
            };

            if (resume.IsFeatured)
            {
                await ClearOtherFeaturedAsync(requestedOwner, excludeId: null);
            }

            await _resumeStore.AddAsync(resume);
            _logger.LogInformation("Resume created for owner '{Owner}' by '{Username}'.", requestedOwner, owner);

            var response = req.CreateResponse(HttpStatusCode.Created);
            await response.WriteAsJsonAsync(ToDto(resume));
            return response;
        }

        [Function("uploadResume")]
        public async Task<HttpResponseData> Upload(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "resumes/upload")] HttpRequestData req,
            FunctionContext context)
        {
            var user = context.GetAuthenticatedUser();
            if (user is null || !context.IsInRoleOrHigher(AccountRoles.ResumeAdmin))
            {
                return await Forbidden(req, user);
            }

            var contentType = req.Headers.TryGetValues("Content-Type", out var contentTypeValues)
                ? contentTypeValues.FirstOrDefault()
                : null;
            if (!string.Equals(contentType, PdfContentType, StringComparison.OrdinalIgnoreCase))
            {
                return await BadRequest(req, "Only application/pdf uploads are accepted.");
            }

            using var buffer = new MemoryStream();
            await req.Body.CopyToAsync(buffer);

            if (buffer.Length == 0)
            {
                return await BadRequest(req, "The uploaded file is empty.");
            }

            if (buffer.Length > MaxUploadSizeBytes)
            {
                return await BadRequest(req, $"File exceeds the {MaxUploadSizeBytes / (1024 * 1024)}MB size limit.");
            }

            buffer.Position = 0;
            var header = new byte[PdfMagicBytes.Length];
            var bytesRead = await buffer.ReadAsync(header.AsMemory(0, header.Length));
            if (bytesRead < PdfMagicBytes.Length || !header.AsSpan().SequenceEqual(PdfMagicBytes))
            {
                return await BadRequest(req, "The uploaded file is not a valid PDF.");
            }

            var owner = NormalizedUsername(user);
            var fileName = req.Headers.TryGetValues("X-File-Name", out var fileNameValues)
                ? fileNameValues.FirstOrDefault()
                : null;
            var resumeId = Guid.NewGuid().ToString();
            var blobName = $"{owner}/{resumeId}.pdf";

            buffer.Position = 0;
            await _resumeBlobStore.UploadAsync(blobName, buffer, PdfContentType);

            var now = DateTimeOffset.UtcNow;
            var resume = new ResumeEntity
            {
                RowKey = resumeId,
                OwnerUserId = owner,
                IsFeatured = false,
                Status = ResumeEntity.StatusDraft,
                PayloadJson = string.Empty,
                BlobPath = blobName,
                OriginalFileName = string.IsNullOrWhiteSpace(fileName) ? "resume.pdf" : fileName,
                ContentType = PdfContentType,
                FileSizeBytes = buffer.Length,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
            };

            await _resumeStore.AddAsync(resume);
            _logger.LogInformation(
                "Resume PDF uploaded for owner '{Owner}' ({Size} bytes, blob '{BlobPath}').",
                owner, resume.FileSizeBytes, resume.BlobPath);

            var response = req.CreateResponse(HttpStatusCode.Created);
            await response.WriteAsJsonAsync(ToDto(resume));
            return response;
        }

        [Function("updateResume")]
        public async Task<HttpResponseData> Update(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "resumes/{id}")] HttpRequestData req,
            FunctionContext context,
            string id)
        {
            var user = context.GetAuthenticatedUser();
            if (user is null || !context.IsInRoleOrHigher(AccountRoles.ResumeAdmin))
            {
                return await Forbidden(req, user);
            }

            var existing = await _resumeStore.FindByIdAsync(id);
            if (existing is null)
            {
                return await ErrorResponse(req, HttpStatusCode.NotFound, "Resume not found.");
            }

            if (!context.IsOwnerOrSuperAdmin(existing.OwnerUserId))
            {
                return await ErrorResponse(req, HttpStatusCode.Forbidden, "You can only manage your own resumes.");
            }

            var payload = await req.ReadFromJsonAsync<CreateOrUpdateResumeRequest>();
            if (payload?.Payload is null)
            {
                return await BadRequest(req, "Payload is required.");
            }

            existing.PayloadJson = JsonSerializer.Serialize(payload.Payload);
            existing.IsFeatured = payload.IsFeatured;
            existing.UpdatedAtUtc = DateTimeOffset.UtcNow;

            if (existing.IsFeatured)
            {
                await ClearOtherFeaturedAsync(existing.OwnerUserId, excludeId: existing.RowKey);
            }

            await _resumeStore.UpdateAsync(existing);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(ToDto(existing));
            return response;
        }

        [Function("deleteResume")]
        public async Task<HttpResponseData> Delete(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "resumes/{id}")] HttpRequestData req,
            FunctionContext context,
            string id)
        {
            var user = context.GetAuthenticatedUser();
            if (user is null || !context.IsInRoleOrHigher(AccountRoles.ResumeAdmin))
            {
                return await Forbidden(req, user);
            }

            var existing = await _resumeStore.FindByIdAsync(id);
            if (existing is null)
            {
                return req.CreateResponse(HttpStatusCode.NotFound);
            }

            if (!context.IsOwnerOrSuperAdmin(existing.OwnerUserId))
            {
                return await ErrorResponse(req, HttpStatusCode.Forbidden, "You can only manage your own resumes.");
            }

            await _resumeStore.DeleteAsync(id);
            return req.CreateResponse(HttpStatusCode.NoContent);
        }

        private async Task ClearOtherFeaturedAsync(string ownerUserId, string? excludeId)
        {
            var owned = await _resumeStore.ListByOwnerAsync(ownerUserId);
            foreach (var other in owned.Where(r => r.IsFeatured && r.RowKey != excludeId))
            {
                other.IsFeatured = false;
                await _resumeStore.UpdateAsync(other);
            }
        }

        private static string NormalizedUsername(ClaimsPrincipal user) =>
            (user.Identity?.Name ?? string.Empty).Trim().ToLowerInvariant();

        private static ResumeDto ToDto(ResumeEntity entity)
        {
            DigitalResumeModel? payload = null;
            try
            {
                payload = JsonSerializer.Deserialize<DigitalResumeModel>(entity.PayloadJson);
            }
            catch (JsonException)
            {
                // Leave payload null rather than fail the whole response over one bad row.
            }

            return new ResumeDto(
                entity.RowKey,
                entity.OwnerUserId,
                entity.IsFeatured,
                payload,
                entity.CreatedAtUtc,
                entity.UpdatedAtUtc,
                entity.Status,
                entity.OriginalFileName,
                entity.ContentType,
                entity.FileSizeBytes);
        }

        private static async Task<HttpResponseData> Forbidden(HttpRequestData req, ClaimsPrincipal? user) =>
            user is null
                ? await ErrorResponse(req, HttpStatusCode.Unauthorized, "You must be logged in.")
                : await ErrorResponse(req, HttpStatusCode.Forbidden, "Resume Admin role required.");

        private static async Task<HttpResponseData> BadRequest(HttpRequestData req, string message) =>
            await ErrorResponse(req, HttpStatusCode.BadRequest, message);

        private static async Task<HttpResponseData> ErrorResponse(HttpRequestData req, HttpStatusCode status, string message)
        {
            var response = req.CreateResponse(status);
            await response.WriteAsJsonAsync(new ErrorResponse(message));
            return response;
        }
    }
}
