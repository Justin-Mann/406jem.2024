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
    public async Task StartAsync_SeedsAdminAccount_WhenNotAlreadyPresent()
    {
        var userStore = new FakeUserStore();
        var seeder = new AdminAccountSeeder(
            userStore, new Pbkdf2PasswordHasher(), BuildConfig(), Substitute.For<ILogger<AdminAccountSeeder>>());

        await seeder.StartAsync(CancellationToken.None);

        var created = await userStore.FindByUsernameAsync("admin");
        Assert.NotNull(created);
        Assert.Equal(AccountRoles.ResumeAdmin, created!.Role);
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
}
