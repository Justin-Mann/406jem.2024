using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BlazorApp.Models
{
    public class DigitalResumeModel
    {
        public string? Id { get; set; }

        [Required(ErrorMessage = "First name is required.")]
        public string? FName { get; set; }

        public string? MName { get; set; }

        [Required(ErrorMessage = "Last name is required.")]
        public string? LName { get; set; }

        [Required(ErrorMessage = "Position is required.")]
        public string? Position { get; set; }

        public string? Subtitle { get; set; }
        public string? SimpleGoal { get; set; }
        public string? LogoFile { get; set; }

        // List<T> (not IEnumerable<T>) so the admin editor (#31) can index into and mutate these
        // in place - collection-expression initializers (`[...]`) used elsewhere still work fine.
        public List<WorkExperienceItem> WorkExperience { get; set; } = new();

        public List<string> Profile { get; set; } = new();

        public List<ContactItem> Contact { get; set; } = new();

        public List<EducationItem> Education { get; set; } = new();

        public List<CustomSectionItem> CustomSections { get; set; } = new();

        public List<SkillAssessmentItem> SkillAssessments { get; set; } = new();
    }

    public class SkillAssessmentItem
    {
        public string? AssessorName { get; set; }
        public List<SkillItem> Skills { get; set; } = new();
    }

    public class SkillItem {
        public string? Name { get; set; }
        public int? Value { get; set; }
    }

    public class CustomSectionItem
    {
        public string? Name { get; set; }
        public List<CustomItem> CustomItems { get; set; } = new();
    }

    public class CustomItem {
        public string? Value { get; set; }
        public CustomTypeEmun? Type { get; set; }
    }

    // No JsonStringEnumConverter - the wire format (ResumeFunctions' WorkerOptions.Serializer,
    // see CLAUDE.md's "Wire serialization" note) sends/expects the plain integer ordinal, not a
    // string name. A converter here would make the admin editor's Create/Update payloads
    // (#31) send e.g. "Lang" where the server expects 0, failing server-side deserialization.
    public enum CustomTypeEmun
    {
        Lang, Win, Comp, CompNetwork, Cloud, RDB, DDB, DataLang
    }

    public class WorkExperienceItem
    {
        public string? CompanyName { get; set; }
        public string? Position { get; set; }
        public string? StartDate { get; set; }
        public string? EndDate { get; set; }
        public List<string> BulletList { get; set; } = new();
        public string? Note { get; set; }
    }

    public class EducationItem
    {
        public string? Name { get; set; }
        public bool Degree { get; set; }
        public string? DegreeName { get; set; }
        public string? DegreeYear { get; set; }
        public List<string> AreasOfStudy { get; set; } = new();
    }

    public class ContactItem
    {
        public ContactTypeEnum? Type { get; set; }
        public string? DisplayValue { get; set; }
        public string? Url { get; set; }
        public string? MailTo { get; set; }
    }

    // See CustomTypeEmun's comment above - no JsonStringEnumConverter, same reason.
    public enum ContactTypeEnum {
        Phone, Website, Email
    }
}
