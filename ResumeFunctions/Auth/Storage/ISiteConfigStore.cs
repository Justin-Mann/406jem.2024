using ResumeFunctions.Auth.Models;

namespace ResumeFunctions.Auth.Storage
{
    public interface ISiteConfigStore
    {
        /// <returns>null if no SiteConfig row has ever been written.</returns>
        Task<SiteConfigEntity?> GetAsync(CancellationToken cancellationToken = default);

        Task<SiteConfigEntity> SetAsync(string? publicResumeOwnerId, string? publicProjectsOwnerId, CancellationToken cancellationToken = default);
    }
}
