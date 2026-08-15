namespace BlazorApp.Models
{
    /// <summary>Mirrors ResumeFunctions.Auth.Dtos.ResumeDto - one of a Resume Admin's own
    /// resumes (#31), Draft (uploaded/unparsed) or Published.</summary>
    public class ResumeDto
    {
        public string Id { get; set; } = string.Empty;
        public string OwnerUserId { get; set; } = string.Empty;
        public bool IsFeatured { get; set; }
        public DigitalResumeModel? Payload { get; set; }
        public DateTimeOffset CreatedAtUtc { get; set; }
        public DateTimeOffset UpdatedAtUtc { get; set; }
        public string Status { get; set; } = "Published";
        public string? OriginalFileName { get; set; }
        public string? ContentType { get; set; }
        public long? FileSizeBytes { get; set; }

        public bool IsDraft => string.Equals(Status, "Draft", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>OwnerUserId is only honored when the caller is a SuperAdmin creating/editing on
    /// behalf of another owner - a plain ResumeAdmin is always pinned to their own username.</summary>
    public class CreateOrUpdateResumeRequest
    {
        public string? OwnerUserId { get; set; }
        public bool IsFeatured { get; set; }
        public DigitalResumeModel? Payload { get; set; }
    }

    /// <summary>Result of POST /resumes/{id}/parse. ParseSucceeded false (with the resume left
    /// Draft/unchanged) is a normal outcome the UI should fall back to manual entry for, not an
    /// error.</summary>
    public class ParseResumeResponse
    {
        public ResumeDto Resume { get; set; } = new();
        public bool ParseSucceeded { get; set; }
        public string? Message { get; set; }
    }
}
