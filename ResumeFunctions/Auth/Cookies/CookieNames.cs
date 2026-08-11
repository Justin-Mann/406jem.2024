namespace ResumeFunctions.Auth.Cookies
{
    public static class CookieNames
    {
        /// <summary>httpOnly session cookie holding the JWT. Never readable from JS.</summary>
        public const string Auth = "406jem_auth";

        /// <summary>
        /// Non-httpOnly double-submit CSRF cookie. Readable by same-origin JS so the frontend
        /// can echo its value back in the <see cref="Middleware.CsrfProtectionMiddleware.HeaderName"/>
        /// header on mutating requests; a cross-site page can make the browser attach this
        /// cookie automatically but can't read its value to also set the header.
        /// </summary>
        public const string Xsrf = "XSRF-TOKEN";
    }
}
