using FluentAssertions;
using MDNote.Core;
using Xunit;

namespace MDNote.Core.Tests;

public class FootnoteAndMiscTests
{
    private readonly IMarkdownConverter _converter = new MarkdownConverter();

    [Fact]
    public void Convert_Footnote_ProducesFootnoteMarkup()
    {
        var md = "Text with a footnote[^1].\n\n[^1]: This is the footnote.";
        var result = _converter.Convert(md);

        result.Html.Should().Contain("footnote");
        result.Html.Should().Contain("This is the footnote.");
    }

    [Fact]
    public void Convert_HorizontalRule_ProducesHrTag()
    {
        var result = _converter.Convert("Above\n\n---\n\nBelow");

        result.Html.Should().Contain("<hr");
    }

    [Fact]
    public void Convert_Emoji_RendersEmoji()
    {
        var result = _converter.Convert("Hello :smile: world");

        // Markdig should convert :smile: to the actual emoji character
        result.Html.Should().NotContain(":smile:");
    }

    [Fact]
    public void Convert_DefinitionList_ProducesCorrectTags()
    {
        var md = "Term\n:   Definition of the term";
        var result = _converter.Convert(md);

        result.Html.Should().Contain("<dl>");
        result.Html.Should().Contain("<dt>");
        result.Html.Should().Contain("<dd>");
    }

    [Fact]
    public void Convert_Abbreviation_ProducesAbbrTag()
    {
        var md = "*[HTML]: Hyper Text Markup Language\n\nThe HTML specification is maintained.";
        var result = _converter.Convert(md);

        result.Html.Should().Contain("<abbr");
        result.Html.Should().Contain("Hyper Text Markup Language");
    }
}
