using Azure;
using Azure.Data.Tables;

namespace ResumeFunctions.Auth.Models
{
    /// <summary>
    /// The minimal gated feature proving the login chain works end-to-end: a logged-in
    /// visitor or admin can leave a note, and only an admin can remove one.
    /// </summary>
    public class TestimonialEntity : ITableEntity
    {
        public const string PartitionKeyValue = "testimonial";

        public string PartitionKey { get; set; } = PartitionKeyValue;
        public string RowKey { get; set; } = Guid.NewGuid().ToString();
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        public string AuthorUsername { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTimeOffset CreatedAtUtc { get; set; }
    }
}
