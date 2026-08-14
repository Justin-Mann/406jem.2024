using Bunit;
using MudBlazor.Services;

namespace BlazorClient.Tests.Helpers;

/// <summary>
/// bUnit TestContext base for any component under test that renders MudBlazor
/// components — MudBlazor's input/interop components resolve services (popover,
/// key interceptor, resize observer, etc.) from DI, which AddMudServices() registers.
/// </summary>
public abstract class MudBunitTestContext : TestContext
{
    protected MudBunitTestContext()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }
}
