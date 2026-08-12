using ResumeFunctions.Auth.Models;
using ResumeFunctions.Auth.Storage;

namespace ResumeFunctions.Tests.Helpers;

/// <summary>In-memory IUserStore for tests that need real mutation (lockout counters, etc.).</summary>
public class FakeUserStore : IUserStore
{
    private readonly Dictionary<string, UserAccountEntity> _users = new();

    public Task<UserAccountEntity?> FindByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        _users.TryGetValue(username.Trim().ToLowerInvariant(), out var user);
        return Task.FromResult(user);
    }

    public Task<bool> CreateAsync(UserAccountEntity user, CancellationToken cancellationToken = default)
    {
        var key = user.Username.Trim().ToLowerInvariant();
        if (_users.ContainsKey(key))
        {
            return Task.FromResult(false);
        }
        _users[key] = user;
        return Task.FromResult(true);
    }

    public Task UpdateAsync(UserAccountEntity user, CancellationToken cancellationToken = default)
    {
        _users[user.Username.Trim().ToLowerInvariant()] = user;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<UserAccountEntity>> ListAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<UserAccountEntity>>(_users.Values.ToList());
    }
}
