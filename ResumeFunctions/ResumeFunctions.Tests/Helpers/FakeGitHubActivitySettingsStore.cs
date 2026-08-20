using System.Text.Json;
using ResumeFunctions.Auth.Models;
using ResumeFunctions.Auth.Storage;

namespace ResumeFunctions.Tests.Helpers;

/// <summary>In-memory IGitHubActivitySettingsStore for tests that need real mutation, keyed by
/// normalized owner username (mirrors TableGitHubActivitySettingsStore's RowKey scheme).</summary>
public class FakeGitHubActivitySettingsStore : IGitHubActivitySettingsStore
{
    private readonly Dictionary<string, GitHubActivitySettingsEntity> _byOwner = new();

    public Task<GitHubActivitySettingsEntity?> GetByOwnerAsync(string ownerUserId, CancellationToken cancellationToken = default)
    {
        _byOwner.TryGetValue(Normalize(ownerUserId), out var entity);
        return Task.FromResult(entity);
    }

    public Task<GitHubActivitySettingsEntity> SetAsync(
        string ownerUserId,
        bool enabled,
        string? gitHubUsername,
        int repoCount,
        IReadOnlyList<string> pinnedRepoNames,
        CancellationToken cancellationToken = default)
    {
        var normalizedOwner = Normalize(ownerUserId);
        var entity = new GitHubActivitySettingsEntity
        {
            RowKey = normalizedOwner,
            OwnerUserId = normalizedOwner,
            Enabled = enabled,
            GitHubUsername = string.IsNullOrWhiteSpace(gitHubUsername) ? null : gitHubUsername.Trim(),
            RepoCount = repoCount,
            PinnedRepoNamesJson = JsonSerializer.Serialize(pinnedRepoNames),
        };
        _byOwner[normalizedOwner] = entity;
        return Task.FromResult(entity);
    }

    private static string Normalize(string ownerUserId) => ownerUserId.Trim().ToLowerInvariant();
}
