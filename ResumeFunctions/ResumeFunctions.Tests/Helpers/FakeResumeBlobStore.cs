using ResumeFunctions.Auth.Storage;

namespace ResumeFunctions.Tests.Helpers;

/// <summary>In-memory IResumeBlobStore so upload tests can assert on stored bytes without a real
/// Azure Storage account.</summary>
public class FakeResumeBlobStore : IResumeBlobStore
{
    public Dictionary<string, (byte[] Content, string ContentType)> Blobs { get; } = new();

    public Task<string> UploadAsync(string blobName, Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        using var buffer = new MemoryStream();
        content.CopyTo(buffer);
        Blobs[blobName] = (buffer.ToArray(), contentType);
        return Task.FromResult(blobName);
    }

    public Task<bool> DeleteAsync(string blobName, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Blobs.Remove(blobName));
    }
}
