using System.Collections.Generic;

namespace ResumeFunctions.Models
{
    /// <summary>
    /// Structured replacement for the hardcoded links in Projects.v2.razor / the Angular
    /// projects component. There was no prior model to reuse — this shape mirrors those pages'
    /// existing grouping (named sections, each with a "last updated" stamp and a list of links).
    /// </summary>
    public class ProjectListingModel
    {
        public string? Title { get; set; }

        public IEnumerable<ProjectSection>? Sections { get; set; }
    }

    public class ProjectSection
    {
        public string? Heading { get; set; }
        public string? LastUpdated { get; set; }
        public IEnumerable<ProjectLink>? Links { get; set; }
    }

    public class ProjectLink
    {
        public string? Label { get; set; }
        public string? Url { get; set; }
        public string? Description { get; set; }
    }
}
