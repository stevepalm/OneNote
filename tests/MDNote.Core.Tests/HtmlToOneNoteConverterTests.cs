using FluentAssertions;
using Xunit;

namespace MDNote.Core.Tests;

public class HtmlToOneNoteConverterTests
{
    private readonly HtmlToOneNoteConverter _converter = new HtmlToOneNoteConverter();

    [Fact]
    public void ConvertForOneNote_Null_ReturnsEmpty()
    {
        _converter.ConvertForOneNote(null).Should().BeEmpty();
    }

    [Fact]
    public void ConvertForOneNote_Empty_ReturnsEmpty()
    {
        _converter.ConvertForOneNote("").Should().BeEmpty();
    }

    [Fact]
    public void ConvertForOneNote_PlainCodeBlock_RendersAsTable()
    {
        var html = "<pre><code>var x = 1;</code></pre>";
        var result = _converter.ConvertForOneNote(html);
        result.Should().Contain("<table");
        result.Should().Contain("Consolas");
        result.Should().Contain("var x = 1;");
        result.Should().NotContain("<pre>");
    }

    [Fact]
    public void ConvertForOneNote_CodeBlockWithLanguage_RendersTableWithLabel()
    {
        var html = "<pre><code class=\"language-csharp\">int x = 42;</code></pre>";
        var result = _converter.ConvertForOneNote(html);
        result.Should().Contain("<table");
        result.Should().Contain("C#");
        result.Should().Contain("int x = 42;");
    }

    [Fact]
    public void ConvertForOneNote_Blockquote_ConvertsToStyledP()
    {
        var html = "<blockquote><p>A wise quote</p></blockquote>";
        var result = _converter.ConvertForOneNote(html);
        result.Should().Contain("border-left:3px solid #ccc");
        result.Should().Contain("margin-left:28px");
        result.Should().Contain("A wise quote");
        result.Should().NotContain("<blockquote>");
    }

    [Fact]
    public void ConvertForOneNote_NestedBlockquotes_HandledIteratively()
    {
        var html = "<blockquote><blockquote><p>Nested</p></blockquote></blockquote>";
        var result = _converter.ConvertForOneNote(html);
        result.Should().NotContain("<blockquote>");
        result.Should().Contain("Nested");
    }

    [Fact]
    public void ConvertForOneNote_Hr_ConvertsToStyledP()
    {
        var html = "<p>Above</p><hr/><p>Below</p>";
        var result = _converter.ConvertForOneNote(html);
        result.Should().Contain("border-bottom:1px solid #ccc");
        result.Should().NotContain("<hr");
    }

    [Fact]
    public void ConvertForOneNote_CheckedCheckbox_ReplacedWithUnicode()
    {
        var html = "<input type=\"checkbox\" checked/>";
        var result = _converter.ConvertForOneNote(html);
        result.Should().Contain("\u2611");
        result.Should().NotContain("<input");
    }

    [Fact]
    public void ConvertForOneNote_UncheckedCheckbox_ReplacedWithUnicode()
    {
        var html = "<input type=\"checkbox\"/>";
        var result = _converter.ConvertForOneNote(html);
        result.Should().Contain("\u2610");
        result.Should().NotContain("<input");
    }

    [Fact]
    public void ConvertForOneNote_Mark_ConvertsToSpanWithYellowBg()
    {
        var html = "<mark>highlighted</mark>";
        var result = _converter.ConvertForOneNote(html);
        result.Should().Contain("<span style=\"background-color:#ffff00\">");
        result.Should().Contain("highlighted");
        result.Should().NotContain("<mark>");
    }

    [Theory]
    [InlineData(1, "20pt")]
    [InlineData(2, "16pt")]
    [InlineData(3, "13pt")]
    [InlineData(4, "11pt")]
    [InlineData(5, "10pt")]
    [InlineData(6, "9pt")]
    public void ConvertForOneNote_Headings_ConvertedToPWithFontSize(int level, string expectedSize)
    {
        var html = $"<h{level}>Test Heading</h{level}>";
        var result = _converter.ConvertForOneNote(html);
        result.Should().Contain($"font-size:{expectedSize}");
        result.Should().Contain("font-weight:bold");
        result.Should().Contain("Test Heading");
        result.Should().NotContain($"<h{level}");
    }

    [Fact]
    public void ConvertForOneNote_TheadTbody_Stripped()
    {
        var html = "<table><thead><tr><th>Col</th></tr></thead><tbody><tr><td>Val</td></tr></tbody></table>";
        var result = _converter.ConvertForOneNote(html);
        result.Should().NotContain("<thead>");
        result.Should().NotContain("</thead>");
        result.Should().NotContain("<tbody>");
        result.Should().NotContain("</tbody>");
    }

    [Fact]
    public void ConvertForOneNote_Th_ConvertedToTdWithBold()
    {
        var html = "<table><tr><th>Header</th></tr></table>";
        var result = _converter.ConvertForOneNote(html);
        result.Should().NotContain("<th");
        result.Should().NotContain("</th>");
        result.Should().Contain("<td");
        result.Should().Contain("font-weight:bold");
        result.Should().Contain("Header");
    }

    [Fact]
    public void ConvertForOneNote_TableWithoutStyle_AddsBorderStyle()
    {
        var html = "<table><tr><td>Cell</td></tr></table>";
        var result = _converter.ConvertForOneNote(html);
        result.Should().Contain("border-collapse:collapse");
        result.Should().Contain("border:1px solid #ccc");
    }

    [Fact]
    public void ConvertForOneNote_PassthroughElements_Preserved()
    {
        var html = "<p><strong>bold</strong> <em>italic</em> <a href=\"#\">link</a></p>";
        var result = _converter.ConvertForOneNote(html);
        result.Should().Contain("<strong>bold</strong>");
        result.Should().Contain("<em>italic</em>");
        result.Should().Contain("<a href=\"#\">link</a>");
    }
}
