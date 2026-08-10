namespace ResumeFunctions.Auth.Identity
{
    /// <summary>
    /// Seam between "how a user proves who they are" and everything downstream (token issuance,
    /// auth guards). Phase 1 only ships <see cref="LocalPasswordIdentityProvider"/> (in-app
    /// username/password against Table Storage). A future Microsoft Entra ID phase can add a
    /// second implementation (e.g. validating an Entra-issued token) and register it alongside
    /// or instead of this one, without touching AuthApi, the token service, or the auth guard.
    /// </summary>
    public interface IIdentityProvider
    {
        Task<AuthenticationResult> AuthenticateAsync(string username, string password, CancellationToken cancellationToken = default);
    }
}
