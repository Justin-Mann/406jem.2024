using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using BlazorApp.BlazorClient;
using BlazorApp.BlazorClient.Services;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped<SessionCookieHandler>();
builder.Services.AddScoped(sp =>
{
    var handler = sp.GetRequiredService<SessionCookieHandler>();
    handler.InnerHandler = new HttpClientHandler();
    return new HttpClient(handler) { BaseAddress = new Uri(builder.Configuration["API_Prefix"] ?? "https://api.406jem.com") };
});

builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<JwtAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<JwtAuthenticationStateProvider>());
builder.Services.AddScoped<AuthenticationService>();

builder.Services.AddMudServices();

var host = builder.Build();

await host.Services.GetRequiredService<AuthenticationService>().InitializeAsync();

await host.RunAsync();



