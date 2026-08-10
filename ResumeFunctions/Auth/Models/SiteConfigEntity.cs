using Azure;
using Azure.Data.Tables;

namespace ResumeFunctions.Auth.Models
{
    /// <summary>
    /// Single-row Table Storage entity (fixed PartitionKey/RowKey) deciding which owner's
    /// featured resume/project listing is currently live on the public site. Only a SuperAdmin
    /// may change it. Absent (no row yet) is a valid state — public endpoints fall back to their
    /// pre-#28 behavior in that case rather than erroring.
    /// </summary>
    public class SiteConfigEntity : ITableEntity
    {
        public const string PartitionKeyValue = "config";
        public const string RowKeyValue = "site";

        public string PartitionKey { get; set; } = PartitionKeyValue;
        public string RowKey { get; set; } = RowKeyValue;
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        public string? PublicResumeOwnerId { get; set; }
        public string? PublicProjectsOwnerId { get; set; }
    }
}
