using BlazorApp.BlazorClient.Pages;
using BlazorApp.Models;
using BlazorClient.Tests.Helpers;
using Bunit;
using Xunit;

namespace BlazorClient.Tests;

public class WorkExperienceSectionTests : TestContext
{
    [Fact]
    public void RendersNothing_WhenWorkExperienceIsNull()
    {
        var cut = RenderComponent<WorkExperienceSection>();

        Assert.Empty(cut.Markup.Trim());
    }

    [Fact]
    public void RendersSectionHeader_WhenDataProvided()
    {
        var cut = RenderComponent<WorkExperienceSection>(p => p
            .Add(x => x.WorkExperienceInfo, TestData.Resume.WorkExperience));

        cut.Find(".sectionheader").MarkupMatches("<div class=\"sectionheader\">Experience (XP)</div>");
    }

    [Fact]
    public void RendersAllWorkItems()
    {
        var cut = RenderComponent<WorkExperienceSection>(p => p
            .Add(x => x.WorkExperienceInfo, TestData.Resume.WorkExperience));

        var items = cut.FindAll(".workXpItem");
        Assert.Equal(2, items.Count);
    }

    [Fact]
    public void RendersCompanyNameAndPosition()
    {
        var xp = new[]
        {
            new WorkExperienceItem
            {
                CompanyName = "Acme Corp",
                Position = "Senior Developer",
                StartDate = "2018",
                EndDate = "Present"
            }
        };

        var cut = RenderComponent<WorkExperienceSection>(p => p
            .Add(x => x.WorkExperienceInfo, xp));

        Assert.Contains("Acme Corp", cut.Find(".companyName").TextContent);
        Assert.Contains("Senior Developer", cut.Find(".position").TextContent);
        Assert.Contains("2018 - Present", cut.Find(".timeFrame").TextContent);
    }

    [Fact]
    public void RendersBulletList_WhenPresent()
    {
        var xp = new[]
        {
            new WorkExperienceItem
            {
                CompanyName = "Acme",
                Position = "Dev",
                StartDate = "2020",
                BulletList = ["Built systems", "Led reviews"]
            }
        };

        var cut = RenderComponent<WorkExperienceSection>(p => p
            .Add(x => x.WorkExperienceInfo, xp));

        var bullets = cut.FindAll(".jobXpList li");
        Assert.Equal(2, bullets.Count);
        Assert.Equal("Built systems", bullets[0].TextContent);
    }

    [Fact]
    public void RendersNote_WhenPresent()
    {
        var xp = new[]
        {
            new WorkExperienceItem
            {
                CompanyName = "Acme",
                Position = "Dev",
                StartDate = "2020",
                Note = "Remote position"
            }
        };

        var cut = RenderComponent<WorkExperienceSection>(p => p
            .Add(x => x.WorkExperienceInfo, xp));

        Assert.Contains("Remote position", cut.Find(".note").TextContent);
    }

    [Fact]
    public void DoesNotRenderNote_WhenNoteIsEmpty()
    {
        var xp = new[]
        {
            new WorkExperienceItem { CompanyName = "Acme", Position = "Dev", StartDate = "2020" }
        };

        var cut = RenderComponent<WorkExperienceSection>(p => p
            .Add(x => x.WorkExperienceInfo, xp));

        Assert.Empty(cut.FindAll(".note"));
    }
}
