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
    /// Authenticated CRUD over a Resume Admin's own project listings (SuperAdmin may act on any
    /// owner's), plus the public read side resolved through SiteConfig — same shape as
    /// ResumeAdminApi/ResumeApi's split.
    /// </summary>
    public class ProjectListingsApi
    {
        private static readonly ProjectListingModel DefaultListing = new()
        {
            Title = "Projects",
            Sections = new[]
            {
                new ProjectSection
                {
                    Heading = "WWW",
                    LastUpdated = "04/2025",
                    Links = new[]
                    {
                        new ProjectLink { Label = "LinkedIn", Url = "https://www.linkedin.com/in/justin-mann-b3822075/" },
                        new ProjectLink { Label = "GitHub", Url = "https://github.com/Justin-Mann" },
                        new ProjectLink { Label = "406CreatorCollections.com", Url = "https://406creatorcollections.com" },
                    },
                },
            },
        };

        private readonly ILogger<ProjectListingsApi> _logger;
        private readonly IProjectListingStore _listingStore;
        private readonly ISiteConfigStore _siteConfigStore;

        public ProjectListingsApi(ILogger<ProjectListingsApi> logger, IProjectListingStore listingStore, ISiteConfigStore siteConfigStore)
        {
            _logger = logger;
            _listingStore = listingStore;
            _siteConfigStore = siteConfigStore;
        }

        [Function("publicProjectListing")]
        public async Task<HttpResponseData> GetPublic(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projectlistings/public")] HttpRequestData req)
        {
            var payload = await TryGetFeaturedListingAsync() ?? DefaultListing;
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(payload);
            return response;
        }

        [Function("listMyProjectListings")]
        public async Task<HttpResponseData> ListMine(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projectlistings/mine")] HttpRequestData req,
            FunctionContext context)
        {
            var user = context.GetAuthenticatedUser();
            if (user is null || !context.IsInRoleOrHigher(AccountRoles.ResumeAdmin))
            {
                return await Forbidden(req, user);
            }

            var owner = NormalizedUsername(user);
            var listings = await _listingStore.ListByOwnerAsync(owner);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(listings.Select(ToDto));
            return response;
        }

        [Function("createProjectListing")]
        public async Task<HttpResponseData> Create(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "projectlistings")] HttpRequestData req,
            FunctionContext context)
        {
            var user = context.GetAuthenticatedUser();
            if (user is null || !context.IsInRoleOrHigher(AccountRoles.ResumeAdmin))
            {
                return await Forbidden(req, user);
            }

            var payload = await req.ReadFromJsonAsync<CreateOrUpdateProjectListingRequest>();
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
                return await ErrorResponse(req, HttpStatusCode.Forbidden, "You can only manage your own project listings.");
            }

            var now = DateTimeOffset.UtcNow;
            var listing = new ProjectListingEntity
            {
                OwnerUserId = requestedOwner,
                IsFeatured = payload.IsFeatured,
                PayloadJson = JsonSerializer.Serialize(payload.Payload),
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
            };

            if (listing.IsFeatured)
            {
                await ClearOtherFeaturedAsync(requestedOwner, excludeId: null);
            }

            await _listingStore.AddAsync(listing);
            _logger.LogInformation("Project listing created for owner '{Owner}' by '{Username}'.", requestedOwner, owner);

            var response = req.CreateResponse(HttpStatusCode.Created);
            await response.WriteAsJsonAsync(ToDto(listing));
            return response;
        }

        [Function("updateProjectListing")]
        public async Task<HttpResponseData> Update(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "projectlistings/{id}")] HttpRequestData req,
            FunctionContext context,
            string id)
        {
            var user = context.GetAuthenticatedUser();
            if (user is null || !context.IsInRoleOrHigher(AccountRoles.ResumeAdmin))
            {
                return await Forbidden(req, user);
            }

            var existing = await _listingStore.FindByIdAsync(id);
            if (existing is null)
            {
                return await ErrorResponse(req, HttpStatusCode.NotFound, "Project listing not found.");
            }

            if (!context.IsOwnerOrSuperAdmin(existing.OwnerUserId))
            {
                return await ErrorResponse(req, HttpStatusCode.Forbidden, "You can only manage your own project listings.");
            }

            var payload = await req.ReadFromJsonAsync<CreateOrUpdateProjectListingRequest>();
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

            await _listingStore.UpdateAsync(existing);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(ToDto(existing));
            return response;
        }

        [Function("deleteProjectListing")]
        public async Task<HttpResponseData> Delete(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "projectlistings/{id}")] HttpRequestData req,
            FunctionContext context,
            string id)
        {
            var user = context.GetAuthenticatedUser();
            if (user is null || !context.IsInRoleOrHigher(AccountRoles.ResumeAdmin))
            {
                return await Forbidden(req, user);
            }

            var existing = await _listingStore.FindByIdAsync(id);
            if (existing is null)
            {
                return req.CreateResponse(HttpStatusCode.NotFound);
            }

            if (!context.IsOwnerOrSuperAdmin(existing.OwnerUserId))
            {
                return await ErrorResponse(req, HttpStatusCode.Forbidden, "You can only manage your own project listings.");
            }

            await _listingStore.DeleteAsync(id);
            return req.CreateResponse(HttpStatusCode.NoContent);
        }

        private async Task<ProjectListingModel?> TryGetFeaturedListingAsync()
        {
            var config = await _siteConfigStore.GetAsync();
            if (string.IsNullOrWhiteSpace(config?.PublicProjectsOwnerId))
            {
                return null;
            }

            var featured = await _listingStore.FindFeaturedByOwnerAsync(config!.PublicProjectsOwnerId!);
            if (featured is null)
            {
                return null;
            }

            try
            {
                return JsonSerializer.Deserialize<ProjectListingModel>(featured.PayloadJson);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private async Task ClearOtherFeaturedAsync(string ownerUserId, string? excludeId)
        {
            var owned = await _listingStore.ListByOwnerAsync(ownerUserId);
            foreach (var other in owned.Where(l => l.IsFeatured && l.RowKey != excludeId))
            {
                other.IsFeatured = false;
                await _listingStore.UpdateAsync(other);
            }
        }

        private static string NormalizedUsername(ClaimsPrincipal user) =>
            (user.Identity?.Name ?? string.Empty).Trim().ToLowerInvariant();

        private static ProjectListingDto ToDto(ProjectListingEntity entity)
        {
            ProjectListingModel? payload = null;
            try
            {
                payload = JsonSerializer.Deserialize<ProjectListingModel>(entity.PayloadJson);
            }
            catch (JsonException)
            {
                // Leave payload null rather than fail the whole response over one bad row.
            }

            return new ProjectListingDto(entity.RowKey, entity.OwnerUserId, entity.IsFeatured, payload, entity.CreatedAtUtc, entity.UpdatedAtUtc);
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
