using Bunit;
using BlazorApp.BlazorClient.Pages;
using Xunit;

namespace BlazorClient.Tests;

public class GeneralSectionTests : TestContext
{
    [Fact]
    public void RendersNothing_WhenSectionNameIsNull()
    {
        var cut = RenderComponent<GeneralSection>(p => p
            .Add(x => x.Items, new[] { "Item 1" }));

        Assert.Empty(cut.Markup.Trim());
    }

    [Fact]
    public void RendersNothing_WhenItemsIsNull()
    {
        var cut = RenderComponent<GeneralSection>(p => p
            .Add(x => x.SectionName, "Profile"));

        Assert.Empty(cut.Markup.Trim());
    }

    [Fact]
    public void RendersSectionHeader()
    {
        var cut = RenderComponent<GeneralSection>(p => p
            .Add(x => x.SectionName, "Profile")
            .Add(x => x.Items, new[] { "Detail oriented" }));

        cut.Find(".sectionheader").MarkupMatches("<div class=\"sectionheader\">Profile</div>");
    }

    [Fact]
    public void RendersAllItems()
    {
        var items = new[] { "Detail oriented", "Team player", "Problem solver" };

        var cut = RenderComponent<GeneralSection>(p => p
            .Add(x => x.SectionName, "Profile")
            .Add(x => x.Items, items));

        var listItems = cut.FindAll("li");
        Assert.Equal(3, listItems.Count);
        Assert.Equal("Detail oriented", listItems[0].TextContent);
        Assert.Equal("Team player", listItems[1].TextContent);
        Assert.Equal("Problem solver", listItems[2].TextContent);
    }

    [Fact]
    public void RendersEmptyList_WhenItemsIsEmpty()
    {
        var cut = RenderComponent<GeneralSection>(p => p
            .Add(x => x.SectionName, "Profile")
            .Add(x => x.Items, Array.Empty<string>()));

        Assert.Empty(cut.FindAll("li"));
        Assert.NotNull(cut.Find(".sectionheader"));
    }
}
