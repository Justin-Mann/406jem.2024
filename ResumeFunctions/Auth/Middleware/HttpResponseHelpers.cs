using System.Net;
using System.Security.Claims;
using Microsoft.Azure.Functions.Worker.Http;
using ResumeFunctions.Auth.Dtos;

namespace ResumeFunctions.Auth.Middleware
{
    /// <summary>Shared error-response helpers used across the authenticated resume/testimonial
    /// APIs, so the 401-vs-403 "must be logged in" vs "role required" distinction stays
    /// consistent everywhere it's checked.</summary>
    internal static class HttpResponseHelpers
    {
        public static async Task<HttpResponseData> Forbidden(HttpRequestData req, ClaimsPrincipal? user, string roleRequiredMessage) =>
            user is null
                ? await ErrorResponse(req, HttpStatusCode.Unauthorized, "You must be logged in.")
                : await ErrorResponse(req, HttpStatusCode.Forbidden, roleRequiredMessage);

        public static async Task<HttpResponseData> ErrorResponse(HttpRequestData req, HttpStatusCode status, string message)
        {
            var response = req.CreateResponse(status);
            await response.WriteAsJsonAsync(new ErrorResponse(message));
            return response;
        }
    }
}
