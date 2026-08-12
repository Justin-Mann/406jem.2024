using Azure;
using Azure.Data.Tables;

namespace ResumeFunctions.Auth.Models
{
    /// <summary>
    /// Table Storage entity for a user account. PartitionKey is fixed so RowKey (the
    /// normalized/lowercased username) is the effective primary key within the "users" table.
    /// </summary>
    public class UserAccountEntity : ITableEntity
    {
        public const string PartitionKeyValue = "user";

        public string PartitionKey { get; set; } = PartitionKeyValue;
        public string RowKey { get; set; } = string.Empty;
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Role { get; set; } = AccountRoles.Visitor;
        public DateTimeOffset CreatedAtUtc { get; set; }

        // Public-facing label for #45's resume-poster directory - a username alone isn't a
        // great public label. Optional, with the effective display name (Username fallback)
        // computed where it's read rather than backfilled, so existing rows need no migration.
        public string DisplayName { get; set; } = string.Empty;

        // Persisted (not in-memory) failed-login tracking so lockout holds even when the
        // Consumption plan scales this Functions app out across multiple instances.
        public int FailedLoginAttempts { get; set; }
        public DateTimeOffset? LockoutUntilUtc { get; set; }
    }
}
