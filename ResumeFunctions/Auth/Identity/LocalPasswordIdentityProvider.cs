using ResumeFunctions.Auth.Models;
using ResumeFunctions.Auth.Security;
using ResumeFunctions.Auth.Storage;

namespace ResumeFunctions.Auth.Identity
{
    public class LocalPasswordIdentityProvider : IIdentityProvider
    {
        private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(5);
        private const int MaxFailedAttempts = 5;

        private readonly IUserStore _userStore;
        private readonly IPasswordHasher _passwordHasher;

        public LocalPasswordIdentityProvider(IUserStore userStore, IPasswordHasher passwordHasher)
        {
            _userStore = userStore;
            _passwordHasher = passwordHasher;
        }

        public async Task<AuthenticationResult> AuthenticateAsync(string username, string password, CancellationToken cancellationToken = default)
        {
            var user = await _userStore.FindByUsernameAsync(username, cancellationToken);
            if (user is null)
            {
                return AuthenticationResult.InvalidCredentials;
            }

            var now = DateTimeOffset.UtcNow;
            if (user.LockoutUntilUtc is { } lockoutUntil && lockoutUntil > now)
            {
                return AuthenticationResult.LockedOut;
            }

            if (!_passwordHasher.Verify(password, user.PasswordHash))
            {
                user.FailedLoginAttempts++;
                if (user.FailedLoginAttempts >= MaxFailedAttempts)
                {
                    user.LockoutUntilUtc = now.Add(LockoutDuration);
                    user.FailedLoginAttempts = 0;
                }
                await _userStore.UpdateAsync(user, cancellationToken);
                return AuthenticationResult.InvalidCredentials;
            }

            if (user.FailedLoginAttempts > 0 || user.LockoutUntilUtc is not null)
            {
                user.FailedLoginAttempts = 0;
                user.LockoutUntilUtc = null;
                await _userStore.UpdateAsync(user, cancellationToken);
            }

            return AuthenticationResult.Success(user.Username, user.Role);
        }
    }
}
