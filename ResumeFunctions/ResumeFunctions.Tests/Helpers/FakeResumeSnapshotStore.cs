using ResumeFunctions.Auth.Storage;

namespace ResumeFunctions.Tests.Helpers;

/// <summary>In-memory IResumeSnapshotStore for tests. Set <see cref="ThrowOnSave"/> to simulate
/// a snapshot write failure without needing a real Azure Storage account.</summary>
public class FakeResumeSnapshotStore : IResumeSnapshotStore
{
    private readonly Dictionary<string, string> _snapshots = new();

    public bool ThrowOnSave { get; set; }
    public bool ThrowOnGet { get; set; }
    public bool ThrowOnDelete { get; set; }

    public Task SaveAsync(string ownerUserId, string payloadJson, CancellationToken cancellationToken = default)
    {
        if (ThrowOnSave)
        {
            throw new InvalidOperationException("Simulated snapshot write failure.");
        }

        _snapshots[ownerUserId.Trim().ToLowerInvariant()] = payloadJson;
        return Task.CompletedTask;
    }

    public Task<string?> GetAsync(string ownerUserId, CancellationToken cancellationToken = default)
    {
        if (ThrowOnGet)
        {
            throw new InvalidOperationException("Simulated snapshot read failure.");
        }

        _snapshots.TryGetValue(ownerUserId.Trim().ToLowerInvariant(), out var json);
        return Task.FromResult(json);
    }

    public Task DeleteAsync(string ownerUserId, CancellationToken cancellationToken = default)
    {
        if (ThrowOnDelete)
        {
            throw new InvalidOperationException("Simulated snapshot delete failure.");
        }

        _snapshots.Remove(ownerUserId.Trim().ToLowerInvariant());
        return Task.CompletedTask;
    }
}
