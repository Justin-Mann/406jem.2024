using BlazorApp.Models;

namespace BlazorClient.Tests.Helpers;

public static class TestData
{
    public static DigitalResumeModel Resume => new()
    {
        Id = 1,
        FName = "Jane",
        MName = "Q",
        LName = "Doe",
        Position = "Software Engineer",
        Subtitle = "C#, Azure",
        SimpleGoal = "Build great software.",
        LogoFile = "/img/logo.png",
        Profile = ["Detail oriented", "Team player", "Problem solver"],
        Contact =
        [
            new ContactItem { Type = ContactTypeEnum.Phone, DisplayValue = "555-1234" },
            new ContactItem { Type = ContactTypeEnum.Email, DisplayValue = "jane@example.com", MailTo = "jane@example.com" },
            new ContactItem { Type = ContactTypeEnum.Website, DisplayValue = "jane.dev", Url = "https://jane.dev" }
        ],
        Education =
        [
            new EducationItem
            {
                Name = "State University",
                Degree = true,
                DegreeName = "B.S. Computer Science",
                DegreeYear = "2015",
                AreasOfStudy = ["Algorithms", "Systems"]
            }
        ],
        WorkExperience =
        [
            new WorkExperienceItem
            {
                CompanyName = "Acme Corp",
                Position = "Senior Developer",
                StartDate = "2018",
                EndDate = "Present",
                BulletList = ["Built distributed systems", "Led code reviews"],
                Note = "Remote"
            },
            new WorkExperienceItem
            {
                CompanyName = "StartupCo",
                Position = "Junior Developer",
                StartDate = "2015",
                EndDate = "2018",
                BulletList = ["Developed features", "Fixed bugs"]
            }
        ],
        CustomSections =
        [
            new CustomSectionItem
            {
                Name = "Languages",
                CustomItems = [new CustomItem { Value = "C#", Type = CustomTypeEmun.Lang }]
            }
        ]
    };

    public static string ResumeJson =>
        System.Text.Json.JsonSerializer.Serialize(Resume);
}
