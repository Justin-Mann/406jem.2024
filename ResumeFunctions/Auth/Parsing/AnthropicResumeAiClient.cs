using System.Text.Json;
using Anthropic;
using Anthropic.Models.Messages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ResumeFunctions.Auth.Parsing
{
    /// <summary>
    /// Calls the Anthropic Messages API (#30) with structured output constrained to a
    /// DigitalResumeModel-shaped schema, using Claude Haiku 4.5 — per the issue, this is a
    /// low-frequency, well-defined extraction task that doesn't need the largest available
    /// model. Reads the API key from the 'Anthropic:ApiKey' app setting (Function App
    /// setting/Key Vault reference in Azure, local.settings.json locally) — never committed,
    /// never re-provisioned here.
    /// </summary>
    public class AnthropicResumeAiClient : IResumeAiClient
    {
        private const string ModelId = "claude-haiku-4-5";
        private const int MaxOutputTokens = 8000;

        // Guards against a pathological upload (e.g. an accidentally-huge PDF) blowing past
        // Haiku's context window or running up cost — not an expected code path for a resume.
        private const int MaxInputCharacters = 60_000;

        private const string SystemPrompt = """
            You extract structured resume data from raw text that was extracted from a PDF resume.
            Return only information that is actually present in the text -- never invent names,
            dates, employers, or skills. Use null for anything you can't find. Preserve the
            wording used in the source text rather than paraphrasing it.

            For each contact entry, pick the "type" that matches how the value is used: "Email"
            for an email address (populate mailTo with the address), "Website" for a URL or
            profile link (populate url), "Phone" for a phone number (populate displayValue).

            For custom-section items such as programming languages, tools, or platforms, choose
            the closest matching "type" from Lang, Win, Comp, CompNetwork, Cloud, RDB, DDB,
            DataLang -- or null if none of them fit. Group related items (e.g. all programming
            languages) under one custom section.
            """;

        private static readonly Dictionary<string, JsonElement> ResponseSchema = BuildSchema();

        private readonly string? _apiKey;
        private readonly ILogger<AnthropicResumeAiClient> _logger;

        public AnthropicResumeAiClient(IConfiguration configuration, ILogger<AnthropicResumeAiClient> logger)
        {
            _apiKey = configuration["Anthropic:ApiKey"];
            _logger = logger;
        }

        public async Task<ResumeAiExtractionResult> ExtractResumeJsonAsync(string resumeText, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                _logger.LogError("The 'Anthropic:ApiKey' app setting is not configured -- cannot parse resumes.");
                return ResumeAiExtractionResult.Failed("AI extraction is not configured on the server.");
            }

            var truncatedText = resumeText.Length > MaxInputCharacters
                ? resumeText[..MaxInputCharacters]
                : resumeText;

            try
            {
                AnthropicClient client = new() { ApiKey = _apiKey };

                var response = await client.Messages.Create(new MessageCreateParams
                {
                    Model = ModelId,
                    MaxTokens = MaxOutputTokens,
                    System = SystemPrompt,
                    OutputConfig = new OutputConfig
                    {
                        Format = new JsonOutputFormat { Schema = ResponseSchema },
                    },
                    Messages = [new() { Role = Role.User, Content = truncatedText }],
                });

                if (response.StopReason == "refusal")
                {
                    _logger.LogWarning("Anthropic declined to extract this resume.");
                    return ResumeAiExtractionResult.Failed("The AI declined to process this resume.");
                }

                var text = response.Content
                    .Select(block => block.Value)
                    .OfType<TextBlock>()
                    .FirstOrDefault()?.Text;

                return string.IsNullOrWhiteSpace(text)
                    ? ResumeAiExtractionResult.Failed("The AI returned an empty response.")
                    : ResumeAiExtractionResult.Successful(text);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Anthropic resume extraction call failed.");
                return ResumeAiExtractionResult.Failed("AI extraction failed. Try again later.");
            }
        }

        private static Dictionary<string, JsonElement> BuildSchema()
        {
            using var document = JsonDocument.Parse(SchemaJson);
            return document.RootElement.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.Clone());
        }

        // Mirrors (a subset of) DigitalResumeModel.cs. Every property is listed in its object's
        // "required" array with a nullable type, rather than omitted, per structured-output
        // requirements -- "optional" is expressed as `null`, not absence. Enum values use the
        // C# enum member names (ContactTypeEnum / CustomTypeEmun) so deserializing the response
        // with JsonStringEnumConverter maps directly onto DigitalResumeModel without a manual
        // translation step. LogoFile and Id are intentionally excluded -- not extractable from
        // resume text.
        private const string SchemaJson = """
            {
              "type": "object",
              "properties": {
                "fName": { "type": ["string", "null"] },
                "mName": { "type": ["string", "null"] },
                "lName": { "type": ["string", "null"] },
                "position": { "type": ["string", "null"] },
                "subtitle": { "type": ["string", "null"] },
                "simpleGoal": { "type": ["string", "null"] },
                "profile": {
                  "type": ["array", "null"],
                  "items": { "type": "string" }
                },
                "workExperience": {
                  "type": ["array", "null"],
                  "items": {
                    "type": "object",
                    "properties": {
                      "companyName": { "type": ["string", "null"] },
                      "position": { "type": ["string", "null"] },
                      "startDate": { "type": ["string", "null"] },
                      "endDate": { "type": ["string", "null"] },
                      "bulletList": { "type": ["array", "null"], "items": { "type": "string" } },
                      "note": { "type": ["string", "null"] }
                    },
                    "required": ["companyName", "position", "startDate", "endDate", "bulletList", "note"],
                    "additionalProperties": false
                  }
                },
                "contact": {
                  "type": ["array", "null"],
                  "items": {
                    "type": "object",
                    "properties": {
                      "type": { "type": ["string", "null"], "enum": ["Phone", "Website", "Email", null] },
                      "displayValue": { "type": ["string", "null"] },
                      "url": { "type": ["string", "null"] },
                      "mailTo": { "type": ["string", "null"] }
                    },
                    "required": ["type", "displayValue", "url", "mailTo"],
                    "additionalProperties": false
                  }
                },
                "education": {
                  "type": ["array", "null"],
                  "items": {
                    "type": "object",
                    "properties": {
                      "name": { "type": ["string", "null"] },
                      "degree": { "type": "boolean" },
                      "degreeName": { "type": ["string", "null"] },
                      "degreeYear": { "type": ["string", "null"] },
                      "areasOfStudy": { "type": ["array", "null"], "items": { "type": "string" } }
                    },
                    "required": ["name", "degree", "degreeName", "degreeYear", "areasOfStudy"],
                    "additionalProperties": false
                  }
                },
                "customSections": {
                  "type": ["array", "null"],
                  "items": {
                    "type": "object",
                    "properties": {
                      "name": { "type": ["string", "null"] },
                      "customItems": {
                        "type": ["array", "null"],
                        "items": {
                          "type": "object",
                          "properties": {
                            "value": { "type": ["string", "null"] },
                            "type": {
                              "type": ["string", "null"],
                              "enum": ["Lang", "Win", "Comp", "CompNetwork", "Cloud", "RDB", "DDB", "DataLang", null]
                            }
                          },
                          "required": ["value", "type"],
                          "additionalProperties": false
                        }
                      }
                    },
                    "required": ["name", "customItems"],
                    "additionalProperties": false
                  }
                }
              },
              "required": [
                "fName", "mName", "lName", "position", "subtitle", "simpleGoal",
                "profile", "workExperience", "contact", "education", "customSections"
              ],
              "additionalProperties": false
            }
            """;
    }
}
