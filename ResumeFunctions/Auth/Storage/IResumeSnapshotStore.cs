namespace ResumeFunctions.Auth.Storage
{
    /// <summary>
    /// Durable per-owner fallback snapshot of a Resume Admin's featured resume (#39) — one JSON
    /// blob per owner, refreshed whenever their featured resume's content changes. Read paths
    /// (ResumeApi) consult this only after the live IResumeStore lookup fails to resolve a
    /// featured resume for that owner; the single global static file remains the last resort
    /// after that. Writing a snapshot is the caller's responsibility to treat as best-effort — a
    /// failure here must never block a resume save/publish from succeeding.
    /// </summary>
    public interface IResumeSnapshotStore
    {
        Task SaveAsync(string ownerUserId, string payloadJson, CancellationToken cancellationToken = default);

        /// <returns>null if no snapshot has ever been saved for this owner.</returns>
        Task<string?> GetAsync(string ownerUserId, CancellationToken cancellationToken = default);
    }
}
