using ResumeFunctions.Auth;
using ResumeFunctions.Auth.Identity;
using ResumeFunctions.Auth.Models;
using ResumeFunctions.Auth.Security;
using ResumeFunctions.Tests.Helpers;
using Xunit;

namespace ResumeFunctions.Tests.Auth;

public class LocalPasswordIdentityProviderTests
{
    private readonly FakeUserStore _userStore = new();
    private readonly Pbkdf2PasswordHasher _hasher = new();
    private readonly LocalPasswordIdentityProvider _provider;

    public LocalPasswordIdentityProviderTests()
    {
        _provider = new LocalPasswordIdentityProvider(_userStore, _hasher);
    }

    private async Task SeedUserAsync(string username, string password, string role = AccountRoles.Visitor)
    {
        await _userStore.CreateAsync(new UserAccountEntity
        {
            Username = username,
            Email = $"{username}@example.com",
            PasswordHash = _hasher.Hash(password),
            Role = role,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        });
    }

    [Fact]
    public async Task AuthenticateAsync_Succeeds_ForCorrectCredentials()
    {
        await SeedUserAsync("jane", "correct-password");

        var result = await _provider.AuthenticateAsync("jane", "correct-password");

        Assert.Equal(AuthenticationOutcome.Success, result.Outcome);
        Assert.Equal("jane", result.Username);
        Assert.Equal(AccountRoles.Visitor, result.Role);
    }

    [Fact]
    public async Task AuthenticateAsync_Fails_ForUnknownUsername()
    {
        var result = await _provider.AuthenticateAsync("nobody", "whatever");

        Assert.Equal(AuthenticationOutcome.InvalidCredentials, result.Outcome);
    }

    [Fact]
    public async Task AuthenticateAsync_Fails_ForWrongPassword()
    {
        await SeedUserAsync("jane", "correct-password");

        var result = await _provider.AuthenticateAsync("jane", "wrong-password");

        Assert.Equal(AuthenticationOutcome.InvalidCredentials, result.Outcome);
    }

    [Fact]
    public async Task AuthenticateAsync_LocksOutAccount_AfterFiveFailedAttempts()
    {
        await SeedUserAsync("jane", "correct-password");

        for (var i = 0; i < 5; i++)
        {
            await _provider.AuthenticateAsync("jane", "wrong-password");
        }

        var result = await _provider.AuthenticateAsync("jane", "correct-password");

        Assert.Equal(AuthenticationOutcome.LockedOut, result.Outcome);
    }

    [Fact]
    public async Task AuthenticateAsync_ResetsFailedAttempts_AfterSuccessfulLogin()
    {
        await SeedUserAsync("jane", "correct-password");
        await _provider.AuthenticateAsync("jane", "wrong-password");
        await _provider.AuthenticateAsync("jane", "wrong-password");

        await _provider.AuthenticateAsync("jane", "correct-password");
        var user = await _userStore.FindByUsernameAsync("jane");

        Assert.Equal(0, user!.FailedLoginAttempts);
    }
}
