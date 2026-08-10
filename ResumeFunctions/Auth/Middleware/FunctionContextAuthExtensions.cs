using System.Security.Claims;
using Microsoft.Azure.Functions.Worker;
using ResumeFunctions.Auth;

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

        /// <summary>
        /// True if the caller has the given role, or a role that implies it. The only current
        /// hierarchy is SuperAdmin implying ResumeAdmin — encoded here rather than in the JWT so
        /// tokens stay single-role (see AccountRoles.cs).
        /// </summary>
        public static bool IsInRoleOrHigher(this FunctionContext context, string role)
        {
            var user = context.GetAuthenticatedUser();
            if (user is null)
            {
                return false;
            }

            return user.IsInRole(AccountRoles.SuperAdmin) || user.IsInRole(role);
        }

        /// <summary>
        /// True if the caller is the SuperAdmin, or is the owner of the resource identified by
        /// <paramref name="ownerUserId"/> (case-insensitive — usernames are normalized lowercase
        /// throughout, same as TableUserStore.NormalizeUsername).
        /// </summary>
        public static bool IsOwnerOrSuperAdmin(this FunctionContext context, string ownerUserId)
        {
            var user = context.GetAuthenticatedUser();
            if (user is null)
            {
                return false;
            }

            if (user.IsInRole(AccountRoles.SuperAdmin))
            {
                return true;
            }

            var username = user.Identity?.Name ?? string.Empty;
            return string.Equals(username.Trim(), ownerUserId.Trim(), StringComparison.OrdinalIgnoreCase);
        }
    }
}
