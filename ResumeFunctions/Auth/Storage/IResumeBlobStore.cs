namespace ResumeFunctions.Auth.Storage
{
    /// <summary>Durable storage for uploaded resume PDFs (#29), separate from IResumeStore's
    /// Table Storage entities — a ResumeEntity created from an upload references its blob by
    /// name via ResumeEntity.BlobPath.</summary>
    public interface IResumeBlobStore
    {
        /// <returns>The blob name the content was stored under (same as <paramref name="blobName"/>).</returns>
        Task<string> UploadAsync(string blobName, Stream content, string contentType, CancellationToken cancellationToken = default);

        Task<bool> DeleteAsync(string blobName, CancellationToken cancellationToken = default);
    }
}
