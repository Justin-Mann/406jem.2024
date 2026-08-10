using System.Net;
using System.Text;

namespace BlazorClient.Tests.Helpers;

/// <summary>
/// Routes by HTTP method + a substring match on the request URI, for pages that call more
/// than one endpoint (unlike FakeHttpHandler, which always returns the same response).
/// </summary>
public class RoutedFakeHttpHandler : HttpMessageHandler
{
    public record Route(HttpMethod Method, string UriContains, string Json, HttpStatusCode StatusCode = HttpStatusCode.OK);

    private readonly List<Route> _routes = new();
    public List<HttpRequestMessage> Requests { get; } = new();

    public RoutedFakeHttpHandler When(HttpMethod method, string uriContains, string json, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        _routes.Add(new Route(method, uriContains, json, statusCode));
        return this;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request);

        var route = _routes.FirstOrDefault(r => r.Method == request.Method && (request.RequestUri?.ToString().Contains(r.UriContains) ?? false));
        if (route is null)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            });
        }

        var response = new HttpResponseMessage(route.StatusCode)
        {
            Content = new StringContent(route.Json, Encoding.UTF8, "application/json")
        };
        return Task.FromResult(response);
    }
}
