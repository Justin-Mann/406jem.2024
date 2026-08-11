namespace ResumeFunctions.Auth.Storage
{
    /// <summary>Durable storage for uploaded resume PDFs (#29), separate from IResumeStore's
    /// Table Storage entities — a ResumeEntity created from an upload references its blob by
    /// name via ResumeEntity.BlobPath.</summary>
    public interface IResumeBlobStore
    {
        /// <returns>The blob name the content was stored under (same as <paramref name="blobName"/>).</returns>
        Task<string> UploadAsync(string blobName, Stream content, string contentType, CancellationToken cancellationToken = default);

        /// <returns>A seekable, fully-buffered stream positioned at the start of the blob's
        /// content (#30 — PdfPig needs random access, which a lazily-fetched blob stream isn't
        /// guaranteed to support).</returns>
        Task<Stream> DownloadAsync(string blobName, CancellationToken cancellationToken = default);

        Task<bool> DeleteAsync(string blobName, CancellationToken cancellationToken = default);
    }
}
