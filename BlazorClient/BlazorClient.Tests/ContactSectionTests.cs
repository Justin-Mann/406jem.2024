using BlazorApp.BlazorClient.Pages;
using BlazorApp.Models;
using BlazorClient.Tests.Helpers;
using Bunit;
using Xunit;

namespace BlazorClient.Tests;

public class ContactSectionTests : TestContext
{
    [Fact]
    public void RendersNothing_WhenContactInfoIsNull()
    {
        var cut = RenderComponent<ContactSection>();

        Assert.Empty(cut.Markup.Trim());
    }

    [Fact]
    public void RendersSectionHeader()
    {
        var cut = RenderComponent<ContactSection>(p => p
            .Add(x => x.ContactInfo, TestData.Resume.Contact));

        cut.Find(".sectionheader").MarkupMatches("<div class=\"sectionheader\">Contact</div>");
    }

    [Fact]
    public void RendersPhoneWithTelLink()
    {
        var contacts = new[]
        {
            new ContactItem { Type = ContactTypeEnum.Phone, DisplayValue = "555-1234" }
        };

        var cut = RenderComponent<ContactSection>(p => p
            .Add(x => x.ContactInfo, contacts));

        var link = cut.Find("a[href='tel:555-1234']");
        Assert.Equal("555-1234", link.TextContent);
    }

    [Fact]
    public void RendersEmailWithMailtoLink()
    {
        var contacts = new[]
        {
            new ContactItem { Type = ContactTypeEnum.Email, DisplayValue = "jane@example.com", MailTo = "jane@example.com" }
        };

        var cut = RenderComponent<ContactSection>(p => p
            .Add(x => x.ContactInfo, contacts));

        var link = cut.Find("a[href='mailto:jane@example.com']");
        Assert.Equal("jane@example.com", link.TextContent);
    }

    [Fact]
    public void RendersWebsiteWithExternalLink()
    {
        var contacts = new[]
        {
            new ContactItem { Type = ContactTypeEnum.Website, DisplayValue = "jane.dev", Url = "https://jane.dev" }
        };

        var cut = RenderComponent<ContactSection>(p => p
            .Add(x => x.ContactInfo, contacts));

        var link = cut.Find("a[href='https://jane.dev']");
        Assert.Equal("jane.dev", link.TextContent);
    }

    [Fact]
    public void RendersAllContactTypes()
    {
        var cut = RenderComponent<ContactSection>(p => p
            .Add(x => x.ContactInfo, TestData.Resume.Contact));

        var items = cut.FindAll("li");
        Assert.Equal(3, items.Count);
    }
}
