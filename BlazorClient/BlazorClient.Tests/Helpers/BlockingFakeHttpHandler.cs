using System.Net;
using System.Text;

namespace BlazorClient.Tests.Helpers;

public class BlockingFakeHttpHandler : HttpMessageHandler
{
    private readonly TaskCompletionSource<HttpResponseMessage> _tcs = new();

    public void Complete(string json, HttpStatusCode status = HttpStatusCode.OK)
    {
        _tcs.SetResult(new HttpResponseMessage(status)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        });
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
        => _tcs.Task;
}
