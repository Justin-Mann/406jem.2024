using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ResumeFunctions.Auth.Models;
using ResumeFunctions.Auth.Security;
using ResumeFunctions.Auth.Storage;

namespace ResumeFunctions.Auth.Seeding
{
    /// <summary>
    /// Seeds the one admin account from app settings on cold start. There is deliberately no
    /// public registration path to the admin role — this is the only way an admin account gets
    /// created.
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
            var username = _configuration["Auth:AdminUsername"] ?? "admin";
            var password = _configuration["Auth:AdminPassword"];

            if (string.IsNullOrWhiteSpace(password))
            {
                _logger.LogWarning("Auth:AdminPassword is not configured — skipping admin account seeding.");
                return;
            }

            var existing = await _userStore.FindByUsernameAsync(username, cancellationToken);
            if (existing is not null)
            {
                return;
            }

            var admin = new UserAccountEntity
            {
                Username = username,
                Email = _configuration["Auth:AdminEmail"] ?? string.Empty,
                PasswordHash = _passwordHasher.Hash(password),
                Role = AccountRoles.ResumeAdmin,
                CreatedAtUtc = DateTimeOffset.UtcNow,
            };

            await _userStore.CreateAsync(admin, cancellationToken);
            _logger.LogInformation("Seeded admin account '{Username}'.", username);
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
