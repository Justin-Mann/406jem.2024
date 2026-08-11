// Reads a cookie value by name. Used by SessionCookieHandler to echo the non-httpOnly
// XSRF-TOKEN cookie back in the X-XSRF-TOKEN header on mutating API requests (#47) - .NET
// in the browser has no direct access to document.cookie.
window.getCookie = function (name) {
    const match = document.cookie.match(new RegExp('(?:^|; )' + name + '=([^;]*)'));
    return match ? decodeURIComponent(match[1]) : null;
};
