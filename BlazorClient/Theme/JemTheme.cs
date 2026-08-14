using MudBlazor;

namespace BlazorApp.BlazorClient.Theme;

/// <summary>
/// MudBlazor theme tuned to the existing 406JEM brand tokens (navy/blue palette,
/// Caviar Dreams headings) instead of Material's default Roboto/color-role look.
/// Mirrors the CSS custom properties in wwwroot/css/app.css.
/// </summary>
public static class JemTheme
{
    private const string Navy = "#1e2d5a";
    private const string Blue = "#245a8e";
    private const string Background = "#f4f6fb";
    private const string CardBackground = "#ffffff";
    private const string Border = "#d0d8ea";
    private const string TextPrimary = "#1a1a2e";
    private const string TextMuted = "#5a6070";

    private static readonly string[] BodyFontFamily = { "Helvetica Neue", "Helvetica", "Arial", "sans-serif" };
    private static readonly string[] HeadingFontFamily = { "caviar-bold", "Arial", "sans-serif" };

    public static readonly MudTheme Default = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = Navy,
            Secondary = Blue,
            AppbarBackground = Navy,
            AppbarText = "#ffffff",
            DrawerBackground = Navy,
            DrawerText = "#ffffff",
            Background = Background,
            Surface = CardBackground,
            TextPrimary = TextPrimary,
            TextSecondary = TextMuted,
            Divider = Border,
            Success = "#3f8f5f",
            Error = "#b3261e",
        },
        Typography = new Typography
        {
            Default = new Default { FontFamily = BodyFontFamily },
            H1 = new H1 { FontFamily = HeadingFontFamily, FontWeight = 700 },
            H2 = new H2 { FontFamily = HeadingFontFamily, FontWeight = 700 },
            H3 = new H3 { FontFamily = HeadingFontFamily, FontWeight = 700 },
            H4 = new H4 { FontFamily = HeadingFontFamily, FontWeight = 700 },
            H5 = new H5 { FontFamily = HeadingFontFamily, FontWeight = 700 },
            H6 = new H6 { FontFamily = HeadingFontFamily, FontWeight = 700 },
            Button = new Button { FontFamily = BodyFontFamily, FontWeight = 700, TextTransform = "none" },
        },
        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "8px",
            AppbarHeight = "64px",
        },
    };
}
