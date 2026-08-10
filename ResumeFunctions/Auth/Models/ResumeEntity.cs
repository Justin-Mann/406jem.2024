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

        /// <summary>Created from an upload (#29) and not yet parsed/reviewed — excluded from
        /// public visibility regardless of IsFeatured/SiteConfig.</summary>
        public const string StatusDraft = "Draft";

        /// <summary>Has a complete, reviewed Payload and is eligible for public visibility via
        /// IsFeatured + SiteConfig, same as the pre-#29 behavior.</summary>
        public const string StatusPublished = "Published";

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

        /// <summary>StatusDraft or StatusPublished. Defaults to Draft so any code path that
        /// forgets to set it explicitly fails closed rather than becoming publicly eligible.</summary>
        public string Status { get; set; } = StatusDraft;

        /// <summary>Blob name within the resume-uploads container (#29) for the source PDF, if
        /// this resume originated from an upload rather than the JSON create/update endpoints.</summary>
        public string? BlobPath { get; set; }

        public string? OriginalFileName { get; set; }
        public string? ContentType { get; set; }
        public long? FileSizeBytes { get; set; }

        public DateTimeOffset CreatedAtUtc { get; set; }
        public DateTimeOffset UpdatedAtUtc { get; set; }
    }
}
