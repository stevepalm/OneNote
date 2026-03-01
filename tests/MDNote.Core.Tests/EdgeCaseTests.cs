using FluentAssertions;
using MDNote.Core;
using MDNote.Core.Models;
using Xunit;

namespace MDNote.Core.Tests;

public class EdgeCaseTests
{
    private readonly IMarkdownConverter _converter = new MarkdownConverter();

    [Fact]
    public void Convert_EmptyString_ReturnsEmptyHtml()
    {
        var result = _converter.Convert("");

        result.Html.Should().BeEmpty();
        result.Title.Should().BeNull();
        result.Headings.Should().BeEmpty();
        result.MermaidBlocks.Should().BeEmpty();
        result.FrontMatter.Should().BeEmpty();
    }

    [Fact]
    public void Convert_Null_ReturnsEmptyHtml()
    {
        var result = _converter.Convert(null);

        result.Html.Should().BeEmpty();
    }

    [Fact]
    public void Convert_WhitespaceOnly_ReturnsMinimalHtml()
    {
        var result = _converter.Convert("   \n  \n   ");

        result.Title.Should().BeNull();
        result.Headings.Should().BeEmpty();
    }

    [Fact]
    public void Convert_SingleLine_Works()
    {
        var result = _converter.Convert("Hello world");

        result.Html.Should().Contain("Hello world");
    }

    [Fact]
    public void Convert_LargeDocument_DoesNotThrow()
    {
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < 1000; i++)
        {
            sb.AppendLine($"## Heading {i}");
            sb.AppendLine($"Paragraph {i} with **bold** and *italic* text.");
            sb.AppendLine();
        }

        var result = _converter.Convert(sb.ToString());

        result.Html.Should().NotBeNullOrEmpty();
        result.Headings.Should().HaveCount(1000);
    }

    [Fact]
    public void Convert_NullOptions_UsesDefaults()
    {
        var result = _converter.Convert("# Test", null);

        result.Html.Should().Contain("<h1");
    }

    [Fact]
    public void Convert_SpecialCharacters_HandledCorrectly()
    {
        var result = _converter.Convert("Symbols: < > & \" '");

        result.Html.Should().Contain("&lt;");
        result.Html.Should().Contain("&gt;");
        result.Html.Should().Contain("&amp;");
    }
}
