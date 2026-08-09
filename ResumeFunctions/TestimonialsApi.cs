using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using ResumeFunctions.Auth;
using ResumeFunctions.Auth.Dtos;
using ResumeFunctions.Auth.Middleware;
using ResumeFunctions.Auth.Models;
using ResumeFunctions.Auth.Storage;

namespace ResumeFunctions
{
    /// <summary>
    /// The minimal end-to-end proof that the login gate works: anyone can read testimonials,
    /// but only a logged-in user can leave one, and only an admin can remove one.
    /// </summary>
    public class TestimonialsApi
    {
        private const int MaxMessageLength = 500;

        private readonly ILogger<TestimonialsApi> _logger;
        private readonly ITestimonialStore _testimonialStore;

        public TestimonialsApi(ILogger<TestimonialsApi> logger, ITestimonialStore testimonialStore)
        {
            _logger = logger;
            _testimonialStore = testimonialStore;
        }

        [Function("listTestimonials")]
        public async Task<HttpResponseData> List(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "testimonials")] HttpRequestData req)
        {
            var testimonials = await _testimonialStore.ListAsync();
            var dtos = testimonials.Select(t => new TestimonialDto(t.RowKey, t.AuthorUsername, t.Message, t.CreatedAtUtc));

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(dtos);
            return response;
        }

        [Function("createTestimonial")]
        public async Task<HttpResponseData> Create(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "testimonials")] HttpRequestData req,
            FunctionContext context)
        {
            var user = context.GetAuthenticatedUser();
            if (user is null)
            {
                return await Unauthorized(req);
            }

            var payload = await req.ReadFromJsonAsync<CreateTestimonialRequest>();
            var message = payload?.Message?.Trim();
            if (string.IsNullOrWhiteSpace(message))
            {
                return await BadRequest(req, "Message is required.");
            }

            if (message.Length > MaxMessageLength)
            {
                return await BadRequest(req, $"Message must be {MaxMessageLength} characters or fewer.");
            }

            var testimonial = new TestimonialEntity
            {
                AuthorUsername = user.Identity?.Name ?? "unknown",
                Message = message,
                CreatedAtUtc = DateTimeOffset.UtcNow,
            };

            await _testimonialStore.AddAsync(testimonial);
            _logger.LogInformation("Testimonial created by '{Username}'.", testimonial.AuthorUsername);

            var response = req.CreateResponse(HttpStatusCode.Created);
            await response.WriteAsJsonAsync(new TestimonialDto(testimonial.RowKey, testimonial.AuthorUsername, testimonial.Message, testimonial.CreatedAtUtc));
            return response;
        }

        [Function("deleteTestimonial")]
        public async Task<HttpResponseData> Delete(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "testimonials/{id}")] HttpRequestData req,
            FunctionContext context,
            string id)
        {
            var user = context.GetAuthenticatedUser();
            if (user is null)
            {
                return await Unauthorized(req);
            }

            if (!context.IsInRole(AccountRoles.Admin))
            {
                return await ErrorResponse(req, HttpStatusCode.Forbidden, "Admin role required.");
            }

            var deleted = await _testimonialStore.DeleteAsync(id);
            return req.CreateResponse(deleted ? HttpStatusCode.NoContent : HttpStatusCode.NotFound);
        }

        private static async Task<HttpResponseData> Unauthorized(HttpRequestData req) =>
            await ErrorResponse(req, HttpStatusCode.Unauthorized, "You must be logged in.");

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
