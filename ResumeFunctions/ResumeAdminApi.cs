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
        private readonly ILogger<ResumeAdminApi> _logger;
        private readonly IResumeStore _resumeStore;

        public ResumeAdminApi(ILogger<ResumeAdminApi> logger, IResumeStore resumeStore)
        {
            _logger = logger;
            _resumeStore = resumeStore;
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

            return new ResumeDto(entity.RowKey, entity.OwnerUserId, entity.IsFeatured, payload, entity.CreatedAtUtc, entity.UpdatedAtUtc);
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
