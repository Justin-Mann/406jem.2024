using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections.Generic;

namespace BlazorApp.Models
{
    public class DigitalResumeModel
    {
        public string? Id { get; set; }
        public string? FName { get; set; }
        public string? MName { get; set; }
        public string? LName { get; set; }
        public string? Position { get; set; }
        public string? Subtitle { get; set; }
        public string? SimpleGoal { get; set; }
        public string? LogoFile { get; set; }

        public IEnumerable<WorkExperienceItem>? WorkExperience { get; set; }

        public IEnumerable<string>? Profile { get; set; }

        public IEnumerable<ContactItem>? Contact { get; set; }

        public IEnumerable<EducationItem>? Education { get; set; }

        public IEnumerable<CustomSectionItem>? CustomSections { get; set; }

        public IEnumerable<SkillAssessmentItem>? SkillAssessments { get; set; }
    }

    public class SkillAssessmentItem
    { 
        public string? AssessorName { get; set; }
        public IEnumerable<SkillItem>? Skills { get; set; }
    }

    public class SkillItem { 
        public string? Name { get; set; }
        public int? Value { get; set; }
    }

    public class CustomSectionItem
    {
        public string? Name { get; set; }
        public IEnumerable<CustomItem>? CustomItems { get; set; }
    }

    public class CustomItem { 
        public string? Value { get; set; }
        public CustomTypeEmun? Type { get; set; }
    }

    [JsonConverter(typeof(StringEnumConverter))]
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
        public IEnumerable<string>? BulletList { get; set; }
        public string? Note { get; set; }
    }

    public class EducationItem
    {
        public string? Name { get; set; }
        public bool Degree { get; set; }
        public string? DegreeName { get; set; }
        public string? DegreeYear { get; set; }
        public IEnumerable<string>? AreasOfStudy { get; set; }
    }

    public class ContactItem
    {
        public ContactTypeEnum? Type { get; set; }
        public string? DisplayValue { get; set; }
        public string? Url { get; set; }
        public string? MailTo { get; set; }
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum ContactTypeEnum { 
        Phone, Website, Email
    }
}
