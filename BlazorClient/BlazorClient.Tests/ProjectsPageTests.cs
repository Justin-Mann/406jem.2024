using System.Net;
using System.Text.Json;
using BlazorApp.BlazorClient.Pages;
using BlazorApp.BlazorClient.Services;
using BlazorClient.Tests.Helpers;
using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BlazorClient.Tests;

public class ProjectsPageTests : MudBunitTestContext
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
        RegisterGitHubActivityService();
        return handler;
    }

    /// <summary>Projects_v2 renders GitHubActivitySection unconditionally (#68), which needs its
    /// own GitHubActivityService in DI - unrelated to this page's own project-listing tests, so
    /// it's stubbed to the "disabled/unconfigured" (render-nothing) case here.</summary>
    private void RegisterGitHubActivityService()
    {
        var apiHttp = new HttpClient(new FakeHttpHandler("""{"enabled":false,"gitHubUsername":null,"repoCount":5,"pinnedRepoNames":[]}"""))
        {
            BaseAddress = new Uri("http://localhost"),
        };
        var gitHubHttp = new HttpClient(new FakeHttpHandler("[]")) { BaseAddress = new Uri("https://api.github.test/") };
        Services.AddScoped(_ => new GitHubActivityService(apiHttp, gitHubHttp));
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

        cut.Find("button.listing-new-btn").Click();
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

        cut.Find("button.listing-edit-btn").Click();
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

        cut.Find("button.listing-delete-btn").Click();

        cut.WaitForAssertion(() =>
            Assert.Contains(handler.Requests, r => r.Method == HttpMethod.Delete && r.RequestUri!.ToString().Contains("projectlistings/listing-1")));
        Assert.Equal(2, handler.Requests.Count(r => r.Method == HttpMethod.Get && r.RequestUri!.ToString().Contains("projectlistings/mine")));
    }

    [Fact]
    public async Task Admin_ReordersSections_WhenMoveDownClicked()
    {
        var handler = RegisterHttpClient();
        var authContext = this.AddTestAuthorization();
        authContext.SetAuthorized("admin");
        authContext.SetRoles("admin");

        var cut = RenderComponent<Projects_v2>();
        cut.WaitForAssertion(() => Assert.Contains("Manage My Project Listings", cut.Markup));
        cut.Find("button").Click();
        cut.WaitForAssertion(() => Assert.Contains("+ New Listing", cut.Markup));
        cut.Find("button.listing-new-btn").Click();
        cut.WaitForAssertion(() => Assert.Contains("+ Add Section", cut.Markup));

        cut.Find("button.section-add-btn").Click();

        var headingInputs = cut.FindAll("input[placeholder='Section heading']");
        Assert.Equal(2, headingInputs.Count);

        // bUnit's simulated oninput/onchange events don't reflect back into the
        // DOM's "value" attribute (Blazor skips re-applying it to avoid clobbering
        // user-typed input), so the reorder is verified via the submitted payload
        // rather than reading the inputs back after the swap.
        headingInputs[0].Input("First");
        headingInputs = cut.FindAll("input[placeholder='Section heading']");
        headingInputs[1].Input("Second");

        var moveDownButtons = cut.FindAll("button").Where(b => b.TextContent.Contains("Section") && b.TextContent.Contains("↓")).ToList();
        moveDownButtons[0].Click();

        cut.Find("form").Submit();

        cut.WaitForAssertion(() => Assert.Contains(handler.Requests, r => r.Method == HttpMethod.Post));
        var postRequest = handler.Requests.First(r => r.Method == HttpMethod.Post);
        var body = JsonDocument.Parse(await postRequest.Content!.ReadAsStringAsync());
        var headings = body.RootElement.GetProperty("payload").GetProperty("sections")
            .EnumerateArray().Select(s => s.GetProperty("heading").GetString()).ToList();

        Assert.Equal(new[] { "Second", "First" }, headings);
    }
}
