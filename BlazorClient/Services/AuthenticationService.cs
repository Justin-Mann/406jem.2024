using System.Net.Http.Json;
using BlazorApp.Models;

namespace BlazorApp.BlazorClient.Services
{
    /// <summary>
    /// Phase 1 talks to the in-app username/password endpoints directly. A future Microsoft
    /// Entra ID phase would add a second implementation of this same surface (e.g. redirecting
    /// through MSAL and exchanging the resulting token) without the rest of the app — pages,
    /// the auth-state provider, the gated Testimonials feature — needing to change.
    ///
    /// Since #47, the session lives in an httpOnly, Domain=406jem.com cookie set by the API and
    /// shared by every browser-based client on the site - not in sessionStorage/a JS-readable
    /// token. The cookie is deliberately unreadable from Blazor, so this service hydrates
    /// "am I logged in, as whom" by asking GET /api/auth/me on startup.
    /// </summary>
    public class AuthenticationService
    {
        private readonly HttpClient _http;
        private readonly JwtAuthenticationStateProvider _authStateProvider;

        public AuthenticationService(HttpClient http, JwtAuthenticationStateProvider authStateProvider)
        {
            _http = http;
            _authStateProvider = authStateProvider;
        }

        /// <summary>Hydrates auth state on app startup from the session cookie via GET /api/auth/me.</summary>
        public async Task InitializeAsync()
        {
            try
            {
                var response = await _http.GetAsync("api/auth/me");
                if (response.IsSuccessStatusCode)
                {
                    var me = await response.Content.ReadFromJsonAsync<MeResponse>();
                    if (me is not null)
                    {
                        _authStateProvider.NotifyUserAuthenticated(me.Username, me.Role);
                        return;
                    }
                }
            }
            catch (HttpRequestException)
            {
                // API unreachable at startup - fall through to anonymous rather than blocking app load.
            }

            _authStateProvider.NotifyUserLoggedOut();
        }

        /// <returns>null on success, otherwise a user-facing error message.</returns>
        public async Task<string?> RegisterAsync(string username, string email, string password)
        {
            var response = await _http.PostAsJsonAsync("api/auth/register", new { username, email, password });
            return response.IsSuccessStatusCode ? null : await ExtractErrorAsync(response);
        }

        /// <returns>null on success, otherwise a user-facing error message.</returns>
        public async Task<string?> LoginAsync(string username, string password)
        {
            var response = await _http.PostAsJsonAsync("api/auth/login", new { username, password });
            if (!response.IsSuccessStatusCode)
            {
                return await ExtractErrorAsync(response);
            }

            var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
            if (result is null || string.IsNullOrEmpty(result.Username))
            {
                return "Unexpected response from server.";
            }

            _authStateProvider.NotifyUserAuthenticated(result.Username, result.Role);
            return null;
        }

        public async Task LogoutAsync()
        {
            _authStateProvider.NotifyUserLoggedOut();

            try
            {
                await _http.PostAsync("api/auth/logout", null);
            }
            catch (HttpRequestException)
            {
                // Logout is a client-side session clear; a failed notify-the-server call
                // shouldn't stop the user from being logged out locally.
            }
        }

        private static async Task<string> ExtractErrorAsync(HttpResponseMessage response)
        {
            try
            {
                var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
                return !string.IsNullOrWhiteSpace(error?.Message) ? error.Message : $"Request failed ({(int)response.StatusCode}).";
            }
            catch (Exception ex) when (ex is System.Text.Json.JsonException or NotSupportedException)
            {
                return $"Request failed ({(int)response.StatusCode}).";
            }
        }
    }
}
