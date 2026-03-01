using FluentAssertions;
using MDNote.Core;
using MDNote.Core.Models;
using Xunit;

namespace MDNote.Core.Tests;

public class CodeBlockTests
{
    private readonly IMarkdownConverter _converter = new MarkdownConverter();

    [Fact]
    public void Convert_FencedCodeWithLanguage_HasInlineStyles()
    {
        var md = "```csharp\nvar x = 42;\n```";
        var result = _converter.Convert(md);

        result.Html.Should().Contain("style=");
        result.Html.Should().Contain("var");
        result.Html.Should().Contain("42");
    }

    [Fact]
    public void Convert_FencedCodeWithLanguage_ShowsLanguageLabel()
    {
        var md = "```javascript\nconsole.log('hi');\n```";
        var result = _converter.Convert(md);

        result.Html.Should().Contain("JavaScript");
    }

    [Fact]
    public void Convert_FencedCodeWithoutLanguage_FallsBackToMonospace()
    {
        var md = "```\nplain code here\n```";
        var result = _converter.Convert(md);

        result.Html.Should().Contain("plain code here");
        result.Html.Should().Contain("style=");
    }

    [Fact]
    public void Convert_FencedCode_HasDarkBackground()
    {
        var md = "```python\nprint('hello')\n```";
        var result = _converter.Convert(md);

        result.Html.Should().Contain("#1E1E1E");
    }

    [Fact]
    public void Convert_UnsupportedLanguage_FallsBackGracefully()
    {
        var md = "```rust\nfn main() {}\n```";
        var result = _converter.Convert(md);

        result.Html.Should().Contain("Rust");
        result.Html.Should().Contain("fn main()");
        result.Html.Should().Contain("style=");
    }

    [Fact]
    public void Convert_CodeBlock_NoStyleTags()
    {
        var md = "```csharp\nvar x = 42;\n```";
        var result = _converter.Convert(md);

        result.Html.Should().NotContain("<style");
    }

    [Fact]
    public void Convert_CodeWithHtmlEntities_HandledCorrectly()
    {
        var md = "```html\n<div class=\"test\">Hello</div>\n```";
        var result = _converter.Convert(md);

        result.Html.Should().Contain("style=");
        result.Html.Should().Contain("div");
        result.Html.Should().Contain("Hello");
    }

    [Fact]
    public void Convert_InlineCode_NotHighlighted()
    {
        var result = _converter.Convert("Use `var x = 1;` inline.");

        // Inline code should be simple <code> tag, not wrapped in highlight div
        result.Html.Should().Contain("<code>var x = 1;</code>");
    }

    [Fact]
    public void Convert_SyntaxHighlightingDisabled_NoColorStyles()
    {
        var md = "```csharp\nvar x = 42;\n```";
        var options = new ConversionOptions { EnableSyntaxHighlighting = false };
        var result = _converter.Convert(md, options);

        // Should still have the code block but as plain Markdig output
        result.Html.Should().Contain("<pre>");
        result.Html.Should().Contain("<code");
    }

    [Fact]
    public void Convert_MultipleSupportedLanguages_AllHighlighted()
    {
        var md = "```csharp\nvar x = 1;\n```\n\n```python\nx = 1\n```\n\n```javascript\nlet x = 1;\n```";
        var result = _converter.Convert(md);

        result.Html.Should().Contain("C#");
        result.Html.Should().Contain("Python");
        result.Html.Should().Contain("JavaScript");
    }
}
