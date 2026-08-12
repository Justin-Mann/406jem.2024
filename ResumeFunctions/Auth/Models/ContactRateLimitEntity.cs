using Azure;
using Azure.Data.Tables;

namespace ResumeFunctions.Auth.Models
{
    /// <summary>
    /// Table Storage entity backing #45's contact-relay rate limit. PartitionKey is fixed so
    /// RowKey (a normalized caller key - see ResumePostersApi.GetClientKey) is the effective
    /// primary key. Persisted (not in-memory) for the same reason as UserAccountEntity's
    /// lockout fields: an in-memory counter would reset per Consumption-plan instance.
    /// </summary>
    public class ContactRateLimitEntity : ITableEntity
    {
        public const string PartitionKeyValue = "contactRateLimit";

        public string PartitionKey { get; set; } = PartitionKeyValue;
        public string RowKey { get; set; } = string.Empty;
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        public int AttemptCount { get; set; }
        public DateTimeOffset WindowStartUtc { get; set; }
    }
}
