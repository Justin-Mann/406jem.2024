using System.Net;
using BlazorApp.BlazorClient.Pages;
using BlazorClient.Tests.Helpers;
using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BlazorClient.Tests;

public class ProjectsPageTests : TestContext
{
    private const string PublicJson = """
        {"title":"Projects","sections":[{"heading":"WWWSection","lastUpdated":"04/2025","links":[{"label":"GitHubLink","url":"https://github.com/x"}]}]}
        """;

    private const string MineJson = """
        [{"id":"listing-1","ownerUserId":"admin","isFeatured":true,"payload":{"title":"My List","sections":[{"heading":"Sec1","lastUpdated":"01/2026","links":[{"label":"Link1","url":"https://a.com"}]}]},"createdAtUtc":"2026-01-01T00:00:00Z","updatedAtUtc":"2026-01-01T00:00:00Z"}]
        """;

    private const string SavedDtoJson = """
        {"id":"listing-1","ownerUserId":"admin","isFeatured":true,"payload":{"title":"My List","sections":[{"heading":"Sec1","lastUpdated":"01/2026","links":[{"label":"Link1","url":"https://a.com"}]}]},"createdAtUtc":"2026-01-01T00:00:00Z","updatedAtUtc":"2026-01-01T00:00:00Z"}
        """;

    private RoutedFakeHttpHandler RegisterHttpClient()
    {
        var handler = new RoutedFakeHttpHandler()
            .When(HttpMethod.Get, "projectlistings/public", PublicJson)
            .When(HttpMethod.Get, "projectlistings/mine", MineJson)
            .When(HttpMethod.Post, "projectlistings", SavedDtoJson, HttpStatusCode.Created)
            .When(HttpMethod.Put, "projectlistings/listing-1", SavedDtoJson)
            .When(HttpMethod.Delete, "projectlistings/listing-1", string.Empty, HttpStatusCode.NoContent);

        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        Services.AddScoped(_ => client);
        return handler;
    }

    [Fact]
    public void RendersPublicListing_FromApiData()
    {
        RegisterHttpClient();
        this.AddTestAuthorization();

        var cut = RenderComponent<Projects_v2>();

        cut.WaitForAssertion(() => Assert.Contains("WWWSection", cut.Markup));
        Assert.Contains("GitHubLink", cut.Markup);
        Assert.Contains("Last Updated 04/2025", cut.Markup);
    }

    [Fact]
    public void NonAdmin_DoesNotSeeManagePanel()
    {
        RegisterHttpClient();
        var authContext = this.AddTestAuthorization();
        authContext.SetAuthorized("jane");
        authContext.SetRoles("visitor");

        var cut = RenderComponent<Projects_v2>();

        cut.WaitForAssertion(() => Assert.Contains("WWWSection", cut.Markup));
        Assert.DoesNotContain("Manage My Project Listings", cut.Markup);
    }

    [Fact]
    public void Admin_OpensManagePanel_LoadsOwnListings()
    {
        var handler = RegisterHttpClient();
        var authContext = this.AddTestAuthorization();
        authContext.SetAuthorized("admin");
        authContext.SetRoles("admin");

        var cut = RenderComponent<Projects_v2>();
        cut.WaitForAssertion(() => Assert.Contains("Manage My Project Listings", cut.Markup));

        cut.Find("button").Click();

        cut.WaitForAssertion(() => Assert.Contains("My List", cut.Markup));
        Assert.Contains(handler.Requests, r => r.Method == HttpMethod.Get && r.RequestUri!.ToString().Contains("projectlistings/mine"));
    }

    [Fact]
    public void Admin_CreatesNewListing_PostsAndReloadsMine()
    {
        var handler = RegisterHttpClient();
        var authContext = this.AddTestAuthorization();
        authContext.SetAuthorized("admin");
        authContext.SetRoles("admin");

        var cut = RenderComponent<Projects_v2>();
        cut.WaitForAssertion(() => Assert.Contains("Manage My Project Listings", cut.Markup));
        cut.Find("button").Click();
        cut.WaitForAssertion(() => Assert.Contains("+ New Listing", cut.Markup));

        cut.Find("button.btn-primary.btn-sm").Click();
        cut.WaitForAssertion(() => Assert.Contains("Show publicly", cut.Markup));

        cut.Find("form").Submit();

        cut.WaitForAssertion(() =>
            Assert.Contains(handler.Requests, r => r.Method == HttpMethod.Post && r.RequestUri!.ToString().Contains("projectlistings")));
        Assert.Equal(2, handler.Requests.Count(r => r.Method == HttpMethod.Get && r.RequestUri!.ToString().Contains("projectlistings/mine")));
    }

    [Fact]
    public void Admin_EditsExistingListing_PutsAndReloadsMine()
    {
        var handler = RegisterHttpClient();
        var authContext = this.AddTestAuthorization();
        authContext.SetAuthorized("admin");
        authContext.SetRoles("admin");

        var cut = RenderComponent<Projects_v2>();
        cut.WaitForAssertion(() => Assert.Contains("Manage My Project Listings", cut.Markup));
        cut.Find("button").Click();
        cut.WaitForAssertion(() => Assert.Contains("My List", cut.Markup));

        cut.Find("button.btn-outline-primary").Click();
        cut.WaitForAssertion(() => Assert.Contains("Show publicly", cut.Markup));

        cut.Find("form").Submit();

        cut.WaitForAssertion(() =>
            Assert.Contains(handler.Requests, r => r.Method == HttpMethod.Put && r.RequestUri!.ToString().Contains("projectlistings/listing-1")));
    }

    [Fact]
    public void Admin_DeletesListing_DeletesAndReloadsMine()
    {
        var handler = RegisterHttpClient();
        var authContext = this.AddTestAuthorization();
        authContext.SetAuthorized("admin");
        authContext.SetRoles("admin");

        var cut = RenderComponent<Projects_v2>();
        cut.WaitForAssertion(() => Assert.Contains("Manage My Project Listings", cut.Markup));
        cut.Find("button").Click();
        cut.WaitForAssertion(() => Assert.Contains("My List", cut.Markup));

        cut.Find("button.btn-outline-danger").Click();

        cut.WaitForAssertion(() =>
            Assert.Contains(handler.Requests, r => r.Method == HttpMethod.Delete && r.RequestUri!.ToString().Contains("projectlistings/listing-1")));
        Assert.Equal(2, handler.Requests.Count(r => r.Method == HttpMethod.Get && r.RequestUri!.ToString().Contains("projectlistings/mine")));
    }

    [Fact]
    public void Admin_ReordersSections_WhenMoveDownClicked()
    {
        RegisterHttpClient();
        var authContext = this.AddTestAuthorization();
        authContext.SetAuthorized("admin");
        authContext.SetRoles("admin");

        var cut = RenderComponent<Projects_v2>();
        cut.WaitForAssertion(() => Assert.Contains("Manage My Project Listings", cut.Markup));
        cut.Find("button").Click();
        cut.WaitForAssertion(() => Assert.Contains("+ New Listing", cut.Markup));
        cut.Find("button.btn-primary.btn-sm").Click();
        cut.WaitForAssertion(() => Assert.Contains("+ Add Section", cut.Markup));

        cut.Find("button.btn-outline-secondary.mb-3").Click();

        var headingInputs = cut.FindAll("input[placeholder='Section heading']");
        Assert.Equal(2, headingInputs.Count);

        headingInputs[0].Change("First");
        headingInputs = cut.FindAll("input[placeholder='Section heading']");
        headingInputs[1].Change("Second");

        var moveDownButtons = cut.FindAll("button").Where(b => b.TextContent.Contains("Section") && b.TextContent.Contains("↓")).ToList();
        moveDownButtons[0].Click();

        headingInputs = cut.FindAll("input[placeholder='Section heading']");
        Assert.Equal("Second", headingInputs[0].GetAttribute("value"));
        Assert.Equal("First", headingInputs[1].GetAttribute("value"));
    }
}
