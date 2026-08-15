using BlazorApp.BlazorClient.Layout;
using BlazorApp.BlazorClient.Services;
using BlazorClient.Tests.Helpers;
using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BlazorClient.Tests;

public class MainLayoutTests : MudBunitTestContext
{
    private void RegisterHttpClient()
    {
        var handler = new FakeHttpHandler("{}");
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        Services.AddScoped(_ => client);
        Services.AddScoped<JwtAuthenticationStateProvider>();
        Services.AddScoped(sp => new AuthenticationService(client, sp.GetRequiredService<JwtAuthenticationStateProvider>()));
    }

    [Fact]
    public void Visitor_DoesNotSeeAdminNavGroup()
    {
        RegisterHttpClient();
        var authContext = this.AddTestAuthorization();
        authContext.SetAuthorized("jane");
        authContext.SetRoles("visitor");

        var cut = RenderComponent<MainLayout>();

        Assert.DoesNotContain("Manage Resumes", cut.Markup);
        Assert.DoesNotContain("Manage Project Listings", cut.Markup);
    }

    [Fact]
    public void ResumeAdmin_SeesManageResumesButNotProjectListings()
    {
        RegisterHttpClient();
        var authContext = this.AddTestAuthorization();
        authContext.SetAuthorized("admin");
        authContext.SetRoles("admin");

        var cut = RenderComponent<MainLayout>();

        Assert.Contains("Manage Resumes", cut.Markup);
        Assert.DoesNotContain("Manage Project Listings", cut.Markup);
    }

    [Fact]
    public void SuperAdmin_SeesManageResumesAndProjectListings()
    {
        RegisterHttpClient();
        var authContext = this.AddTestAuthorization();
        authContext.SetAuthorized("root");
        authContext.SetRoles("superadmin");

        var cut = RenderComponent<MainLayout>();

        Assert.Contains("Manage Resumes", cut.Markup);
        Assert.Contains("Manage Project Listings", cut.Markup);
    }
}
