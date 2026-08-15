using System.Net;
using System.Text.Json;
using BlazorApp.BlazorClient.Pages.Admin;
using BlazorClient.Tests.Helpers;
using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BlazorClient.Tests;

public class ManageResumesPageTests : MudBunitTestContext
{
    private const string MineJson = """
        [
            {"id":"resume-1","ownerUserId":"admin","isFeatured":true,"payload":{"fName":"Jane","lName":"Doe","position":"Engineer"},"createdAtUtc":"2026-01-01T00:00:00Z","updatedAtUtc":"2026-01-01T00:00:00Z","status":"Published"},
            {"id":"resume-2","ownerUserId":"admin","isFeatured":false,"payload":null,"createdAtUtc":"2026-01-02T00:00:00Z","updatedAtUtc":"2026-01-02T00:00:00Z","status":"Draft","originalFileName":"resume.pdf"}
        ]
        """;

    private const string SavedDtoJson = """
        {"id":"resume-1","ownerUserId":"admin","isFeatured":true,"payload":{"fName":"Jane","lName":"Doe","position":"Engineer"},"createdAtUtc":"2026-01-01T00:00:00Z","updatedAtUtc":"2026-01-01T00:00:00Z","status":"Published"}
        """;

    private const string PublishedDraftJson = """
        {"id":"resume-2","ownerUserId":"admin","isFeatured":false,"payload":null,"createdAtUtc":"2026-01-02T00:00:00Z","updatedAtUtc":"2026-01-02T00:00:00Z","status":"Published","originalFileName":"resume.pdf"}
        """;

    private RoutedFakeHttpHandler RegisterHttpClient()
    {
        var handler = new RoutedFakeHttpHandler()
            .When(HttpMethod.Get, "resumes/mine", MineJson)
            .When(HttpMethod.Post, "resumes/resume-2/publish", PublishedDraftJson)
            .When(HttpMethod.Post, "resumes", SavedDtoJson, HttpStatusCode.Created)
            .When(HttpMethod.Put, "resumes/resume-1", SavedDtoJson)
            .When(HttpMethod.Delete, "resumes/resume-1", string.Empty, HttpStatusCode.NoContent);

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

        var cut = RenderComponent<ManageResumes>();

        Assert.Contains("Resume Admin access required", cut.Markup);
        Assert.DoesNotContain("resume-list", cut.Markup);
    }

    [Fact]
    public void Admin_SeesOwnResumes_WithStatusAndFeaturedBadges()
    {
        RegisterHttpClient();
        var authContext = this.AddTestAuthorization();
        authContext.SetAuthorized("admin");
        authContext.SetRoles("admin");

        var cut = RenderComponent<ManageResumes>();

        cut.WaitForAssertion(() => Assert.Contains("Jane Doe", cut.Markup));
        Assert.Contains("Draft", cut.Markup);
        Assert.Contains("Published", cut.Markup);
        Assert.Contains("Featured", cut.Markup);
    }

    [Fact]
    public void Admin_CreatesNewResume_PostsAndReloadsMine()
    {
        var handler = RegisterHttpClient();
        var authContext = this.AddTestAuthorization();
        authContext.SetAuthorized("admin");
        authContext.SetRoles("admin");

        var cut = RenderComponent<ManageResumes>();
        cut.WaitForAssertion(() => Assert.Contains("resume-new-btn", cut.Markup));

        cut.Find("button.resume-new-btn").Click();
        cut.WaitForAssertion(() => Assert.Contains("New Resume", cut.Markup));

        cut.Find("input[placeholder='First name']").Input("Jane");
        cut.Find("input[placeholder='Last name']").Input("Doe");
        cut.Find("input[placeholder='Position']").Input("Engineer");

        cut.Find("form.resume-edit-form").Submit();

        cut.WaitForAssertion(() =>
            Assert.Contains(handler.Requests, r => r.Method == HttpMethod.Post && r.RequestUri!.ToString().EndsWith("resumes")));
        Assert.Equal(2, handler.Requests.Count(r => r.Method == HttpMethod.Get && r.RequestUri!.ToString().Contains("resumes/mine")));
    }

    [Fact]
    public void Admin_SavingWithoutRequiredFields_ShowsValidationError_AndDoesNotPost()
    {
        var handler = RegisterHttpClient();
        var authContext = this.AddTestAuthorization();
        authContext.SetAuthorized("admin");
        authContext.SetRoles("admin");

        var cut = RenderComponent<ManageResumes>();
        cut.WaitForAssertion(() => Assert.Contains("resume-new-btn", cut.Markup));

        cut.Find("button.resume-new-btn").Click();
        cut.WaitForAssertion(() => Assert.Contains("New Resume", cut.Markup));

        cut.Find("form.resume-edit-form").Submit();

        cut.WaitForAssertion(() => Assert.Contains("resume-validation-error", cut.Markup));
        Assert.DoesNotContain(handler.Requests, r => r.Method == HttpMethod.Post && r.RequestUri!.ToString().EndsWith("resumes"));
    }

    [Fact]
    public void Admin_EditsExistingResume_PutsAndReloadsMine()
    {
        var handler = RegisterHttpClient();
        var authContext = this.AddTestAuthorization();
        authContext.SetAuthorized("admin");
        authContext.SetRoles("admin");

        var cut = RenderComponent<ManageResumes>();
        cut.WaitForAssertion(() => Assert.Contains("Jane Doe", cut.Markup));

        cut.FindAll("button.resume-edit-btn")[0].Click();
        cut.WaitForAssertion(() => Assert.Contains("Edit Resume", cut.Markup));

        cut.Find("form.resume-edit-form").Submit();

        cut.WaitForAssertion(() =>
            Assert.Contains(handler.Requests, r => r.Method == HttpMethod.Put && r.RequestUri!.ToString().Contains("resumes/resume-1")));
    }

    [Fact]
    public void Admin_PublishesDraftResume_PostsPublish()
    {
        var handler = RegisterHttpClient();
        var authContext = this.AddTestAuthorization();
        authContext.SetAuthorized("admin");
        authContext.SetRoles("admin");

        var cut = RenderComponent<ManageResumes>();
        cut.WaitForAssertion(() => Assert.Contains("resume-publish-btn", cut.Markup));

        cut.Find("button.resume-publish-btn").Click();

        cut.WaitForAssertion(() =>
            Assert.Contains(handler.Requests, r => r.Method == HttpMethod.Post && r.RequestUri!.ToString().Contains("resumes/resume-2/publish")));
    }

    [Fact]
    public void Admin_DeletesResume_DeletesAndReloadsMine()
    {
        var handler = RegisterHttpClient();
        var authContext = this.AddTestAuthorization();
        authContext.SetAuthorized("admin");
        authContext.SetRoles("admin");

        var cut = RenderComponent<ManageResumes>();
        cut.WaitForAssertion(() => Assert.Contains("Jane Doe", cut.Markup));

        cut.FindAll("button.resume-delete-btn")[0].Click();

        cut.WaitForAssertion(() =>
            Assert.Contains(handler.Requests, r => r.Method == HttpMethod.Delete && r.RequestUri!.ToString().Contains("resumes/resume-1")));
        Assert.Equal(2, handler.Requests.Count(r => r.Method == HttpMethod.Get && r.RequestUri!.ToString().Contains("resumes/mine")));
    }

    [Fact]
    public void Admin_AddsAndRemovesProfileBullet_InEditForm()
    {
        RegisterHttpClient();
        var authContext = this.AddTestAuthorization();
        authContext.SetAuthorized("admin");
        authContext.SetRoles("admin");

        var cut = RenderComponent<ManageResumes>();
        cut.WaitForAssertion(() => Assert.Contains("resume-new-btn", cut.Markup));
        cut.Find("button.resume-new-btn").Click();
        cut.WaitForAssertion(() => Assert.Contains("profile-add-btn", cut.Markup));

        cut.Find("button.profile-add-btn").Click();
        Assert.Single(cut.FindAll("button.profile-remove-btn"));

        cut.Find("button.profile-remove-btn").Click();
        Assert.Empty(cut.FindAll("button.profile-remove-btn"));
    }
}
