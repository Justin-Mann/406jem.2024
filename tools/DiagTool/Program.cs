using Anthropic;
using Anthropic.Models.Messages;
using System.Text.Json;

var apiKey = Environment.GetEnvironmentVariable("DIAG_ANTHROPIC_KEY");
if (string.IsNullOrWhiteSpace(apiKey))
{
    Console.WriteLine("DIAG: no API key in env var.");
    return;
}

const string SchemaJson = """
    {
      "type": "object",
      "properties": {
        "fName": { "type": ["string", "null"] }
      },
      "required": ["fName"],
      "additionalProperties": false
    }
    """;

using var document = JsonDocument.Parse(SchemaJson);
var schema = document.RootElement.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.Clone());

try
{
    AnthropicClient client = new() { ApiKey = apiKey };

    var response = await client.Messages.Create(new MessageCreateParams
    {
        Model = "claude-haiku-4-5-20251001",
        MaxTokens = 200,
        System = "Extract the first name.",
        OutputConfig = new OutputConfig
        {
            Format = new JsonOutputFormat { Schema = schema },
        },
        Messages = [new() { Role = Role.User, Content = "My name is Justin Mann." }],
    });

    Console.WriteLine("DIAG: SUCCESS");
    Console.WriteLine("StopReason: " + response.StopReason);
    var text = response.Content.Select(b => b.Value).OfType<TextBlock>().FirstOrDefault()?.Text;
    Console.WriteLine("Text: " + text);
}
catch (Exception ex)
{
    Console.WriteLine("DIAG: EXCEPTION");
    Console.WriteLine(ex.GetType().FullName);
    Console.WriteLine(ex.Message);
    Console.WriteLine(ex.ToString());
    if (ex.InnerException is not null)
    {
        Console.WriteLine("INNER: " + ex.InnerException);
    }
}
