using Microsoft.Azure.Functions.Worker.Http;
using ResumeFunctions.Auth.Cookies;
using ResumeFunctions.Auth.Middleware;
using Xunit;

namespace ResumeFunctions.Tests.Auth;

public class CsrfProtectionMiddlewareTests
{
    private static readonly IReadOnlyCollection<IHttpCookie> NoCookies = Array.Empty<IHttpCookie>();

    [Theory]
    [InlineData("GET")]
    [InlineData("HEAD")]
    public void IsValid_AllowsSafeMethods_EvenWithoutCsrfToken(string method)
    {
        Assert.True(CsrfProtectionMiddleware.IsValid(method, "/api/testimonials", NoCookies, new HttpHeadersCollection()));
    }

    [Theory]
    [InlineData("/api/auth/login")]
    [InlineData("/api/auth/register")]
    public void IsValid_ExemptsLoginAndRegister_EvenWithoutCsrfToken(string path)
    {
        Assert.True(CsrfProtectionMiddleware.IsValid("POST", path, NoCookies, new HttpHeadersCollection()));
    }

    [Fact]
    public void IsValid_AllowsMutatingRequest_WhenNoSessionCookiePresent()
    {
        // A bearer-token-only caller (no cookie session) has nothing ambient for a forged
        // cross-site request to exploit.
        Assert.True(CsrfProtectionMiddleware.IsValid("POST", "/api/testimonials", NoCookies, new HttpHeadersCollection()));
    }

    [Fact]
    public void IsValid_RejectsMutatingRequest_WhenSessionCookiePresentButXsrfHeaderMissing()
    {
        var cookies = new IHttpCookie[] { new HttpCookie(CookieNames.Auth, "jwt-value"), new HttpCookie(CookieNames.Xsrf, "xsrf-value") };

        Assert.False(CsrfProtectionMiddleware.IsValid("POST", "/api/testimonials", cookies, new HttpHeadersCollection()));
    }

    [Fact]
    public void IsValid_RejectsMutatingRequest_WhenXsrfHeaderDoesNotMatchCookie()
    {
        var cookies = new IHttpCookie[] { new HttpCookie(CookieNames.Auth, "jwt-value"), new HttpCookie(CookieNames.Xsrf, "xsrf-value") };
        var headers = new HttpHeadersCollection();
        headers.Add(CsrfProtectionMiddleware.HeaderName, "some-other-value");

        Assert.False(CsrfProtectionMiddleware.IsValid("DELETE", "/api/testimonials/1", cookies, headers));
    }

    [Fact]
    public void IsValid_AcceptsMutatingRequest_WhenXsrfHeaderMatchesCookie()
    {
        var cookies = new IHttpCookie[] { new HttpCookie(CookieNames.Auth, "jwt-value"), new HttpCookie(CookieNames.Xsrf, "matching-token") };
        var headers = new HttpHeadersCollection();
        headers.Add(CsrfProtectionMiddleware.HeaderName, "matching-token");

        Assert.True(CsrfProtectionMiddleware.IsValid("POST", "/api/testimonials", cookies, headers));
    }

    [Fact]
    public void IsValid_RejectsMutatingRequest_WhenSessionCookiePresentButNoXsrfCookieIssued()
    {
        var cookies = new IHttpCookie[] { new HttpCookie(CookieNames.Auth, "jwt-value") };
        var headers = new HttpHeadersCollection();
        headers.Add(CsrfProtectionMiddleware.HeaderName, "anything");

        Assert.False(CsrfProtectionMiddleware.IsValid("PUT", "/api/siteconfig", cookies, headers));
    }
}
