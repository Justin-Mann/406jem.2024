using BlazorApp.BlazorClient.Pages;
using BlazorApp.Models;
using BlazorClient.Tests.Helpers;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text.Json;
using Xunit;

namespace BlazorClient.Tests;

public class DigitalResumePageTests : MudBunitTestContext
{
    private void RegisterHttpClient(string json, HttpStatusCode status = HttpStatusCode.OK)
    {
        var handler = new FakeHttpHandler(json, status);
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        Services.AddScoped(_ => client);
    }

    [Fact]
    public void ShowsLoadingState_BeforeApiResponds()
    {
        var handler = new BlockingFakeHttpHandler();
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        Services.AddScoped(_ => client);

        var cut = RenderComponent<DigitalResume>();

        Assert.Contains("Give it a Second", cut.Markup);
        handler.Complete(TestData.ResumeJson);
    }

    [Fact]
    public void RendersResumeData_WhenApiReturnsNullId()
    {
        // Regression test: the API's DigitalResumeModel.Id is never populated (excluded from
        // AI-parsing output, unset on manually-created resumes) so the wire payload always has
        // "id": null. Id used to be declared as non-nullable `int` on this client's model,
        // which made System.Text.Json throw on the whole payload -- not just the Id field --
        // silently failing every resume load in production.
        RegisterHttpClient("""{"id":null,"fName":"Jane","lName":"Doe","position":"Software Engineer"}""");

        var cut = RenderComponent<DigitalResume>();

        cut.WaitForAssertion(() => Assert.Contains("Jane", cut.Markup));
        Assert.Contains("Software Engineer", cut.Markup);
    }

    [Fact]
    public void RendersResumeName_AfterApiLoad()
    {
        RegisterHttpClient(TestData.ResumeJson);

        var cut = RenderComponent<DigitalResume>();

        cut.WaitForAssertion(() => Assert.Contains("Jane", cut.Markup));
        Assert.Contains("Doe", cut.Markup);
    }

    [Fact]
    public void RendersPosition_AfterApiLoad()
    {
        RegisterHttpClient(TestData.ResumeJson);

        var cut = RenderComponent<DigitalResume>();

        cut.WaitForAssertion(() => Assert.Contains("Software Engineer", cut.Markup));
    }

    [Fact]
    public void RendersSimpleGoal_AfterApiLoad()
    {
        RegisterHttpClient(TestData.ResumeJson);

        var cut = RenderComponent<DigitalResume>();

        cut.WaitForAssertion(() => Assert.Contains("Build great software", cut.Markup));
    }

    [Fact]
    public void ShowsNoResumeRecordsFound_WhenApiErrors()
    {
        RegisterHttpClient("not json", HttpStatusCode.InternalServerError);

        var cut = RenderComponent<DigitalResume>();

        cut.WaitForAssertion(() => Assert.Contains("No Resume Records Found", cut.Markup));
        Assert.DoesNotContain("Give it a Second", cut.Markup);
    }

    [Fact]
    public void ShowsNoResumeRecordsFound_WhenApiReturnsNullBody()
    {
        RegisterHttpClient("null");

        var cut = RenderComponent<DigitalResume>();

        cut.WaitForAssertion(() => Assert.Contains("No Resume Records Found", cut.Markup));
    }

    [Fact]
    public void HidesLoadingMessage_AfterApiLoad()
    {
        RegisterHttpClient(TestData.ResumeJson);

        var cut = RenderComponent<DigitalResume>();

        cut.WaitForAssertion(() => Assert.DoesNotContain("Give it a Second", cut.Markup));
    }

    [Fact]
    public void RendersContactButton_WithMailtoHref_WhenEmailPresent()
    {
        RegisterHttpClient(TestData.ResumeJson);

        var cut = RenderComponent<DigitalResume>();

        cut.WaitForAssertion(() =>
        {
            var button = cut.Find("#contact-me-button");
            Assert.Equal("mailto:jane@example.com", button.GetAttribute("href"));
        });
    }

    [Fact]
    public void HidesContactButton_WhenNoEmailContactEntry()
    {
        var resumeWithoutEmail = new DigitalResumeModel
        {
            Id = TestData.Resume.Id,
            FName = TestData.Resume.FName,
            LName = TestData.Resume.LName,
            Position = TestData.Resume.Position,
            SimpleGoal = TestData.Resume.SimpleGoal,
            Contact = [new ContactItem { Type = ContactTypeEnum.Phone, DisplayValue = "555-1234" }]
        };
        RegisterHttpClient(JsonSerializer.Serialize(resumeWithoutEmail));

        var cut = RenderComponent<DigitalResume>();

        cut.WaitForAssertion(() => Assert.Contains("Jane", cut.Markup));
        Assert.Empty(cut.FindAll("#contact-me-button"));
    }
}
