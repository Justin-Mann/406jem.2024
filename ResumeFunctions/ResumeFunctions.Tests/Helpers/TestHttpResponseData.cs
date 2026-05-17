using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;

namespace ResumeFunctions.Tests.Helpers;

public class TestHttpResponseData : HttpResponseData
{
    public TestHttpResponseData(FunctionContext context) : base(context) { }

    public override HttpStatusCode StatusCode { get; set; }
    public override HttpHeadersCollection Headers { get; set; } = new HttpHeadersCollection();
    public override Stream Body { get; set; } = new MemoryStream();
    public override HttpCookies Cookies { get; } = null!;
}
