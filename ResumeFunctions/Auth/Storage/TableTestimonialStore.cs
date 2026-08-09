using Azure;
using Azure.Data.Tables;
using ResumeFunctions.Auth.Models;

namespace ResumeFunctions.Auth.Storage
{
    public class TableTestimonialStore : ITestimonialStore
    {
        private const string TableName = "Testimonials";
        private readonly TableClient _tableClient;

        public TableTestimonialStore(TableServiceClient tableServiceClient)
        {
            _tableClient = tableServiceClient.GetTableClient(TableName);
            _tableClient.CreateIfNotExists();
        }

        public async Task<IReadOnlyList<TestimonialEntity>> ListAsync(CancellationToken cancellationToken = default)
        {
            var results = new List<TestimonialEntity>();
            await foreach (var entity in _tableClient.QueryAsync<TestimonialEntity>(
                t => t.PartitionKey == TestimonialEntity.PartitionKeyValue, cancellationToken: cancellationToken))
            {
                results.Add(entity);
            }
            return results.OrderByDescending(t => t.CreatedAtUtc).ToList();
        }

        public async Task AddAsync(TestimonialEntity testimonial, CancellationToken cancellationToken = default)
        {
            testimonial.PartitionKey = TestimonialEntity.PartitionKeyValue;
            await _tableClient.AddEntityAsync(testimonial, cancellationToken);
        }

        public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            try
            {
                await _tableClient.DeleteEntityAsync(TestimonialEntity.PartitionKeyValue, id, cancellationToken: cancellationToken);
                return true;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return false;
            }
        }
    }
}
