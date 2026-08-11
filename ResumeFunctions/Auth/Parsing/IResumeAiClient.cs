namespace ResumeFunctions.Auth.Parsing
{
    public record ResumeAiExtractionResult(bool Succeeded, string? Json, string? ErrorMessage)
    {
        public static ResumeAiExtractionResult Failed(string message) => new(false, null, message);

        public static ResumeAiExtractionResult Successful(string json) => new(true, json, null);
    }

    /// <summary>
    /// Seam around the Anthropic Messages API call (#30) — kept separate from
    /// ResumeParsingApi's JSON-to-DigitalResumeModel mapping so tests can substitute a fake here
    /// and exercise the mapping/fallback logic without hitting the real API, per the issue's
    /// "mock the Anthropic call" test requirement.
    /// </summary>
    public interface IResumeAiClient
    {
        /// <returns>On success, the raw JSON text Claude returned (expected to match the
        /// DigitalResumeModel shape). On failure — missing API key, network/API error, refusal,
        /// or an empty response — Succeeded is false and ErrorMessage explains why.</returns>
        Task<ResumeAiExtractionResult> ExtractResumeJsonAsync(string resumeText, CancellationToken cancellationToken = default);
    }
}
