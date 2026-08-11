using Microsoft.Azure.Functions.Worker.Http;

namespace ResumeFunctions.Tests.Helpers;

/// <summary>Records every cookie a function under test appends to the response, so tests can
/// assert on attributes (HttpOnly, Secure, SameSite, Domain, Expires) directly.</summary>
public class TestHttpCookies : HttpCookies
{
    public List<IHttpCookie> Appended { get; } = new();

    public override void Append(string name, string value) => Appended.Add(new HttpCookie(name, value));

    public override void Append(IHttpCookie cookie) => Appended.Add(cookie);

    public override IHttpCookie CreateNew() => new HttpCookie(string.Empty, string.Empty);
}
