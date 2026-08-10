using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Security.Claims;

namespace ResumeFunctions.Tests.Helpers;

public class TestHttpRequestData : HttpRequestData
{
    private readonly HttpResponseData _response;
    private readonly Stream _body;
    private readonly string _method;
    private readonly HttpHeadersCollection _headers;

    public TestHttpRequestData(
        FunctionContext context,
        HttpResponseData response,
        Stream? body = null,
        string method = "GET",
        HttpHeadersCollection? headers = null) : base(context)
    {
        _response = response;
        _body = body ?? Stream.Null;
        _method = method;
        _headers = headers ?? new HttpHeadersCollection();
    }

    public override Stream Body => _body;
    public override HttpHeadersCollection Headers => _headers;
    public override IReadOnlyCollection<IHttpCookie> Cookies => Array.Empty<IHttpCookie>();
    public override Uri Url => new Uri("https://localhost/api/resumes/myresume");
    public override IEnumerable<ClaimsIdentity> Identities => Array.Empty<ClaimsIdentity>();
    public override string Method => _method;
    public override HttpResponseData CreateResponse() => _response;
}
