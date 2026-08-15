using System.Text;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace ResumeFunctions.Auth.Storage
{
    public class BlobResumeSnapshotStore : IResumeSnapshotStore
    {
        private const string ContainerName = "resume-snapshots";
        private readonly BlobContainerClient _containerClient;

        public BlobResumeSnapshotStore(BlobServiceClient blobServiceClient)
        {
            _containerClient = blobServiceClient.GetBlobContainerClient(ContainerName);
            // No PublicAccessType specified — same private-by-default posture as
            // resume-uploads; a fallback snapshot carries the same PII as the live resume.
            _containerClient.CreateIfNotExists();
        }

        private static string BlobName(string ownerUserId) => $"{ownerUserId.Trim().ToLowerInvariant()}.json";

        public async Task SaveAsync(string ownerUserId, string payloadJson, CancellationToken cancellationToken = default)
        {
            var blobClient = _containerClient.GetBlobClient(BlobName(ownerUserId));
            using var content = new MemoryStream(Encoding.UTF8.GetBytes(payloadJson));
            var options = new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = "application/json" },
            };
            // Overwrites unconditionally (no access conditions set) — one blob per owner, so
            // each save replaces the previous snapshot.
            await blobClient.UploadAsync(content, options, cancellationToken);
        }

        public async Task<string?> GetAsync(string ownerUserId, CancellationToken cancellationToken = default)
        {
            var blobClient = _containerClient.GetBlobClient(BlobName(ownerUserId));
            try
            {
                var response = await blobClient.DownloadContentAsync(cancellationToken);
                return response.Value.Content.ToString();
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return null;
            }
        }

        public async Task DeleteAsync(string ownerUserId, CancellationToken cancellationToken = default)
        {
            var blobClient = _containerClient.GetBlobClient(BlobName(ownerUserId));
            await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
        }
    }
}
