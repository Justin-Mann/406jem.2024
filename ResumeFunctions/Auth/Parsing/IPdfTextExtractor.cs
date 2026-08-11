namespace ResumeFunctions.Auth.Parsing
{
    /// <summary>Extracts raw text from a PDF, for downstream LLM-based structured extraction
    /// (#30) — deliberately not a heuristic resume parser itself, per the issue's product
    /// decision to feed raw text to Claude rather than hand-parse resume structure.</summary>
    public interface IPdfTextExtractor
    {
        /// <param name="pdfStream">A seekable stream positioned at the start of the PDF.</param>
        string ExtractText(Stream pdfStream);
    }
}
