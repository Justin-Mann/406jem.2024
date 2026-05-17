using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Security.Claims;

namespace ResumeFunctions.Tests.Helpers;

public class TestHttpRequestData : HttpRequestData
{
    private readonly HttpResponseData _response;

    public TestHttpRequestData(FunctionContext context, HttpResponseData response) : base(context)
    {
        _response = response;
    }

    public override Stream Body => Stream.Null;
    public override HttpHeadersCollection Headers => new HttpHeadersCollection();
    public override IReadOnlyCollection<IHttpCookie> Cookies => Array.Empty<IHttpCookie>();
    public override Uri Url => new Uri("https://localhost/api/resumes/myresume");
    public override IEnumerable<ClaimsIdentity> Identities => Array.Empty<ClaimsIdentity>();
    public override string Method => "GET";
    public override HttpResponseData CreateResponse() => _response;
}
