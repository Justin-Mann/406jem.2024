using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ResumeFunctions.Auth.Parsing;

var apiKey = Environment.GetEnvironmentVariable("DIAG_ANTHROPIC_KEY");
if (string.IsNullOrWhiteSpace(apiKey))
{
    Console.WriteLine("DIAG: no API key in env var.");
    return;
}

var pdfPath = args.Length > 0 ? args[0] : "jmResume.8.2025.pdf";
using var pdfStream = File.OpenRead(pdfPath);

using var loggerFactory = LoggerFactory.Create(b => b.AddConsole().SetMinimumLevel(LogLevel.Debug));

var extractor = new PdfPigTextExtractor();
var text = extractor.ExtractText(pdfStream);
Console.WriteLine($"DIAG: extracted {text.Length} characters of text from PDF.");

var config = new ConfigurationBuilder()
    .AddInMemoryCollection(new Dictionary<string, string?> { ["Anthropic:ApiKey"] = apiKey })
    .Build();

var client = new AnthropicResumeAiClient(config, loggerFactory.CreateLogger<AnthropicResumeAiClient>());
var result = await client.ExtractResumeJsonAsync(text);

Console.WriteLine("DIAG: Succeeded = " + result.Succeeded);
Console.WriteLine("DIAG: ErrorMessage = " + result.ErrorMessage);
Console.WriteLine("DIAG: Json length = " + (result.Json?.Length ?? -1));
if (!string.IsNullOrEmpty(result.Json))
{
    Console.WriteLine("DIAG: Json (first 500 chars) = " + result.Json[..Math.Min(500, result.Json.Length)]);
}
