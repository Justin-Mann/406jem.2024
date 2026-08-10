using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using ResumeFunctions.Auth;
using ResumeFunctions.Auth.Dtos;
using ResumeFunctions.Auth.Middleware;
using ResumeFunctions.Auth.Storage;

namespace ResumeFunctions
{
    /// <summary>
    /// Decides which Resume Admin's featured resume/project listing is currently public.
    /// Readable by any Resume Admin or SuperAdmin; writable only by a SuperAdmin.
    /// </summary>
    public class SiteConfigApi
    {
        private readonly ILogger<SiteConfigApi> _logger;
        private readonly ISiteConfigStore _siteConfigStore;

        public SiteConfigApi(ILogger<SiteConfigApi> logger, ISiteConfigStore siteConfigStore)
        {
            _logger = logger;
            _siteConfigStore = siteConfigStore;
        }

        [Function("getSiteConfig")]
        public async Task<HttpResponseData> Get(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "siteconfig")] HttpRequestData req,
            FunctionContext context)
        {
            var user = context.GetAuthenticatedUser();
            if (user is null)
            {
                return await ErrorResponse(req, HttpStatusCode.Unauthorized, "You must be logged in.");
            }

            if (!context.IsInRoleOrHigher(AccountRoles.ResumeAdmin))
            {
                return await ErrorResponse(req, HttpStatusCode.Forbidden, "Resume Admin role required.");
            }

            var config = await _siteConfigStore.GetAsync();
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new SiteConfigDto(config?.PublicResumeOwnerId, config?.PublicProjectsOwnerId));
            return response;
        }

        [Function("updateSiteConfig")]
        public async Task<HttpResponseData> Update(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "siteconfig")] HttpRequestData req,
            FunctionContext context)
        {
            var user = context.GetAuthenticatedUser();
            if (user is null)
            {
                return await ErrorResponse(req, HttpStatusCode.Unauthorized, "You must be logged in.");
            }

            if (!context.IsInRole(AccountRoles.SuperAdmin))
            {
                return await ErrorResponse(req, HttpStatusCode.Forbidden, "SuperAdmin role required.");
            }

            var payload = await req.ReadFromJsonAsync<UpdateSiteConfigRequest>();
            var updated = await _siteConfigStore.SetAsync(payload?.PublicResumeOwnerId, payload?.PublicProjectsOwnerId);

            _logger.LogInformation(
                "SiteConfig updated by '{Username}': resumeOwner='{ResumeOwner}', projectsOwner='{ProjectsOwner}'.",
                user.Identity?.Name, updated.PublicResumeOwnerId, updated.PublicProjectsOwnerId);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new SiteConfigDto(updated.PublicResumeOwnerId, updated.PublicProjectsOwnerId));
            return response;
        }

        private static async Task<HttpResponseData> ErrorResponse(HttpRequestData req, HttpStatusCode status, string message)
        {
            var response = req.CreateResponse(status);
            await response.WriteAsJsonAsync(new ErrorResponse(message));
            return response;
        }
    }
}
