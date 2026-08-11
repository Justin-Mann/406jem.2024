using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace ResumeFunctions.Auth.Storage
{
    public class BlobResumeStore : IResumeBlobStore
    {
        private const string ContainerName = "resume-uploads";
        private readonly BlobContainerClient _containerClient;

        public BlobResumeStore(BlobServiceClient blobServiceClient)
        {
            _containerClient = blobServiceClient.GetBlobContainerClient(ContainerName);
            // No PublicAccessType specified — the container (and every blob in it) stays
            // private. Uploaded resumes are draft/unreviewed and may contain PII.
            _containerClient.CreateIfNotExists();
        }

        public async Task<string> UploadAsync(string blobName, Stream content, string contentType, CancellationToken cancellationToken = default)
        {
            var blobClient = _containerClient.GetBlobClient(blobName);
            var options = new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = contentType },
            };
            await blobClient.UploadAsync(content, options, cancellationToken);
            return blobName;
        }

        public async Task<Stream> DownloadAsync(string blobName, CancellationToken cancellationToken = default)
        {
            var blobClient = _containerClient.GetBlobClient(blobName);
            var buffer = new MemoryStream();
            await blobClient.DownloadToAsync(buffer, cancellationToken);
            buffer.Position = 0;
            return buffer;
        }

        public async Task<bool> DeleteAsync(string blobName, CancellationToken cancellationToken = default)
        {
            var response = await _containerClient.GetBlobClient(blobName).DeleteIfExistsAsync(cancellationToken: cancellationToken);
            return response.Value;
        }
    }
}
