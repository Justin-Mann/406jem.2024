using BlazorApp.BlazorClient.Pages;
using BlazorClient.Tests.Helpers;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using Xunit;

namespace BlazorClient.Tests;

public class DigitalResumePageTests : TestContext
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
    public void ShowsLoadingMessage_WhenApiErrors()
    {
        // API errors are caught; loadingMessageShow stays true since the catch block
        // does not clear it, and Id (int) is never null so the else branch renders
        RegisterHttpClient("not json", HttpStatusCode.InternalServerError);

        var cut = RenderComponent<DigitalResume>();

        cut.WaitForAssertion(() => Assert.Contains("Give it a Second", cut.Markup));
    }

    [Fact]
    public void HidesLoadingMessage_AfterApiLoad()
    {
        RegisterHttpClient(TestData.ResumeJson);

        var cut = RenderComponent<DigitalResume>();

        cut.WaitForAssertion(() => Assert.DoesNotContain("Give it a Second", cut.Markup));
    }
}
