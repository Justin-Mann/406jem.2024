using ResumeFunctions.Auth.Models;

namespace ResumeFunctions.Auth.Storage
{
    public interface IGitHubActivitySettingsStore
    {
        /// <returns>null if no settings row has ever been written for this owner.</returns>
        Task<GitHubActivitySettingsEntity?> GetByOwnerAsync(string ownerUserId, CancellationToken cancellationToken = default);

        Task<GitHubActivitySettingsEntity> SetAsync(
            string ownerUserId,
            bool enabled,
            string? gitHubUsername,
            int repoCount,
            IReadOnlyList<string> pinnedRepoNames,
            CancellationToken cancellationToken = default);
    }
}
