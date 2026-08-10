using ResumeFunctions.Auth.Models;
using ResumeFunctions.Auth.Storage;

namespace ResumeFunctions.Tests.Helpers;

/// <summary>In-memory ISiteConfigStore for tests that need real mutation.</summary>
public class FakeSiteConfigStore : ISiteConfigStore
{
    private SiteConfigEntity? _config;

    public Task<SiteConfigEntity?> GetAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(_config);

    public Task<SiteConfigEntity> SetAsync(string? publicResumeOwnerId, string? publicProjectsOwnerId, CancellationToken cancellationToken = default)
    {
        _config = new SiteConfigEntity
        {
            PublicResumeOwnerId = string.IsNullOrWhiteSpace(publicResumeOwnerId) ? null : publicResumeOwnerId.Trim().ToLowerInvariant(),
            PublicProjectsOwnerId = string.IsNullOrWhiteSpace(publicProjectsOwnerId) ? null : publicProjectsOwnerId.Trim().ToLowerInvariant(),
        };
        return Task.FromResult(_config);
    }
}
