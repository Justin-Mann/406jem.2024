using Azure;
using Azure.Data.Tables;
using ResumeFunctions.Auth.Models;

namespace ResumeFunctions.Auth.Storage
{
    public class TableContactRateLimitStore : IContactRateLimitStore
    {
        private const string TableName = "ContactRateLimits";
        private const int MaxAttemptsPerWindow = 5;
        private static readonly TimeSpan Window = TimeSpan.FromMinutes(15);

        private readonly TableClient _tableClient;

        public TableContactRateLimitStore(TableServiceClient tableServiceClient)
        {
            _tableClient = tableServiceClient.GetTableClient(TableName);
            try
            {
                _tableClient.CreateIfNotExists();
            }
            catch (RequestFailedException)
            {
                // Best-effort: see TableUserStore for why this must not throw.
            }
        }

        public async Task<bool> TryRecordAttemptAsync(string clientKey, CancellationToken cancellationToken = default)
        {
            var rowKey = NormalizeKey(clientKey);
            var now = DateTimeOffset.UtcNow;

            ContactRateLimitEntity? entity;
            try
            {
                var response = await _tableClient.GetEntityAsync<ContactRateLimitEntity>(
                    ContactRateLimitEntity.PartitionKeyValue, rowKey, cancellationToken: cancellationToken);
                entity = response.Value;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                entity = null;
            }

            if (entity is null)
            {
                var created = new ContactRateLimitEntity
                {
                    RowKey = rowKey,
                    AttemptCount = 1,
                    WindowStartUtc = now,
                };

                try
                {
                    await _tableClient.AddEntityAsync(created, cancellationToken);
                }
                catch (RequestFailedException ex) when (ex.Status == 409)
                {
                    // Lost a race with a concurrent first request for the same key - treat this
                    // one as allowed too; worst case is one extra attempt in the window.
                }

                return true;
            }

            if (now - entity.WindowStartUtc > Window)
            {
                entity.AttemptCount = 1;
                entity.WindowStartUtc = now;
                await _tableClient.UpdateEntityAsync(entity, entity.ETag, TableUpdateMode.Replace, cancellationToken);
                return true;
            }

            if (entity.AttemptCount >= MaxAttemptsPerWindow)
            {
                return false;
            }

            entity.AttemptCount++;
            await _tableClient.UpdateEntityAsync(entity, entity.ETag, TableUpdateMode.Replace, cancellationToken);
            return true;
        }

        private static string NormalizeKey(string clientKey) => clientKey.Trim().ToLowerInvariant();
    }
}
