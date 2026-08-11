using ResumeFunctions.Auth.Parsing;

namespace ResumeFunctions.Tests.Helpers;

/// <summary>In-memory IResumeAiClient so ResumeParsingApi tests can exercise the
/// success/malformed-response/failure paths without calling the real Anthropic API.</summary>
public class FakeResumeAiClient : IResumeAiClient
{
    private readonly ResumeAiExtractionResult _result;

    public string? LastResumeTextReceived { get; private set; }

    public FakeResumeAiClient(ResumeAiExtractionResult result)
    {
        _result = result;
    }

    public static FakeResumeAiClient ReturningJson(string json) => new(ResumeAiExtractionResult.Successful(json));

    public static FakeResumeAiClient Failing(string message) => new(ResumeAiExtractionResult.Failed(message));

    public Task<ResumeAiExtractionResult> ExtractResumeJsonAsync(string resumeText, CancellationToken cancellationToken = default)
    {
        LastResumeTextReceived = resumeText;
        return Task.FromResult(_result);
    }
}
