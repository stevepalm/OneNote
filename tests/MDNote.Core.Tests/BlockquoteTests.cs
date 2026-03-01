using FluentAssertions;
using MDNote.Core;
using Xunit;

namespace MDNote.Core.Tests;

public class BlockquoteTests
{
    private readonly IMarkdownConverter _converter = new MarkdownConverter();

    [Fact]
    public void Convert_SimpleBlockquote_ProducesBlockquoteTag()
    {
        var result = _converter.Convert("> This is a quote.");

        result.Html.Should().Contain("<blockquote>");
        result.Html.Should().Contain("This is a quote.");
    }

    [Fact]
    public void Convert_NestedBlockquote_ProducesNestedTags()
    {
        var md = "> Outer\n>> Inner";
        var result = _converter.Convert(md);

        // Should have nested blockquotes
        var count = System.Text.RegularExpressions.Regex.Matches(result.Html, "<blockquote>").Count;
        count.Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public void Convert_BlockquoteWithFormatting_PreservesInlineFormatting()
    {
        var result = _converter.Convert("> This is **bold** in a quote.");

        result.Html.Should().Contain("<blockquote>");
        result.Html.Should().Contain("<strong>bold</strong>");
    }
}
