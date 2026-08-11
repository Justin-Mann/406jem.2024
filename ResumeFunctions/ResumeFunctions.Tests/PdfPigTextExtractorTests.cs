using ResumeFunctions.Auth.Parsing;
using Xunit;

namespace ResumeFunctions.Tests;

public class PdfPigTextExtractorTests
{
    private static readonly string FixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "sample-resume.pdf");

    [Fact]
    public void ExtractText_ReturnsNonEmptyText_ForRealResumePdf()
    {
        var extractor = new PdfPigTextExtractor();

        using var stream = File.OpenRead(FixturePath);
        var text = extractor.ExtractText(stream);

        Assert.False(string.IsNullOrWhiteSpace(text));
        // The fixture is Justin Mann's own resume PDF (per #30's acceptance criteria) — a loose
        // sanity check that we actually got real page content back, not just extraction not
        // crashing.
        Assert.Contains("Justin", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Mann", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ExtractText_Throws_ForNonPdfContent()
    {
        var extractor = new PdfPigTextExtractor();
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("not a pdf at all"));

        Assert.ThrowsAny<Exception>(() => extractor.ExtractText(stream));
    }
}
