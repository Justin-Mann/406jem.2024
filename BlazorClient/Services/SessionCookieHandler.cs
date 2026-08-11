using Microsoft.AspNetCore.Components.WebAssembly.Http;
using Microsoft.JSInterop;

namespace BlazorApp.BlazorClient.Services
{
    /// <summary>
    /// Attaches the session cookie to every API call - Blazor WASM's fetch-backed HttpClient
    /// defaults to "same-origin" credentials, which would silently drop the cross-origin
    /// session cookie (#47) - and, on state-changing requests, echoes the non-httpOnly
    /// XSRF-TOKEN cookie back in a custom header, the double-submit CSRF check the API
    /// requires alongside SameSite=Lax. The cookie itself can't be read via .NET APIs in the
    /// browser, so a tiny JS helper (wwwroot/js/xsrf.js) reads document.cookie.
    /// </summary>
    public class SessionCookieHandler : DelegatingHandler
    {
        private static readonly HashSet<HttpMethod> MutatingMethods = new()
        {
            HttpMethod.Post, HttpMethod.Put, HttpMethod.Patch, HttpMethod.Delete,
        };

        private readonly IJSRuntime _js;

        public SessionCookieHandler(IJSRuntime js)
        {
            _js = js;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);

            if (MutatingMethods.Contains(request.Method))
            {
                var xsrfToken = await _js.InvokeAsync<string?>("getCookie", cancellationToken, "XSRF-TOKEN");
                if (!string.IsNullOrEmpty(xsrfToken))
                {
                    request.Headers.Add("X-XSRF-TOKEN", xsrfToken);
                }
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
