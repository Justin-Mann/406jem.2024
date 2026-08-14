using System.Text.Json;
using Anthropic;
using Anthropic.Models.Messages;
using ResumeFunctions.Auth.Parsing;

var apiKey = Environment.GetEnvironmentVariable("DIAG_ANTHROPIC_KEY");
if (string.IsNullOrWhiteSpace(apiKey))
{
    Console.WriteLine("DIAG: no API key in env var.");
    return;
}

var pdfPath = args.Length > 0 ? args[0] : "jmResume.8.2025.pdf";
using var pdfStream = File.OpenRead(pdfPath);

var extractor = new PdfPigTextExtractor();
var text = extractor.ExtractText(pdfStream);
Console.WriteLine($"DIAG: extracted {text.Length} characters of text from PDF.");

const string SystemPrompt = """
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

const string SchemaJson = """
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
              "type": {
                "anyOf": [
                  { "type": "string", "enum": ["Phone", "Website", "Email"] },
                  { "type": "null" }
                ]
              },
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
                      "anyOf": [
                        { "type": "string", "enum": ["Lang", "Win", "Comp", "CompNetwork", "Cloud", "RDB", "DDB", "DataLang"] },
                        { "type": "null" }
                      ]
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

using var document = JsonDocument.Parse(SchemaJson);
var schema = document.RootElement.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.Clone());

var truncatedText = text.Length > 60_000 ? text[..60_000] : text;

AnthropicClient client = new() { ApiKey = apiKey };

var response = await client.Messages.Create(new MessageCreateParams
{
    Model = "claude-haiku-4-5-20251001",
    MaxTokens = 8000,
    System = SystemPrompt,
    OutputConfig = new OutputConfig
    {
        Format = new JsonOutputFormat { Schema = schema },
    },
    Messages = [new() { Role = Role.User, Content = truncatedText }],
});

Console.WriteLine("DIAG: SUCCESS (no exception thrown)");
Console.WriteLine("StopReason: " + response.StopReason);
var responseText = response.Content.Select(b => b.Value).OfType<TextBlock>().FirstOrDefault()?.Text;
Console.WriteLine("Text length: " + (responseText?.Length ?? -1));
Console.WriteLine("Text (first 1000 chars): " + responseText?[..Math.Min(1000, responseText.Length)]);
