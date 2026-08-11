using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace BlazorApp.BlazorClient.Services
{
    /// <summary>
    /// Since #47, holds whatever auth state AuthenticationService last told it about - it never
    /// decodes a token itself, because the session lives in an httpOnly cookie the app can't
    /// read. AuthenticationService.InitializeAsync() calls NotifyUserAuthenticated/
    /// NotifyUserLoggedOut once at startup (from GET /api/auth/me) before the host runs, so the
    /// first GetAuthenticationStateAsync() call already reflects the hydrated result.
    /// </summary>
    public class JwtAuthenticationStateProvider : AuthenticationStateProvider
    {
        private static readonly AuthenticationState AnonymousState = new(new ClaimsPrincipal(new ClaimsIdentity()));

        private AuthenticationState _currentState = AnonymousState;

        public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(_currentState);

        public void NotifyUserAuthenticated(string username, string role)
        {
            var identity = new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.Name, username), new Claim(ClaimTypes.Role, role) },
                authenticationType: "auth-me",
                nameType: ClaimTypes.Name,
                roleType: ClaimTypes.Role);

            _currentState = new AuthenticationState(new ClaimsPrincipal(identity));
            NotifyAuthenticationStateChanged(Task.FromResult(_currentState));
        }

        public void NotifyUserLoggedOut()
        {
            _currentState = AnonymousState;
            NotifyAuthenticationStateChanged(Task.FromResult(_currentState));
        }
    }
}
