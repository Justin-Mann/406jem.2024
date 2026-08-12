using Azure;
using Azure.Data.Tables;
using ResumeFunctions.Auth.Models;

namespace ResumeFunctions.Auth.Storage
{
    public class TableUserStore : IUserStore
    {
        private const string TableName = "Users";
        private readonly TableClient _tableClient;

        public TableUserStore(TableServiceClient tableServiceClient)
        {
            _tableClient = tableServiceClient.GetTableClient(TableName);
            try
            {
                _tableClient.CreateIfNotExists();
            }
            catch (RequestFailedException)
            {
                // Best-effort: a transient failure here must not take down the whole host
                // (this constructor runs during DI graph creation for a hosted service, before
                // the Functions host starts accepting requests). Table operations below already
                // treat a missing table as equivalent to "not found" / surface their own errors.
            }
        }

        private static string NormalizeUsername(string username) => username.Trim().ToLowerInvariant();

        public async Task<UserAccountEntity?> FindByUsernameAsync(string username, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _tableClient.GetEntityAsync<UserAccountEntity>(
                    UserAccountEntity.PartitionKeyValue, NormalizeUsername(username), cancellationToken: cancellationToken);
                return response.Value;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return null;
            }
        }

        public async Task<bool> CreateAsync(UserAccountEntity user, CancellationToken cancellationToken = default)
        {
            user.PartitionKey = UserAccountEntity.PartitionKeyValue;
            user.RowKey = NormalizeUsername(user.Username);
            try
            {
                await _tableClient.AddEntityAsync(user, cancellationToken);
                return true;
            }
            catch (RequestFailedException ex) when (ex.Status == 409)
            {
                return false;
            }
        }

        public async Task UpdateAsync(UserAccountEntity user, CancellationToken cancellationToken = default)
        {
            await _tableClient.UpdateEntityAsync(user, user.ETag, TableUpdateMode.Replace, cancellationToken);
        }

        public async Task<IReadOnlyList<UserAccountEntity>> ListAsync(CancellationToken cancellationToken = default)
        {
            var results = new List<UserAccountEntity>();
            await foreach (var entity in _tableClient.QueryAsync<UserAccountEntity>(
                u => u.PartitionKey == UserAccountEntity.PartitionKeyValue, cancellationToken: cancellationToken))
            {
                results.Add(entity);
            }
            return results;
        }
    }
}
