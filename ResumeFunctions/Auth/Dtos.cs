using ResumeFunctions.Models;

namespace ResumeFunctions.Auth.Dtos
{
    public record RegisterRequest(string? Username, string? Email, string? Password);

    public record LoginRequest(string? Username, string? Password);

    /// <summary>No token field - the JWT lives only in the httpOnly session cookie (#47),
    /// never in a response body a script could read.</summary>
    public record AuthResponse(string Username, string Role, DateTimeOffset ExpiresAtUtc);

    /// <summary>Body of GET /api/auth/me, used by clients to hydrate "am I logged in, as
    /// whom" on load since the session cookie itself is deliberately unreadable from JS.</summary>
    public record MeResponse(string Username, string Role);

    public record TestimonialDto(string Id, string AuthorUsername, string Message, DateTimeOffset CreatedAtUtc);

    public record CreateTestimonialRequest(string? Message);

    public record ErrorResponse(string Message);

    public record ResumeDto(
        string Id,
        string OwnerUserId,
        bool IsFeatured,
        DigitalResumeModel? Payload,
        DateTimeOffset CreatedAtUtc,
        DateTimeOffset UpdatedAtUtc,
        string Status = "Published",
        string? OriginalFileName = null,
        string? ContentType = null,
        long? FileSizeBytes = null);

    /// <summary>OwnerUserId is only honored when the caller is a SuperAdmin creating on behalf
    /// of another owner — a plain ResumeAdmin is always pinned to their own username.</summary>
    public record CreateOrUpdateResumeRequest(string? OwnerUserId, bool IsFeatured, DigitalResumeModel? Payload);

    /// <summary>Result of a POST /resumes/{id}/parse call (#30). ParseSucceeded is false — with
    /// the resume otherwise unchanged and still Draft — when text extraction or the AI call
    /// failed or returned something unusable; this is a normal, non-error outcome the client
    /// should fall back to manual entry for, not a request failure.</summary>
    public record ParseResumeResponse(ResumeDto Resume, bool ParseSucceeded, string? Message);

    public record ProjectListingDto(string Id, string OwnerUserId, bool IsFeatured, ProjectListingModel? Payload, DateTimeOffset CreatedAtUtc, DateTimeOffset UpdatedAtUtc);

    public record CreateOrUpdateProjectListingRequest(string? OwnerUserId, bool IsFeatured, ProjectListingModel? Payload);

    public record SiteConfigDto(string? PublicResumeOwnerId, string? PublicProjectsOwnerId);

    public record UpdateSiteConfigRequest(string? PublicResumeOwnerId, string? PublicProjectsOwnerId);

    /// <summary>#69's per-owner GitHub Activity display configuration. No GitHub API calls
    /// happen anywhere in this DTO's endpoints — purely storage for what an admin has
    /// configured; the actual fetch/display of GitHub data is #68.</summary>
    public record GitHubActivitySettingsDto(bool Enabled, string? GitHubUsername, int RepoCount, IReadOnlyList<string> PinnedRepoNames);

    public record UpdateGitHubActivitySettingsRequest(bool Enabled, string? GitHubUsername, int? RepoCount, List<string>? PinnedRepoNames);

    /// <summary>#45's public resume-poster directory entry - name only, never an email address.</summary>
    public record ResumePosterDto(string Id, string DisplayName);

    public record ContactPosterRequest(string? Message, string? ReplyToEmail);
}
