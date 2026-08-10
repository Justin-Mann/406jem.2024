using ResumeFunctions.Auth.Email;
using Xunit;

namespace ResumeFunctions.Tests.Auth.Email;

public class EmailTemplatesTests
{
    [Fact]
    public void Render_SubstitutesTokensInBothHtmlAndText()
    {
        var (html, text) = EmailTemplates.Render(
            "<p>Hi {{name}}, your code is {{code}}.</p>",
            "Hi {{name}}, your code is {{code}}.",
            new Dictionary<string, string> { ["name"] = "Justin", ["code"] = "123456" });

        Assert.Equal("<p>Hi Justin, your code is 123456.</p>", html);
        Assert.Equal("Hi Justin, your code is 123456.", text);
    }

    [Fact]
    public void Render_LeavesUnmatchedTokensAsIs()
    {
        var (html, text) = EmailTemplates.Render(
            "Hello {{missing}}",
            "Hello {{missing}}",
            new Dictionary<string, string>());

        Assert.Equal("Hello {{missing}}", html);
        Assert.Equal("Hello {{missing}}", text);
    }

    [Fact]
    public void Render_SubstitutesRepeatedToken()
    {
        var (html, _) = EmailTemplates.Render(
            "{{name}} {{name}}",
            "{{name}} {{name}}",
            new Dictionary<string, string> { ["name"] = "Justin" });

        Assert.Equal("Justin Justin", html);
    }
}
