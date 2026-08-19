using System.Net;
using BlazorApp.BlazorClient.Pages.Admin;
using BlazorClient.Tests.Helpers;
using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BlazorClient.Tests;

public class GitHubActivitySettingsPageTests : MudBunitTestContext
{
    private const string MineJson = """
        {"enabled":true,"gitHubUsername":"justin-mann","repoCount":5,"pinnedRepoNames":["406jem.2026"]}
        """;

    private const string SavedJson = """
        {"enabled":false,"gitHubUsername":"justin-mann","repoCount":8,"pinnedRepoNames":["406jem.2026","another-repo"]}
        """;

    private RoutedFakeHttpHandler RegisterHttpClient(HttpStatusCode putStatusCode = HttpStatusCode.OK)
    {
        var handler = new RoutedFakeHttpHandler()
            .When(HttpMethod.Get, "github-activity-settings/mine", MineJson)
            .When(HttpMethod.Put, "github-activity-settings/mine", SavedJson, putStatusCode);

        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        Services.AddScoped(_ => client);
        return handler;
    }

    [Fact]
    public void NonAdmin_SeesAccessRequiredMessage()
    {
        RegisterHttpClient();
        var authContext = this.AddTestAuthorization();
        authContext.SetAuthorized("jane");
        authContext.SetRoles("visitor");

        var cut = RenderComponent<GitHubActivitySettings>();

        Assert.Contains("Resume Admin access required", cut.Markup);
        Assert.DoesNotContain("github-activity-save-btn", cut.Markup);
    }

    [Fact]
    public void Admin_SeesLoadedSettings_WithPinnedRepo()
    {
        RegisterHttpClient();
        var authContext = this.AddTestAuthorization();
        authContext.SetAuthorized("admin");
        authContext.SetRoles("admin");

        var cut = RenderComponent<GitHubActivitySettings>();

        cut.WaitForAssertion(() => Assert.Contains("406jem.2026", cut.Markup));
        Assert.Contains("github-activity-save-btn", cut.Markup);
    }

    [Fact]
    public void Admin_AddsAndRemovesPinnedRepo()
    {
        RegisterHttpClient();
        var authContext = this.AddTestAuthorization();
        authContext.SetAuthorized("admin");
        authContext.SetRoles("admin");

        var cut = RenderComponent<GitHubActivitySettings>();
        cut.WaitForAssertion(() => Assert.Contains("github-activity-pinned-add-btn", cut.Markup));

        cut.Find("input[placeholder='Repo name']").Input("new-repo");
        cut.Find("button.github-activity-pinned-add-btn").Click();

        Assert.Equal(2, cut.FindAll("div.github-activity-pinned-chip").Count);

        cut.FindAll(".github-activity-pinned-chip .mud-chip-close-button")[0].Click();

        Assert.Single(cut.FindAll("div.github-activity-pinned-chip"));
    }

    [Fact]
    public void Admin_SavesSettings_PutsAndShowsSuccess()
    {
        var handler = RegisterHttpClient();
        var authContext = this.AddTestAuthorization();
        authContext.SetAuthorized("admin");
        authContext.SetRoles("admin");

        var cut = RenderComponent<GitHubActivitySettings>();
        cut.WaitForAssertion(() => Assert.Contains("github-activity-save-btn", cut.Markup));

        cut.Find("button.github-activity-save-btn").Click();

        cut.WaitForAssertion(() =>
            Assert.Contains(handler.Requests, r => r.Method == HttpMethod.Put && r.RequestUri!.ToString().Contains("github-activity-settings/mine")));
        cut.WaitForAssertion(() => Assert.Contains("GitHub Activity settings saved.", cut.Markup));
    }

    [Fact]
    public void Admin_SaveFailure_ShowsErrorMessage()
    {
        RegisterHttpClient(HttpStatusCode.BadRequest);
        var authContext = this.AddTestAuthorization();
        authContext.SetAuthorized("admin");
        authContext.SetRoles("admin");

        var cut = RenderComponent<GitHubActivitySettings>();
        cut.WaitForAssertion(() => Assert.Contains("github-activity-save-btn", cut.Markup));

        cut.Find("button.github-activity-save-btn").Click();

        cut.WaitForAssertion(() => Assert.Contains("Could not save your GitHub Activity settings", cut.Markup));
    }
}
