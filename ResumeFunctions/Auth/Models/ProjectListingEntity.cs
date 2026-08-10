using Azure;
using Azure.Data.Tables;

namespace ResumeFunctions.Auth.Models
{
    /// <summary>
    /// Table Storage entity for one Resume Admin's project listing. Same shape/rationale as
    /// ResumeEntity — see that type for why the payload is a JSON blob column.
    /// </summary>
    public class ProjectListingEntity : ITableEntity
    {
        public const string PartitionKeyValue = "projectlisting";

        public string PartitionKey { get; set; } = PartitionKeyValue;
        public string RowKey { get; set; } = Guid.NewGuid().ToString();
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        public string OwnerUserId { get; set; } = string.Empty;

        /// <summary>The owner's primary project listing — exactly one true per owner at a time.
        /// Not necessarily the site's public listing; see SiteConfigEntity.PublicProjectsOwnerId.</summary>
        public bool IsFeatured { get; set; }

        public string PayloadJson { get; set; } = string.Empty;

        public DateTimeOffset CreatedAtUtc { get; set; }
        public DateTimeOffset UpdatedAtUtc { get; set; }
    }
}
