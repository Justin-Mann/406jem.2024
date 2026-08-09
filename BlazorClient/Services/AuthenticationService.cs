using System.Net.Http.Headers;
using System.Net.Http.Json;
using BlazorApp.Models;
using Microsoft.JSInterop;

namespace BlazorApp.BlazorClient.Services
{
    /// <summary>
    /// Phase 1 talks to the in-app username/password endpoints directly. A future Microsoft
    /// Entra ID phase would add a second implementation of this same surface (e.g. redirecting
    /// through MSAL and exchanging the resulting token) without the rest of the app — pages,
    /// the auth-state provider, the gated Testimonials feature — needing to change.
    /// </summary>
    public class AuthenticationService
    {
        private readonly HttpClient _http;
        private readonly IJSRuntime _js;
        private readonly JwtAuthenticationStateProvider _authStateProvider;

        public AuthenticationService(HttpClient http, IJSRuntime js, JwtAuthenticationStateProvider authStateProvider)
        {
            _http = http;
            _js = js;
            _authStateProvider = authStateProvider;
        }

        /// <summary>Restores the Authorization header on app startup from a still-valid session token.</summary>
        public async Task InitializeAsync()
        {
            var token = await _js.InvokeAsync<string?>("sessionStorage.getItem", JwtAuthenticationStateProvider.TokenStorageKey);
            if (!string.IsNullOrWhiteSpace(token))
            {
                _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
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
            if (result is null || string.IsNullOrEmpty(result.Token))
            {
                return "Unexpected response from server.";
            }

            // sessionStorage (not localStorage) so the token is cleared when the tab closes,
            // shrinking the exposure window on a shared machine. The 2-hour JWT expiry bounds it further.
            await _js.InvokeVoidAsync("sessionStorage.setItem", JwtAuthenticationStateProvider.TokenStorageKey, result.Token);
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.Token);
            _authStateProvider.NotifyUserAuthenticated(result.Token);
            return null;
        }

        public async Task LogoutAsync()
        {
            await _js.InvokeVoidAsync("sessionStorage.removeItem", JwtAuthenticationStateProvider.TokenStorageKey);
            _http.DefaultRequestHeaders.Authorization = null;
            _authStateProvider.NotifyUserLoggedOut();

            try
            {
                await _http.PostAsync("api/auth/logout", null);
            }
            catch (HttpRequestException)
            {
                // Logout is a client-side token discard; a failed notify-the-server call
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
