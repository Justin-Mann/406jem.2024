using System.Text;
using UglyToad.PdfPig;

namespace ResumeFunctions.Auth.Parsing
{
    /// <summary>
    /// PdfPig-based text extraction (#30) — pure managed code, no native dependencies, so it
    /// works unmodified on the Linux Consumption-plan Functions host. Extracts raw page text
    /// only; no layout/structure inference — the LLM downstream handles structure.
    /// </summary>
    public class PdfPigTextExtractor : IPdfTextExtractor
    {
        public string ExtractText(Stream pdfStream)
        {
            using var document = PdfDocument.Open(pdfStream);
            var builder = new StringBuilder();
            foreach (var page in document.GetPages())
            {
                builder.AppendLine(page.Text);
            }
            return builder.ToString();
        }
    }
}
