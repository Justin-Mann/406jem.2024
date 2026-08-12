using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using ResumeFunctions.Auth;
using ResumeFunctions.Auth.Models;
using ResumeFunctions.Auth.Security;
using ResumeFunctions.Auth.Seeding;
using ResumeFunctions.Auth.Storage;
using ResumeFunctions.Tests.Helpers;
using Xunit;

namespace ResumeFunctions.Tests.Auth;

public class AdminAccountSeederTests
{
    private static IConfiguration BuildConfig(string? adminPassword = "super-secret-password") =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Auth:AdminUsername"] = "admin",
                ["Auth:AdminEmail"] = "admin@example.com",
                ["Auth:AdminPassword"] = adminPassword,
            })
            .Build();

    [Fact]
    public async Task StartAsync_SeedsAccountAsSuperAdmin_WhenNotAlreadyPresent()
    {
        var userStore = new FakeUserStore();
        var seeder = new AdminAccountSeeder(
            userStore, new Pbkdf2PasswordHasher(), BuildConfig(), Substitute.For<ILogger<AdminAccountSeeder>>());

        await seeder.StartAsync(CancellationToken.None);

        var created = await userStore.FindByUsernameAsync("admin");
        Assert.NotNull(created);
        Assert.Equal(AccountRoles.SuperAdmin, created!.Role);
    }

    [Fact]
    public async Task StartAsync_PromotesExistingAccount_WhenRoleIsLesser()
    {
        var userStore = new FakeUserStore();
        var existingHash = new Pbkdf2PasswordHasher().Hash("original-password");
        await userStore.CreateAsync(new UserAccountEntity
        {
            Username = "admin",
            Email = "admin@example.com",
            PasswordHash = existingHash,
            Role = AccountRoles.ResumeAdmin,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        });

        var seeder = new AdminAccountSeeder(
            userStore, new Pbkdf2PasswordHasher(), BuildConfig(), Substitute.For<ILogger<AdminAccountSeeder>>());

        await seeder.StartAsync(CancellationToken.None);

        var promoted = await userStore.FindByUsernameAsync("admin");
        Assert.NotNull(promoted);
        Assert.Equal(AccountRoles.SuperAdmin, promoted!.Role);
        // Promotion must never reset the existing password.
        Assert.Equal(existingHash, promoted.PasswordHash);
    }

    [Fact]
    public async Task StartAsync_IsNoOp_WhenAccountAlreadySuperAdmin()
    {
        var userStore = new RecordingUserStore();
        var existingHash = new Pbkdf2PasswordHasher().Hash("original-password");
        await userStore.CreateAsync(new UserAccountEntity
        {
            Username = "admin",
            Email = "admin@example.com",
            PasswordHash = existingHash,
            Role = AccountRoles.SuperAdmin,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        });
        userStore.CreateCallCount = 0;

        var seeder = new AdminAccountSeeder(
            userStore, new Pbkdf2PasswordHasher(), BuildConfig(), Substitute.For<ILogger<AdminAccountSeeder>>());

        await seeder.StartAsync(CancellationToken.None);

        var unchanged = await userStore.FindByUsernameAsync("admin");
        Assert.NotNull(unchanged);
        Assert.Equal(AccountRoles.SuperAdmin, unchanged!.Role);
        Assert.Equal(existingHash, unchanged.PasswordHash);
        Assert.Equal(0, userStore.UpdateCallCount);
        Assert.Equal(0, userStore.CreateCallCount);
    }

    [Fact]
    public async Task StartAsync_SkipsSeeding_WhenAdminPasswordNotConfigured()
    {
        var userStore = new FakeUserStore();
        var seeder = new AdminAccountSeeder(
            userStore, new Pbkdf2PasswordHasher(), BuildConfig(adminPassword: null), Substitute.For<ILogger<AdminAccountSeeder>>());

        await seeder.StartAsync(CancellationToken.None);

        Assert.Null(await userStore.FindByUsernameAsync("admin"));
    }

    [Fact]
    public async Task StartAsync_DoesNotThrow_WhenUserStoreFails()
    {
        // A cold-start failure talking to Table Storage must never abort the whole Functions
        // host — that would take down anonymous endpoints (myResume) that have nothing to do
        // with auth. IHostedService.StartAsync throwing is exactly what does that.
        var userStore = Substitute.For<IUserStore>();
        userStore.FindByUsernameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<UserAccountEntity?>(new InvalidOperationException("storage unavailable")));

        var seeder = new AdminAccountSeeder(
            userStore, new Pbkdf2PasswordHasher(), BuildConfig(), Substitute.For<ILogger<AdminAccountSeeder>>());

        var exception = await Record.ExceptionAsync(() => seeder.StartAsync(CancellationToken.None));

        Assert.Null(exception);
    }

    /// <summary>In-memory IUserStore with call counters, used to assert true no-op behavior.</summary>
    private class RecordingUserStore : IUserStore
    {
        private readonly Dictionary<string, UserAccountEntity> _users = new();

        public int CreateCallCount { get; set; }
        public int UpdateCallCount { get; set; }

        public Task<UserAccountEntity?> FindByUsernameAsync(string username, CancellationToken cancellationToken = default)
        {
            _users.TryGetValue(username.Trim().ToLowerInvariant(), out var user);
            return Task.FromResult(user);
        }

        public Task<bool> CreateAsync(UserAccountEntity user, CancellationToken cancellationToken = default)
        {
            CreateCallCount++;
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
            UpdateCallCount++;
            _users[user.Username.Trim().ToLowerInvariant()] = user;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<UserAccountEntity>> ListAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<UserAccountEntity>>(_users.Values.ToList());
        }
    }
}
