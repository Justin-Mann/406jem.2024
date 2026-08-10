using ResumeFunctions.Auth.Models;
using ResumeFunctions.Auth.Storage;

namespace ResumeFunctions.Tests.Helpers;

/// <summary>In-memory IResumeStore for tests that need real mutation (featured-flag toggling, etc.).</summary>
public class FakeResumeStore : IResumeStore
{
    private readonly Dictionary<string, ResumeEntity> _resumes = new();

    public Task<IReadOnlyList<ResumeEntity>> ListByOwnerAsync(string ownerUserId, CancellationToken cancellationToken = default)
    {
        var owner = ownerUserId.Trim().ToLowerInvariant();
        IReadOnlyList<ResumeEntity> results = _resumes.Values.Where(r => r.OwnerUserId == owner).ToList();
        return Task.FromResult(results);
    }

    public Task<ResumeEntity?> FindByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        _resumes.TryGetValue(id, out var resume);
        return Task.FromResult(resume);
    }

    public Task<ResumeEntity?> FindFeaturedByOwnerAsync(string ownerUserId, CancellationToken cancellationToken = default)
    {
        var owner = ownerUserId.Trim().ToLowerInvariant();
        var featured = _resumes.Values.FirstOrDefault(r => r.OwnerUserId == owner && r.IsFeatured);
        return Task.FromResult(featured);
    }

    public Task AddAsync(ResumeEntity resume, CancellationToken cancellationToken = default)
    {
        _resumes[resume.RowKey] = resume;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(ResumeEntity resume, CancellationToken cancellationToken = default)
    {
        _resumes[resume.RowKey] = resume;
        return Task.CompletedTask;
    }

    public Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_resumes.Remove(id));
    }
}
