using System.Collections.Generic;
using FluentAssertions;
using Xunit;

namespace MDNote.Core.Tests;

public class OneNoteHtmlToMarkdownTests
{
    private readonly OneNoteHtmlToMarkdown _converter = new OneNoteHtmlToMarkdown();

    // --- Null / Empty ---

    [Fact]
    public void Convert_Null_ReturnsEmpty()
    {
        _converter.Convert((string)null).Should().BeEmpty();
    }

    [Fact]
    public void Convert_Empty_ReturnsEmpty()
    {
        _converter.Convert("").Should().BeEmpty();
    }

    [Fact]
    public void Convert_NullList_ReturnsEmpty()
    {
        _converter.Convert((List<string>)null).Should().BeEmpty();
    }

    [Fact]
    public void Convert_EmptyList_ReturnsEmpty()
    {
        _converter.Convert(new List<string>()).Should().BeEmpty();
    }

    // --- Headings ---

    [Theory]
    [InlineData(20, "#")]
    [InlineData(16, "##")]
    [InlineData(13, "###")]
    [InlineData(11, "####")]
    [InlineData(10, "#####")]
    [InlineData(9, "######")]
    public void Convert_HeadingByFontSize_ProducesCorrectLevel(int fontSize, string prefix)
    {
        var html = $"<p style=\"font-size:{fontSize}pt;font-weight:bold\">Heading Text</p>";
        var md = _converter.Convert(html);
        md.Should().Contain($"{prefix} Heading Text");
    }

    [Fact]
    public void Convert_HeadingWithInlineTags_StripsHtml()
    {
        var html = "<p style=\"font-size:20pt;font-weight:bold\"><span>Title</span></p>";
        var md = _converter.Convert(html);
        md.Should().Contain("# Title");
    }

    // --- Bold / Italic / Strikethrough ---

    [Fact]
    public void Convert_StrongTag_ProducesDoubleStar()
    {
        var html = "<p><strong>bold text</strong></p>";
        var md = _converter.Convert(html);
        md.Should().Contain("**bold text**");
    }

    [Fact]
    public void Convert_BTag_ProducesDoubleStar()
    {
        var html = "<p><b>bold text</b></p>";
        var md = _converter.Convert(html);
        md.Should().Contain("**bold text**");
    }

    [Fact]
    public void Convert_EmTag_ProducesSingleStar()
    {
        var html = "<p><em>italic text</em></p>";
        var md = _converter.Convert(html);
        md.Should().Contain("*italic text*");
    }

    [Fact]
    public void Convert_ITag_ProducesSingleStar()
    {
        var html = "<p><i>italic text</i></p>";
        var md = _converter.Convert(html);
        md.Should().Contain("*italic text*");
    }

    [Fact]
    public void Convert_DelTag_ProducesTildes()
    {
        var html = "<p><del>deleted</del></p>";
        var md = _converter.Convert(html);
        md.Should().Contain("~~deleted~~");
    }

    // --- Links ---

    [Fact]
    public void Convert_AnchorTag_ProducesMarkdownLink()
    {
        var html = "<p><a href=\"https://example.com\">Example</a></p>";
        var md = _converter.Convert(html);
        md.Should().Contain("[Example](https://example.com)");
    }

    [Fact]
    public void Convert_AnchorWithNestedTags_StripsInnerHtml()
    {
        var html = "<p><a href=\"https://example.com\"><strong>Bold Link</strong></a></p>";
        var md = _converter.Convert(html);
        md.Should().Contain("[Bold Link](https://example.com)");
    }

    // --- Images ---

    [Fact]
    public void Convert_ImgWithAlt_ProducesMarkdownImage()
    {
        var html = "<img src=\"photo.png\" alt=\"A photo\"/>";
        var md = _converter.Convert(html);
        md.Should().Contain("![A photo](photo.png)");
    }

    [Fact]
    public void Convert_ImgWithoutAlt_ProducesEmptyAltImage()
    {
        var html = "<img src=\"photo.png\"/>";
        var md = _converter.Convert(html);
        md.Should().Contain("![](photo.png)");
    }

    // --- Code Blocks ---

    [Fact]
    public void Convert_CodeBlockTableWithLabel_ProducesFencedBlock()
    {
        var html =
            "<table style=\"border-collapse:collapse;width:100%;margin:8px 0;\">" +
            "<tr><td style=\"background:#2d2d2d;color:#858585;padding:4px 12px;" +
            "font-family:Consolas,'Courier New',monospace;font-size:9pt;border:1px solid #444;\">C#</td></tr>" +
            "<tr><td style=\"background:#1e1e1e;color:#dadada;padding:12px;" +
            "font-family:Consolas,'Courier New',monospace;font-size:10pt;" +
            "border:1px solid #444;white-space:pre;\">var x = 42;</td></tr></table>";

        var md = _converter.Convert(html);
        md.Should().Contain("```csharp");
        md.Should().Contain("var x = 42;");
        md.Should().Contain("```");
    }

    [Fact]
    public void Convert_CodeBlockTableWithoutLabel_ProducesFencedBlockNoLang()
    {
        var html =
            "<table style=\"border-collapse:collapse;width:100%;margin:8px 0;\">" +
            "<tr><td style=\"background:#1e1e1e;color:#dadada;padding:12px;" +
            "font-family:Consolas,'Courier New',monospace;font-size:10pt;" +
            "border:1px solid #444;white-space:pre;\">echo hello</td></tr></table>";

        var md = _converter.Convert(html);
        md.Should().Contain("```");
        md.Should().Contain("echo hello");
    }

    [Fact]
    public void Convert_CodeBlockWithColorSpans_StripsSpanTags()
    {
        var html =
            "<table style=\"border-collapse:collapse;width:100%;margin:8px 0;\">" +
            "<tr><td style=\"background:#2d2d2d;color:#858585;padding:4px 12px;" +
            "font-family:Consolas,'Courier New',monospace;font-size:9pt;border:1px solid #444;\">Python</td></tr>" +
            "<tr><td style=\"background:#1e1e1e;color:#dadada;padding:12px;" +
            "font-family:Consolas,'Courier New',monospace;font-size:10pt;" +
            "border:1px solid #444;white-space:pre;\">" +
            "<span style=\"color:#569CD6\">def</span> hello():" +
            "</td></tr></table>";

        var md = _converter.Convert(html);
        md.Should().Contain("```python");
        md.Should().Contain("def hello():");
        md.Should().NotContain("<span");
        md.Should().NotContain("color:");
    }

    [Fact]
    public void Convert_CodeBlockWithHtmlEntities_DecodesEntities()
    {
        var html =
            "<table style=\"border-collapse:collapse;width:100%;margin:8px 0;\">" +
            "<tr><td style=\"background:#1e1e1e;color:#dadada;padding:12px;" +
            "font-family:Consolas,'Courier New',monospace;font-size:10pt;" +
            "border:1px solid #444;white-space:pre;\">" +
            "x &lt; 10 &amp;&amp; y &gt; 5</td></tr></table>";

        var md = _converter.Convert(html);
        md.Should().Contain("x < 10 && y > 5");
    }

    [Theory]
    [InlineData("JavaScript", "javascript")]
    [InlineData("TypeScript", "typescript")]
    [InlineData("SQL", "sql")]
    [InlineData("HTML", "html")]
    [InlineData("PowerShell", "powershell")]
    [InlineData("Go", "go")]
    [InlineData("Rust", "rust")]
    public void Convert_CodeBlockLanguageLabel_MapsToCorrectAlias(string displayName, string expectedAlias)
    {
        var html =
            "<table style=\"border-collapse:collapse;width:100%;margin:8px 0;\">" +
            $"<tr><td style=\"background:#2d2d2d;color:#858585;padding:4px 12px;" +
            $"font-family:Consolas,'Courier New',monospace;font-size:9pt;border:1px solid #444;\">{displayName}</td></tr>" +
            "<tr><td style=\"background:#1e1e1e;color:#dadada;padding:12px;" +
            "font-family:Consolas,'Courier New',monospace;font-size:10pt;" +
            "border:1px solid #444;white-space:pre;\">code</td></tr></table>";

        var md = _converter.Convert(html);
        md.Should().Contain($"```{expectedAlias}");
    }

    // --- Blockquotes ---

    [Fact]
    public void Convert_BlockquoteStyle_ProducesGreaterThan()
    {
        var html = "<p style=\"margin-left:28px;border-left:3px solid #ccc;padding-left:12px;color:#555\">A wise quote</p>";
        var md = _converter.Convert(html);
        md.Should().Contain("> A wise quote");
    }

    // --- Horizontal Rules ---

    [Fact]
    public void Convert_HrStyle_ProducesThreeDashes()
    {
        var html = "<p>Above</p><p style=\"border-bottom:1px solid #ccc\">&nbsp;</p><p>Below</p>";
        var md = _converter.Convert(html);
        md.Should().Contain("---");
    }

    // --- Lists ---

    [Fact]
    public void Convert_UnorderedList_ProducesDashItems()
    {
        var html = "<ul><li>Item A</li><li>Item B</li><li>Item C</li></ul>";
        var md = _converter.Convert(html);
        md.Should().Contain("- Item A");
        md.Should().Contain("- Item B");
        md.Should().Contain("- Item C");
    }

    [Fact]
    public void Convert_OrderedList_ProducesNumberedItems()
    {
        var html = "<ol><li>First</li><li>Second</li><li>Third</li></ol>";
        var md = _converter.Convert(html);
        md.Should().Contain("1. First");
        md.Should().Contain("2. Second");
        md.Should().Contain("3. Third");
    }

    [Fact]
    public void Convert_ListItemWithFormatting_PreservesInline()
    {
        var html = "<ul><li><strong>Bold item</strong></li></ul>";
        var md = _converter.Convert(html);
        md.Should().Contain("- **Bold item**");
    }

    // --- Tables ---

    [Fact]
    public void Convert_SimpleTable_ProducesPipeSyntax()
    {
        var html =
            "<table style=\"border-collapse:collapse;margin:8px 0\">" +
            "<tr><td style=\"border:1px solid #ccc;padding:6px 10px;font-weight:bold\">Name</td>" +
            "<td style=\"border:1px solid #ccc;padding:6px 10px;font-weight:bold\">Age</td></tr>" +
            "<tr><td style=\"border:1px solid #ccc;padding:6px 10px\">Alice</td>" +
            "<td style=\"border:1px solid #ccc;padding:6px 10px\">30</td></tr></table>";

        var md = _converter.Convert(html);
        md.Should().Contain("| Name | Age |");
        md.Should().Contain("| --- | --- |");
        md.Should().Contain("| Alice | 30 |");
    }

    [Fact]
    public void Convert_TableWithoutBoldHeader_StillProducesPipeTable()
    {
        var html =
            "<table style=\"border-collapse:collapse;margin:8px 0\">" +
            "<tr><td style=\"border:1px solid #ccc;padding:6px 10px\">A</td>" +
            "<td style=\"border:1px solid #ccc;padding:6px 10px\">B</td></tr>" +
            "<tr><td style=\"border:1px solid #ccc;padding:6px 10px\">1</td>" +
            "<td style=\"border:1px solid #ccc;padding:6px 10px\">2</td></tr></table>";

        var md = _converter.Convert(html);
        md.Should().Contain("| A | B |");
        md.Should().Contain("| --- | --- |");
        md.Should().Contain("| 1 | 2 |");
    }

    // --- Checkboxes ---

    [Fact]
    public void Convert_CheckedUnicode_ProducesTaskListChecked()
    {
        var html = "<p>\u2611 Buy groceries</p>";
        var md = _converter.Convert(html);
        md.Should().Contain("- [x] Buy groceries");
    }

    [Fact]
    public void Convert_UncheckedUnicode_ProducesTaskListUnchecked()
    {
        var html = "<p>\u2610 Read a book</p>";
        var md = _converter.Convert(html);
        md.Should().Contain("- [ ] Read a book");
    }

    // --- Highlight ---

    [Fact]
    public void Convert_YellowHighlightSpan_ProducesDoubleEquals()
    {
        var html = "<p>This is <span style=\"background-color:#ffff00\">highlighted</span> text</p>";
        var md = _converter.Convert(html);
        md.Should().Contain("==highlighted==");
    }

    // --- Plain Text ---

    [Fact]
    public void Convert_PlainTextInParagraph_PreservesText()
    {
        var html = "<p>Just some plain text</p>";
        var md = _converter.Convert(html);
        md.Should().Contain("Just some plain text");
    }

    // --- HTML Entities ---

    [Fact]
    public void Convert_HtmlEntities_DecodedCorrectly()
    {
        var html = "<p>&amp; &lt; &gt; &quot;</p>";
        var md = _converter.Convert(html);
        md.Should().Contain("& < > \"");
    }

    // --- Line Breaks ---

    [Fact]
    public void Convert_BrTag_ProducesNewline()
    {
        var html = "<p>Line one<br/>Line two</p>";
        var md = _converter.Convert(html);
        md.Should().Contain("Line one\nLine two");
    }

    // --- Multi-fragment ---

    [Fact]
    public void Convert_MultipleFragments_JoinsCorrectly()
    {
        var fragments = new List<string>
        {
            "<p style=\"font-size:20pt;font-weight:bold\">Title</p>",
            "<p>Some text</p>",
            "<p><strong>Bold</strong></p>"
        };

        var md = _converter.Convert(fragments);
        md.Should().Contain("# Title");
        md.Should().Contain("Some text");
        md.Should().Contain("**Bold**");
    }

    // --- Mixed Content ---

    [Fact]
    public void Convert_MixedContent_ProducesCorrectMarkdown()
    {
        var html =
            "<p style=\"font-size:20pt;font-weight:bold\">My Document</p>" +
            "<p>Some <strong>bold</strong> and <em>italic</em> text.</p>" +
            "<ul><li>Item 1</li><li>Item 2</li></ul>" +
            "<p style=\"border-bottom:1px solid #ccc\">&nbsp;</p>" +
            "<p>After the rule.</p>";

        var md = _converter.Convert(html);
        md.Should().Contain("# My Document");
        md.Should().Contain("**bold**");
        md.Should().Contain("*italic*");
        md.Should().Contain("- Item 1");
        md.Should().Contain("---");
        md.Should().Contain("After the rule.");
    }

    // --- Multiple blank lines collapsed ---

    [Fact]
    public void Convert_ExcessiveWhitespace_CollapsedToDoubleNewline()
    {
        var html = "<p>Paragraph 1</p><p></p><p></p><p></p><p>Paragraph 2</p>";
        var md = _converter.Convert(html);
        // Should not have more than 2 consecutive newlines
        md.Should().NotContain("\n\n\n");
    }
}
