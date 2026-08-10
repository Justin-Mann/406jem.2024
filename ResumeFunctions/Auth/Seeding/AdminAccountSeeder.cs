using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ResumeFunctions.Auth.Models;
using ResumeFunctions.Auth.Security;
using ResumeFunctions.Auth.Storage;

namespace ResumeFunctions.Auth.Seeding
{
    /// <summary>
    /// Seeds the one SuperAdmin account from app settings on cold start, and promotes it to
    /// SuperAdmin if it already exists under a lesser role. There is deliberately no public
    /// registration or API path to the SuperAdmin role — this is the only way one gets created,
    /// so a wiped or freshly-stood-up environment can always bootstrap the first SuperAdmin.
    /// </summary>
    public class AdminAccountSeeder : IHostedService
    {
        private readonly IUserStore _userStore;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AdminAccountSeeder> _logger;

        public AdminAccountSeeder(
            IUserStore userStore,
            IPasswordHasher passwordHasher,
            IConfiguration configuration,
            ILogger<AdminAccountSeeder> logger)
        {
            _userStore = userStore;
            _passwordHasher = passwordHasher;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            // Seeding is best-effort and must never take the whole host down: a transient
            // Table Storage hiccup here previously failed IHostedService.StartAsync, which
            // aborts the entire Functions host startup and breaks anonymous endpoints
            // (myResume, resumes) that have nothing to do with auth.
            try
            {
                await SeedAdminAccountAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to seed admin account — continuing host startup without it.");
            }
        }

        private async Task SeedAdminAccountAsync(CancellationToken cancellationToken)
        {
            var username = _configuration["Auth:AdminUsername"] ?? "admin";
            var password = _configuration["Auth:AdminPassword"];

            if (string.IsNullOrWhiteSpace(password))
            {
                _logger.LogWarning("Auth:AdminPassword is not configured — skipping admin account seeding.");
                return;
            }

            var existing = await _userStore.FindByUsernameAsync(username, cancellationToken);
            if (existing is null)
            {
                var admin = new UserAccountEntity
                {
                    Username = username,
                    Email = _configuration["Auth:AdminEmail"] ?? string.Empty,
                    PasswordHash = _passwordHasher.Hash(password),
                    Role = AccountRoles.SuperAdmin,
                    CreatedAtUtc = DateTimeOffset.UtcNow,
                };

                await _userStore.CreateAsync(admin, cancellationToken);
                _logger.LogInformation("Seeded SuperAdmin account '{Username}'.", username);
                return;
            }

            if (existing.Role != AccountRoles.SuperAdmin)
            {
                // Only the role changes — never touch PasswordHash on an existing account, or
                // every cold start would reset whatever password the account currently has.
                existing.Role = AccountRoles.SuperAdmin;
                await _userStore.UpdateAsync(existing, cancellationToken);
                _logger.LogInformation("Promoted account '{Username}' to SuperAdmin.", username);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
