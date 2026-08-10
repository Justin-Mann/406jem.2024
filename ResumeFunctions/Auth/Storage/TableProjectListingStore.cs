using Azure;
using Azure.Data.Tables;
using ResumeFunctions.Auth.Models;

namespace ResumeFunctions.Auth.Storage
{
    public class TableProjectListingStore : IProjectListingStore
    {
        private const string TableName = "ProjectListings";
        private readonly TableClient _tableClient;

        public TableProjectListingStore(TableServiceClient tableServiceClient)
        {
            _tableClient = tableServiceClient.GetTableClient(TableName);
            _tableClient.CreateIfNotExists();
        }

        private static string NormalizeOwner(string ownerUserId) => ownerUserId.Trim().ToLowerInvariant();

        public async Task<IReadOnlyList<ProjectListingEntity>> ListByOwnerAsync(string ownerUserId, CancellationToken cancellationToken = default)
        {
            var owner = NormalizeOwner(ownerUserId);
            var results = new List<ProjectListingEntity>();
            await foreach (var entity in _tableClient.QueryAsync<ProjectListingEntity>(
                p => p.PartitionKey == ProjectListingEntity.PartitionKeyValue && p.OwnerUserId == owner, cancellationToken: cancellationToken))
            {
                results.Add(entity);
            }
            return results;
        }

        public async Task<ProjectListingEntity?> FindByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _tableClient.GetEntityAsync<ProjectListingEntity>(
                    ProjectListingEntity.PartitionKeyValue, id, cancellationToken: cancellationToken);
                return response.Value;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return null;
            }
        }

        public async Task<ProjectListingEntity?> FindFeaturedByOwnerAsync(string ownerUserId, CancellationToken cancellationToken = default)
        {
            var owner = NormalizeOwner(ownerUserId);
            await foreach (var entity in _tableClient.QueryAsync<ProjectListingEntity>(
                p => p.PartitionKey == ProjectListingEntity.PartitionKeyValue && p.OwnerUserId == owner && p.IsFeatured == true,
                cancellationToken: cancellationToken))
            {
                return entity;
            }
            return null;
        }

        public async Task AddAsync(ProjectListingEntity listing, CancellationToken cancellationToken = default)
        {
            listing.PartitionKey = ProjectListingEntity.PartitionKeyValue;
            await _tableClient.AddEntityAsync(listing, cancellationToken);
        }

        public async Task UpdateAsync(ProjectListingEntity listing, CancellationToken cancellationToken = default)
        {
            await _tableClient.UpdateEntityAsync(listing, listing.ETag, TableUpdateMode.Replace, cancellationToken);
        }

        public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            try
            {
                await _tableClient.DeleteEntityAsync(ProjectListingEntity.PartitionKeyValue, id, cancellationToken: cancellationToken);
                return true;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return false;
            }
        }
    }
}
