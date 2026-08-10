using Azure;
using Azure.Data.Tables;
using ResumeFunctions.Auth.Models;

namespace ResumeFunctions.Auth.Storage
{
    public class TableResumeStore : IResumeStore
    {
        private const string TableName = "Resumes";
        private readonly TableClient _tableClient;

        public TableResumeStore(TableServiceClient tableServiceClient)
        {
            _tableClient = tableServiceClient.GetTableClient(TableName);
            _tableClient.CreateIfNotExists();
        }

        private static string NormalizeOwner(string ownerUserId) => ownerUserId.Trim().ToLowerInvariant();

        public async Task<IReadOnlyList<ResumeEntity>> ListByOwnerAsync(string ownerUserId, CancellationToken cancellationToken = default)
        {
            var owner = NormalizeOwner(ownerUserId);
            var results = new List<ResumeEntity>();
            await foreach (var entity in _tableClient.QueryAsync<ResumeEntity>(
                r => r.PartitionKey == ResumeEntity.PartitionKeyValue && r.OwnerUserId == owner, cancellationToken: cancellationToken))
            {
                results.Add(entity);
            }
            return results;
        }

        public async Task<ResumeEntity?> FindByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _tableClient.GetEntityAsync<ResumeEntity>(
                    ResumeEntity.PartitionKeyValue, id, cancellationToken: cancellationToken);
                return response.Value;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return null;
            }
        }

        public async Task<ResumeEntity?> FindFeaturedByOwnerAsync(string ownerUserId, CancellationToken cancellationToken = default)
        {
            var owner = NormalizeOwner(ownerUserId);
            await foreach (var entity in _tableClient.QueryAsync<ResumeEntity>(
                r => r.PartitionKey == ResumeEntity.PartitionKeyValue && r.OwnerUserId == owner && r.IsFeatured == true,
                cancellationToken: cancellationToken))
            {
                return entity;
            }
            return null;
        }

        public async Task AddAsync(ResumeEntity resume, CancellationToken cancellationToken = default)
        {
            resume.PartitionKey = ResumeEntity.PartitionKeyValue;
            await _tableClient.AddEntityAsync(resume, cancellationToken);
        }

        public async Task UpdateAsync(ResumeEntity resume, CancellationToken cancellationToken = default)
        {
            await _tableClient.UpdateEntityAsync(resume, resume.ETag, TableUpdateMode.Replace, cancellationToken);
        }

        public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            try
            {
                await _tableClient.DeleteEntityAsync(ResumeEntity.PartitionKeyValue, id, cancellationToken: cancellationToken);
                return true;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return false;
            }
        }
    }
}
