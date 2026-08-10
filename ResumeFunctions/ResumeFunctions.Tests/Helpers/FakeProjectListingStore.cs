using ResumeFunctions.Auth.Models;
using ResumeFunctions.Auth.Storage;

namespace ResumeFunctions.Tests.Helpers;

/// <summary>In-memory IProjectListingStore for tests that need real mutation.</summary>
public class FakeProjectListingStore : IProjectListingStore
{
    private readonly Dictionary<string, ProjectListingEntity> _listings = new();

    public Task<IReadOnlyList<ProjectListingEntity>> ListByOwnerAsync(string ownerUserId, CancellationToken cancellationToken = default)
    {
        var owner = ownerUserId.Trim().ToLowerInvariant();
        IReadOnlyList<ProjectListingEntity> results = _listings.Values.Where(l => l.OwnerUserId == owner).ToList();
        return Task.FromResult(results);
    }

    public Task<ProjectListingEntity?> FindByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        _listings.TryGetValue(id, out var listing);
        return Task.FromResult(listing);
    }

    public Task<ProjectListingEntity?> FindFeaturedByOwnerAsync(string ownerUserId, CancellationToken cancellationToken = default)
    {
        var owner = ownerUserId.Trim().ToLowerInvariant();
        var featured = _listings.Values.FirstOrDefault(l => l.OwnerUserId == owner && l.IsFeatured);
        return Task.FromResult(featured);
    }

    public Task AddAsync(ProjectListingEntity listing, CancellationToken cancellationToken = default)
    {
        _listings[listing.RowKey] = listing;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(ProjectListingEntity listing, CancellationToken cancellationToken = default)
    {
        _listings[listing.RowKey] = listing;
        return Task.CompletedTask;
    }

    public Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_listings.Remove(id));
    }
}
