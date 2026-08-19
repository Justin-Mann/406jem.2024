using Azure;
using Azure.Data.Tables;

namespace ResumeFunctions.Auth.Models
{
    /// <summary>
    /// Table Storage entity for one Resume Admin's GitHub Activity display configuration (#69).
    /// Fixed PartitionKey; RowKey is the normalized (trimmed, lowercased) owner username — same
    /// normalization as TableUserStore.NormalizeUsername — since each owner has at most one row.
    /// No GitHub API calls happen anywhere in this issue; this is purely storage for what an
    /// admin has configured. The actual fetch/display of GitHub data is #68.
    /// </summary>
    public class GitHubActivitySettingsEntity : ITableEntity
    {
        public const string PartitionKeyValue = "githubactivitysettings";

        public string PartitionKey { get; set; } = PartitionKeyValue;
        public string RowKey { get; set; } = string.Empty;
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        public string OwnerUserId { get; set; } = string.Empty;

        /// <summary>Hidden until an admin explicitly turns it on.</summary>
        public bool Enabled { get; set; }

        public string? GitHubUsername { get; set; }

        public int RepoCount { get; set; } = 5;

        /// <summary>Table Storage is flat, so the pinned repo name list is stored as a JSON array
        /// string, same technique ResumeEntity.PayloadJson uses for its structured payload.</summary>
        public string PinnedRepoNamesJson { get; set; } = "[]";
    }
}
