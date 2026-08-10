using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace BlazorApp.BlazorClient.Services
{
    public class JwtAuthenticationStateProvider : AuthenticationStateProvider
    {
        public const string TokenStorageKey = "authToken";

        private static readonly AuthenticationState AnonymousState = new(new ClaimsPrincipal(new ClaimsIdentity()));

        private readonly IJSRuntime _js;

        public JwtAuthenticationStateProvider(IJSRuntime js)
        {
            _js = js;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var token = await _js.InvokeAsync<string?>("sessionStorage.getItem", TokenStorageKey);
            if (string.IsNullOrWhiteSpace(token))
            {
                return AnonymousState;
            }

            var principal = JwtClaimsParser.ParseClaimsPrincipal(token);
            return principal is null ? AnonymousState : new AuthenticationState(principal);
        }

        public void NotifyUserAuthenticated(string token)
        {
            var principal = JwtClaimsParser.ParseClaimsPrincipal(token) ?? AnonymousState.User;
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(principal)));
        }

        public void NotifyUserLoggedOut()
        {
            NotifyAuthenticationStateChanged(Task.FromResult(AnonymousState));
        }
    }
}
