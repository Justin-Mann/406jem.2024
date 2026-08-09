using System.Security.Claims;
using Microsoft.Azure.Functions.Worker;

namespace ResumeFunctions.Auth.Middleware
{
    public static class FunctionContextAuthExtensions
    {
        public static ClaimsPrincipal? GetAuthenticatedUser(this FunctionContext context)
        {
            return context.Items.TryGetValue(JwtAuthenticationMiddleware.ContextItemKey, out var value)
                ? value as ClaimsPrincipal
                : null;
        }

        public static bool IsInRole(this FunctionContext context, string role)
        {
            var user = context.GetAuthenticatedUser();
            return user is not null && user.IsInRole(role);
        }
    }
}
