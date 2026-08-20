using System.Text.Json;
using Azure;
using Azure.Data.Tables;
using ResumeFunctions.Auth.Models;

namespace ResumeFunctions.Auth.Storage
{
    public class TableGitHubActivitySettingsStore : IGitHubActivitySettingsStore
    {
        private const string TableName = "GitHubActivitySettings";
        private readonly TableClient _tableClient;

        public TableGitHubActivitySettingsStore(TableServiceClient tableServiceClient)
        {
            _tableClient = tableServiceClient.GetTableClient(TableName);
            _tableClient.CreateIfNotExists();
        }

        public async Task<GitHubActivitySettingsEntity?> GetByOwnerAsync(string ownerUserId, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _tableClient.GetEntityAsync<GitHubActivitySettingsEntity>(
                    GitHubActivitySettingsEntity.PartitionKeyValue, NormalizeOwner(ownerUserId), cancellationToken: cancellationToken);
                return response.Value;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return null;
            }
        }

        public async Task<GitHubActivitySettingsEntity> SetAsync(
            string ownerUserId,
            bool enabled,
            string? gitHubUsername,
            int repoCount,
            IReadOnlyList<string> pinnedRepoNames,
            CancellationToken cancellationToken = default)
        {
            var normalizedOwner = NormalizeOwner(ownerUserId);
            var existing = await GetByOwnerAsync(normalizedOwner, cancellationToken);
            var entity = existing ?? new GitHubActivitySettingsEntity { RowKey = normalizedOwner };
            entity.OwnerUserId = normalizedOwner;
            entity.Enabled = enabled;
            entity.GitHubUsername = string.IsNullOrWhiteSpace(gitHubUsername) ? null : gitHubUsername.Trim();
            entity.RepoCount = repoCount;
            entity.PinnedRepoNamesJson = JsonSerializer.Serialize(pinnedRepoNames);

            if (existing is null)
            {
                await _tableClient.AddEntityAsync(entity, cancellationToken);
            }
            else
            {
                await _tableClient.UpdateEntityAsync(entity, entity.ETag, TableUpdateMode.Replace, cancellationToken);
            }

            return entity;
        }

        private static string NormalizeOwner(string ownerUserId) => ownerUserId.Trim().ToLowerInvariant();
    }
}
