using Azure;
using Azure.Data.Tables;

namespace ResumeFunctions.Auth.Models
{
    /// <summary>
    /// Table Storage entity for one Resume Admin's resume. PartitionKey is fixed; RowKey (a
    /// GUID) is the id used in routes. Table Storage entities are flat, so the full structured
    /// digital-resume payload (same shape as DigitalResumeModel) is stored as a JSON blob in
    /// PayloadJson rather than as native columns.
    /// </summary>
    public class ResumeEntity : ITableEntity
    {
        public const string PartitionKeyValue = "resume";

        public string PartitionKey { get; set; } = PartitionKeyValue;
        public string RowKey { get; set; } = Guid.NewGuid().ToString();
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        /// <summary>Normalized (trimmed, lowercased) username — same normalization as
        /// TableUserStore.NormalizeUsername, since there's no separate numeric user id.</summary>
        public string OwnerUserId { get; set; } = string.Empty;

        /// <summary>The owner's "primary" resume — exactly one true per owner at a time. Not
        /// necessarily the site's public resume; see SiteConfigEntity.PublicResumeOwnerId.</summary>
        public bool IsFeatured { get; set; }

        public string PayloadJson { get; set; } = string.Empty;

        public DateTimeOffset CreatedAtUtc { get; set; }
        public DateTimeOffset UpdatedAtUtc { get; set; }
    }
}
