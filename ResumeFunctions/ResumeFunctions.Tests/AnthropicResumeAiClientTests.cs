using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using ResumeFunctions.Auth.Parsing;
using Xunit;

namespace ResumeFunctions.Tests;

public class AnthropicResumeAiClientTests
{
    [Fact]
    public async Task ExtractResumeJsonAsync_FailsGracefully_WhenApiKeyIsNotConfigured()
    {
        // No 'Anthropic:ApiKey' setting present — matches an environment where the app setting
        // hasn't been provisioned yet. Must fail closed rather than throw, per #30's acceptance
        // criteria that a failed parse never crashes the request.
        var configuration = new ConfigurationBuilder().Build();
        var client = new AnthropicResumeAiClient(configuration, Substitute.For<ILogger<AnthropicResumeAiClient>>());

        var result = await client.ExtractResumeJsonAsync("Jane Doe\nSoftware Engineer");

        Assert.False(result.Succeeded);
        Assert.Null(result.Json);
        Assert.False(string.IsNullOrWhiteSpace(result.ErrorMessage));
    }
}
