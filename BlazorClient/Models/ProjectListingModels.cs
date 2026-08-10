namespace BlazorApp.Models
{
    public class ProjectListingModel
    {
        public string? Title { get; set; }

        public List<ProjectSection> Sections { get; set; } = new();
    }

    public class ProjectSection
    {
        public string? Heading { get; set; }
        public string? LastUpdated { get; set; }
        public List<ProjectLink> Links { get; set; } = new();
    }

    public class ProjectLink
    {
        public string? Label { get; set; }
        public string? Url { get; set; }
        public string? Description { get; set; }
    }

    public class ProjectListingDto
    {
        public string Id { get; set; } = string.Empty;
        public string OwnerUserId { get; set; } = string.Empty;
        public bool IsFeatured { get; set; }
        public ProjectListingModel? Payload { get; set; }
        public DateTimeOffset CreatedAtUtc { get; set; }
        public DateTimeOffset UpdatedAtUtc { get; set; }
    }

    public class CreateOrUpdateProjectListingRequest
    {
        public string? OwnerUserId { get; set; }
        public bool IsFeatured { get; set; }
        public ProjectListingModel? Payload { get; set; }
    }
}
