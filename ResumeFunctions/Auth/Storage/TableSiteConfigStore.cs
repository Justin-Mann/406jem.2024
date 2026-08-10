using Azure;
using Azure.Data.Tables;
using ResumeFunctions.Auth.Models;

namespace ResumeFunctions.Auth.Storage
{
    public class TableSiteConfigStore : ISiteConfigStore
    {
        private const string TableName = "SiteConfig";
        private readonly TableClient _tableClient;

        public TableSiteConfigStore(TableServiceClient tableServiceClient)
        {
            _tableClient = tableServiceClient.GetTableClient(TableName);
            _tableClient.CreateIfNotExists();
        }

        public async Task<SiteConfigEntity?> GetAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _tableClient.GetEntityAsync<SiteConfigEntity>(
                    SiteConfigEntity.PartitionKeyValue, SiteConfigEntity.RowKeyValue, cancellationToken: cancellationToken);
                return response.Value;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return null;
            }
        }

        public async Task<SiteConfigEntity> SetAsync(string? publicResumeOwnerId, string? publicProjectsOwnerId, CancellationToken cancellationToken = default)
        {
            var existing = await GetAsync(cancellationToken);
            var entity = existing ?? new SiteConfigEntity();
            entity.PublicResumeOwnerId = NormalizeOwner(publicResumeOwnerId);
            entity.PublicProjectsOwnerId = NormalizeOwner(publicProjectsOwnerId);

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

        private static string? NormalizeOwner(string? ownerUserId) =>
            string.IsNullOrWhiteSpace(ownerUserId) ? null : ownerUserId.Trim().ToLowerInvariant();
    }
}
