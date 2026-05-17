using System.Net;
using System.Text;

namespace BlazorClient.Tests.Helpers;

public class FakeHttpHandler : HttpMessageHandler
{
    private readonly string _json;
    private readonly HttpStatusCode _statusCode;

    public FakeHttpHandler(string json, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        _json = json;
        _statusCode = statusCode;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = new HttpResponseMessage(_statusCode)
        {
            Content = new StringContent(_json, Encoding.UTF8, "application/json")
        };
        return Task.FromResult(response);
    }
}
