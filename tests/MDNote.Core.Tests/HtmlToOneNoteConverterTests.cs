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
        result.Should().Contain("border-left:3px solid");
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
    public void ConvertForOneNote_NestedBlockquotes_IncreasingIndent()
    {
        var html = "<blockquote><p>Outer</p><blockquote><p>Inner</p></blockquote></blockquote>";
        var result = _converter.ConvertForOneNote(html);
        // Innermost (depth 0) gets 28px, outermost (depth 1) gets 48px
        result.Should().Contain("margin-left:28px");
        result.Should().Contain("margin-left:48px");
    }

    [Fact]
    public void ConvertForOneNote_NestedBlockquotes_DifferentBorderColors()
    {
        var html = "<blockquote><blockquote><p>Inner</p></blockquote></blockquote>";
        var result = _converter.ConvertForOneNote(html);
        // Different border colors at different depths
        result.Should().Contain("border-left:3px solid #4a9eff");
        result.Should().Contain("border-left:3px solid #7b68ee");
    }

    [Fact]
    public void ConvertForOneNote_TripleNestedBlockquote_ThreeLevels()
    {
        var html = "<blockquote><blockquote><blockquote><p>Deep</p></blockquote></blockquote></blockquote>";
        var result = _converter.ConvertForOneNote(html);
        result.Should().NotContain("<blockquote>");
        result.Should().Contain("Deep");
        // Should have 3 different indent levels
        result.Should().Contain("margin-left:28px");
        result.Should().Contain("margin-left:48px");
        result.Should().Contain("margin-left:68px");
    }

    [Fact]
    public void ConvertForOneNote_BlockquoteWithFormatting_PreservesContent()
    {
        var html = "<blockquote><p><strong>Bold</strong> in a quote</p></blockquote>";
        var result = _converter.ConvertForOneNote(html);
        result.Should().Contain("<strong>Bold</strong>");
        result.Should().Contain("in a quote");
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
    public void ConvertForOneNote_TableHeaderCells_HaveBottomBorder()
    {
        var html = "<table><tr><th>Header</th></tr><tr><td>Data</td></tr></table>";
        var result = _converter.ConvertForOneNote(html);
        result.Should().Contain("border-bottom:2px solid #999");
    }

    [Fact]
    public void ConvertForOneNote_TableWithAlignment_PreservesTextAlign()
    {
        var html = "<table><tr><th style=\"text-align:center\">Center</th></tr></table>";
        var result = _converter.ConvertForOneNote(html);
        result.Should().Contain("text-align:center");
        result.Should().Contain("font-weight:bold");
        result.Should().Contain("border-bottom:2px solid #999");
    }

    [Fact]
    public void ConvertForOneNote_TableDataCells_NoHeaderBorder()
    {
        var html = "<table><tr><td>Data</td></tr></table>";
        var result = _converter.ConvertForOneNote(html);
        result.Should().NotContain("border-bottom:2px solid #999");
    }

    [Fact]
    public void ConvertForOneNote_TableAlignCenter_MergedStyles()
    {
        var html = "<table><tr><th style=\"text-align:right\">Right</th></tr></table>";
        var result = _converter.ConvertForOneNote(html);
        result.Should().Contain("text-align:right");
        result.Should().Contain("font-weight:bold");
        result.Should().Contain("border:1px solid #ccc");
    }

    [Fact]
    public void ConvertForOneNote_InlineCode_ConvertedToStyledSpan()
    {
        var html = "<p>Use <code>server-settings</code> for the key</p>";
        var result = _converter.ConvertForOneNote(html);
        result.Should().Contain("Consolas");
        result.Should().Contain("server-settings");
        result.Should().NotContain("<code>");
        result.Should().NotContain("</code>");
    }

    [Fact]
    public void ConvertForOneNote_MultipleInlineCodes_AllConverted()
    {
        var html = "<p>Set <code>key</code> and <code>value</code></p>";
        var result = _converter.ConvertForOneNote(html);
        result.Should().NotContain("<code>");
        result.Should().Contain("key");
        result.Should().Contain("value");
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

    // --- Task list spacing ---

    [Fact]
    public void ConvertForOneNote_CheckedBox_HasSpaceAfter()
    {
        var html = "<input type=\"checkbox\" checked/> Done";
        var result = _converter.ConvertForOneNote(html);
        result.Should().Contain("\u2611\u00A0");
    }

    [Fact]
    public void ConvertForOneNote_UncheckedBox_HasSpaceAfter()
    {
        var html = "<input type=\"checkbox\"/> Pending";
        var result = _converter.ConvertForOneNote(html);
        result.Should().Contain("\u2610\u00A0");
    }

    // --- Footnote styling ---

    [Fact]
    public void ConvertForOneNote_FootnoteRef_StyledAsSuperscript()
    {
        var html = "<a id=\"fnref:1\" href=\"#fn:1\" class=\"footnote-ref\"><sup>1</sup></a>";
        var result = _converter.ConvertForOneNote(html);
        result.Should().Contain("<sup style=\"font-size:8pt;color:#4a9eff\">[1]</sup>");
    }

    [Fact]
    public void ConvertForOneNote_FootnoteSection_HasBorderAndStyling()
    {
        var html = "<p>Text</p><div class=\"footnotes\"><hr/><ol><li id=\"fn:1\"><p>Note<a href=\"#fnref:1\" class=\"footnote-back-ref\">^</a></p></li></ol></div>";
        var result = _converter.ConvertForOneNote(html);
        result.Should().Contain("border-top:1px solid #ccc");
        result.Should().Contain("Footnotes");
        result.Should().Contain("font-size:9pt");
    }

    [Fact]
    public void ConvertForOneNote_FootnoteBackRef_Removed()
    {
        var html = "<div class=\"footnotes\"><ol><li><p>Note<a href=\"#fnref:1\" class=\"footnote-back-ref\">^</a></p></li></ol></div>";
        var result = _converter.ConvertForOneNote(html);
        result.Should().NotContain("footnote-back-ref");
        result.Should().Contain("Note");
    }

    [Fact]
    public void ConvertForOneNote_FootnoteMultipleRefs_AllStyled()
    {
        var html = "Text<a id=\"fnref:1\" href=\"#fn:1\" class=\"footnote-ref\"><sup>1</sup></a> more<a id=\"fnref:2\" href=\"#fn:2\" class=\"footnote-ref\"><sup>2</sup></a>";
        var result = _converter.ConvertForOneNote(html);
        result.Should().Contain("[1]");
        result.Should().Contain("[2]");
    }

    // --- Definition list conversion ---

    [Fact]
    public void ConvertForOneNote_DefinitionList_TermIsBold()
    {
        var html = "<dl><dt>Term</dt><dd>Definition</dd></dl>";
        var result = _converter.ConvertForOneNote(html);
        result.Should().Contain("font-weight:bold");
        result.Should().Contain("Term");
        result.Should().NotContain("<dl>");
        result.Should().NotContain("<dt>");
    }

    [Fact]
    public void ConvertForOneNote_DefinitionList_DefinitionIsIndented()
    {
        var html = "<dl><dt>Term</dt><dd>Definition</dd></dl>";
        var result = _converter.ConvertForOneNote(html);
        result.Should().Contain("margin:2px 0 8px 28px");
        result.Should().Contain("Definition");
        result.Should().NotContain("<dd>");
    }

    [Fact]
    public void ConvertForOneNote_DefinitionList_DlTagRemoved()
    {
        var html = "<dl><dt>A</dt><dd>B</dd></dl>";
        var result = _converter.ConvertForOneNote(html);
        result.Should().NotContain("<dl>");
        result.Should().NotContain("</dl>");
    }

    [Fact]
    public void ConvertForOneNote_DefinitionList_MultipleEntries()
    {
        var html = "<dl><dt>Term1</dt><dd>Def1</dd><dt>Term2</dt><dd>Def2</dd></dl>";
        var result = _converter.ConvertForOneNote(html);
        result.Should().Contain("Term1");
        result.Should().Contain("Def1");
        result.Should().Contain("Term2");
        result.Should().Contain("Def2");
    }

    // --- Unicode/Emoji/CJK ---

    [Fact]
    public void ConvertForOneNote_CJKContent_Preserved()
    {
        var html = "<p>\u4f60\u597d\u4e16\u754c</p>";
        var result = _converter.ConvertForOneNote(html);
        result.Should().Contain("\u4f60\u597d\u4e16\u754c");
    }

    [Fact]
    public void ConvertForOneNote_EmojiInParagraph_Preserved()
    {
        var html = "<p>Hello \U0001f600 World</p>";
        var result = _converter.ConvertForOneNote(html);
        result.Should().Contain("\U0001f600");
    }

    [Fact]
    public void ConvertForOneNote_UnicodeInCodeBlock_Preserved()
    {
        var html = "<pre><code>// \u00e9\u00e8\u00ea\u00eb</code></pre>";
        var result = _converter.ConvertForOneNote(html);
        result.Should().Contain("\u00e9\u00e8\u00ea\u00eb");
    }

    // --- ColorCode output without <code> wrapper (Gap 1) ---

    [Fact]
    public void ConvertForOneNote_ColorCodeWithoutCode_RendersAsTable()
    {
        // ColorCode produces <pre>...</pre> without <code> for recognized languages
        var html =
            "<div style=\"margin:8px 0;\">" +
            "<div style=\"background:#2d2d2d;color:#858585;padding:4px 12px;font-size:11px;" +
            "font-family:Consolas,'Courier New',monospace;border-radius:4px 4px 0 0;\">C#</div>" +
            "<div style=\"color:#DADADA;background-color:#1E1E1E;\">" +
            "<pre><span style=\"color:#569CD6\">public</span> <span style=\"color:#4EC9B0\">void</span> Test()</pre>" +
            "</div></div>";
        var result = _converter.ConvertForOneNote(html);
        result.Should().Contain("<table");
        result.Should().Contain("Consolas");
        result.Should().NotContain("<pre>");
        result.Should().NotContain("</pre>");
    }

    [Fact]
    public void ConvertForOneNote_ColorCodeWithCode_StillWorks()
    {
        // Fallback format includes <code> — should still match
        var html =
            "<div style=\"margin:8px 0;\">" +
            "<div style=\"color:#DADADA;background-color:#1E1E1E;\">" +
            "<pre style=\"margin:0;padding:12px;overflow-x:auto;\">" +
            "<code style=\"font-family:Consolas;font-size:13px;\">var x = 1;</code>" +
            "</pre></div></div>";
        var result = _converter.ConvertForOneNote(html);
        result.Should().Contain("<table");
        result.Should().NotContain("<pre>");
        result.Should().NotContain("<code>");
    }

    // --- Bare <pre> handler (Gap 1b) ---

    [Fact]
    public void ConvertForOneNote_BarePre_RendersAsTable()
    {
        var html = "<pre>some preformatted text</pre>";
        var result = _converter.ConvertForOneNote(html);
        result.Should().Contain("<table");
        result.Should().NotContain("<pre>");
    }

    // --- Whitelist tag sanitizer (Gap 2) ---

    [Fact]
    public void ConvertForOneNote_ColgroupCol_Stripped()
    {
        var html = "<table><colgroup><col/><col/></colgroup><tr><td>Cell</td></tr></table>";
        var result = _converter.ConvertForOneNote(html);
        result.Should().NotContain("<colgroup");
        result.Should().NotContain("<col");
        result.Should().Contain("<table");
        result.Should().Contain("Cell");
    }

    [Fact]
    public void ConvertForOneNote_AbbrTag_Unwrapped()
    {
        var html = "<p>The <abbr title=\"HyperText Markup Language\">HTML</abbr> spec</p>";
        var result = _converter.ConvertForOneNote(html);
        result.Should().NotContain("<abbr");
        result.Should().Contain("HTML");
        result.Should().Contain("spec");
    }

    [Fact]
    public void ConvertForOneNote_FigureTag_ContentPreserved()
    {
        var html = "<figure><img src=\"test.png\"/><figcaption>A caption</figcaption></figure>";
        var result = _converter.ConvertForOneNote(html);
        result.Should().NotContain("<figure");
        result.Should().NotContain("<figcaption");
        result.Should().Contain("<img");
        result.Should().Contain("A caption");
    }

    [Fact]
    public void ConvertForOneNote_SectionFooter_Unwrapped()
    {
        var html = "<section><p>Content</p></section><footer><p>Footer</p></footer>";
        var result = _converter.ConvertForOneNote(html);
        result.Should().NotContain("<section");
        result.Should().NotContain("<footer");
        result.Should().Contain("<p>Content</p>");
        result.Should().Contain("<p>Footer</p>");
    }

    [Fact]
    public void ConvertForOneNote_InsTag_Unwrapped()
    {
        var html = "<p><ins>inserted text</ins></p>";
        var result = _converter.ConvertForOneNote(html);
        result.Should().NotContain("<ins");
        result.Should().Contain("inserted text");
    }

    [Fact]
    public void ConvertForOneNote_AllowedTagsPreserved()
    {
        var html = "<p><strong>bold</strong> <em>italic</em> <span>span</span> <a href=\"#\">link</a></p>" +
                   "<ul><li>item</li></ul><ol><li>item</li></ol>" +
                   "<div>div</div><b>b</b><i>i</i><u>u</u><del>del</del><sup>sup</sup><sub>sub</sub><cite>cite</cite>";
        var result = _converter.ConvertForOneNote(html);
        result.Should().Contain("<strong>");
        result.Should().Contain("<em>");
        result.Should().Contain("<span>");
        result.Should().Contain("<a href=");
        result.Should().Contain("<ul>");
        result.Should().Contain("<li>");
        result.Should().Contain("<ol>");
        result.Should().Contain("<div>");
        result.Should().Contain("<b>");
        result.Should().Contain("<i>");
        result.Should().Contain("<u>");
        result.Should().Contain("<del>");
        result.Should().Contain("<sup>");
        result.Should().Contain("<sub>");
        result.Should().Contain("<cite>");
    }

    [Fact]
    public void ConvertForOneNote_NestedUnsupported_ContentPreserved()
    {
        var html = "<section><article><aside><p>deep text</p></aside></article></section>";
        var result = _converter.ConvertForOneNote(html);
        result.Should().NotContain("<section");
        result.Should().NotContain("<article");
        result.Should().NotContain("<aside");
        result.Should().Contain("<p>deep text</p>");
    }
}
