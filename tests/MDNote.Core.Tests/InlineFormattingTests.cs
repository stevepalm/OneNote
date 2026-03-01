using FluentAssertions;
using MDNote.Core;
using Xunit;

namespace MDNote.Core.Tests;

public class InlineFormattingTests
{
    private readonly IMarkdownConverter _converter = new MarkdownConverter();

    [Fact]
    public void Convert_Bold_ProducesStrongTag()
    {
        var result = _converter.Convert("This is **bold** text.");

        result.Html.Should().Contain("<strong>bold</strong>");
    }

    [Fact]
    public void Convert_Italic_ProducesEmTag()
    {
        var result = _converter.Convert("This is *italic* text.");

        result.Html.Should().Contain("<em>italic</em>");
    }

    [Fact]
    public void Convert_Strikethrough_ProducesDelTag()
    {
        var result = _converter.Convert("This is ~~deleted~~ text.");

        result.Html.Should().Contain("<del>deleted</del>");
    }

    [Fact]
    public void Convert_InlineCode_ProducesCodeTag()
    {
        var result = _converter.Convert("Use `console.log()` for debugging.");

        result.Html.Should().Contain("<code>console.log()</code>");
    }

    [Fact]
    public void Convert_BoldAndItalic_ProducesCombinedTags()
    {
        var result = _converter.Convert("This is ***bold and italic*** text.");

        result.Html.Should().Contain("<em>");
        result.Html.Should().Contain("<strong>");
        result.Html.Should().Contain("bold and italic");
    }

    [Fact]
    public void Convert_BoldInsideItalic_Nests()
    {
        var result = _converter.Convert("*italic and **bold** inside*");

        result.Html.Should().Contain("<em>");
        result.Html.Should().Contain("<strong>bold</strong>");
    }
}
