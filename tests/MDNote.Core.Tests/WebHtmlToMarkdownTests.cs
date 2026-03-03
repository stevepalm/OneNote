using FluentAssertions;
using Xunit;

namespace MDNote.Core.Tests;

public class WebHtmlToMarkdownTests
{
    private readonly WebHtmlToMarkdown _converter = new WebHtmlToMarkdown();

    // --- Null / Empty ---

    [Fact]
    public void Convert_Null_ReturnsEmpty()
    {
        _converter.Convert(null).Should().BeEmpty();
    }

    [Fact]
    public void Convert_Empty_ReturnsEmpty()
    {
        _converter.Convert("").Should().BeEmpty();
    }

    [Fact]
    public void Convert_PlainText_ReturnsText()
    {
        _converter.Convert("Hello world").Should().Be("Hello world");
    }

    // --- Headings ---

    [Theory]
    [InlineData("<h1>Title</h1>", "# Title")]
    [InlineData("<h2>Section</h2>", "## Section")]
    [InlineData("<h3>Subsection</h3>", "### Subsection")]
    [InlineData("<h4>Detail</h4>", "#### Detail")]
    [InlineData("<h5>Minor</h5>", "##### Minor")]
    [InlineData("<h6>Smallest</h6>", "###### Smallest")]
    public void Convert_Headings_ProducesCorrectLevel(string html, string expected)
    {
        _converter.Convert(html).Should().Contain(expected);
    }

    [Fact]
    public void Convert_HeadingWithAttributes_StripsAttributes()
    {
        var html = "<h2 id=\"intro\" class=\"section-title\">Introduction</h2>";
        _converter.Convert(html).Should().Contain("## Introduction");
    }

    [Fact]
    public void Convert_HeadingWithInlineFormatting_PreservesFormatting()
    {
        var html = "<h2>This is <strong>important</strong></h2>";
        _converter.Convert(html).Should().Contain("## This is **important**");
    }

    // --- Bold / Italic / Strikethrough ---

    [Theory]
    [InlineData("<strong>bold</strong>", "**bold**")]
    [InlineData("<b>bold</b>", "**bold**")]
    public void Convert_Bold_ProducesDoubleStar(string html, string expected)
    {
        _converter.Convert(html).Should().Contain(expected);
    }

    [Theory]
    [InlineData("<em>italic</em>", "*italic*")]
    [InlineData("<i>italic</i>", "*italic*")]
    public void Convert_Italic_ProducesSingleStar(string html, string expected)
    {
        _converter.Convert(html).Should().Contain(expected);
    }

    [Theory]
    [InlineData("<del>deleted</del>", "~~deleted~~")]
    [InlineData("<s>strikethrough</s>", "~~strikethrough~~")]
    [InlineData("<strike>old</strike>", "~~old~~")]
    public void Convert_Strikethrough_ProducesTildes(string html, string expected)
    {
        _converter.Convert(html).Should().Contain(expected);
    }

    [Fact]
    public void Convert_Mark_ProducesDoubleEquals()
    {
        _converter.Convert("<mark>highlighted</mark>").Should().Contain("==highlighted==");
    }

    // --- Inline Code ---

    [Fact]
    public void Convert_InlineCode_ProducesBackticks()
    {
        _converter.Convert("<code>console.log()</code>").Should().Contain("`console.log()`");
    }

    [Fact]
    public void Convert_InlineCodeWithEntities_DecodesEntities()
    {
        _converter.Convert("<code>&lt;div&gt;</code>").Should().Contain("`<div>`");
    }

    // --- Code Blocks ---

    [Fact]
    public void Convert_PreCodeWithLanguage_ProducesFencedBlock()
    {
        var html = "<pre><code class=\"language-python\">def hello():\n    print(\"hi\")</code></pre>";
        var result = _converter.Convert(html);
        result.Should().Contain("```python");
        result.Should().Contain("def hello():");
        result.Should().Contain("```");
    }

    [Fact]
    public void Convert_PreCodeWithHljsLanguage_ExtractsLanguage()
    {
        var html = "<pre><code class=\"hljs language-javascript\">const x = 1;</code></pre>";
        var result = _converter.Convert(html);
        result.Should().Contain("```javascript");
        result.Should().Contain("const x = 1;");
    }

    [Fact]
    public void Convert_PreCodeNoLanguage_ProducesFencedBlock()
    {
        var html = "<pre><code>some code here</code></pre>";
        var result = _converter.Convert(html);
        result.Should().Contain("```\nsome code here\n```");
    }

    [Fact]
    public void Convert_BarePreTag_ProducesFencedBlock()
    {
        var html = "<pre>preformatted text</pre>";
        var result = _converter.Convert(html);
        result.Should().Contain("```\npreformatted text\n```");
    }

    [Fact]
    public void Convert_CodeBlockWithSyntaxSpans_StripsSpansKeepsText()
    {
        var html = "<pre><code class=\"language-csharp\">" +
                   "<span class=\"hljs-keyword\">var</span> x = <span class=\"hljs-number\">42</span>;" +
                   "</code></pre>";
        var result = _converter.Convert(html);
        result.Should().Contain("```csharp");
        result.Should().Contain("var x = 42;");
        result.Should().NotContain("<span");
    }

    [Fact]
    public void Convert_CodeBlockWithHtmlEntities_DecodesEntities()
    {
        var html = "<pre><code class=\"language-html\">&lt;div class=&quot;test&quot;&gt;&lt;/div&gt;</code></pre>";
        var result = _converter.Convert(html);
        result.Should().Contain("<div class=\"test\"></div>");
    }

    // --- Links ---

    [Fact]
    public void Convert_Link_ProducesMarkdownLink()
    {
        _converter.Convert("<a href=\"https://example.com\">Example</a>")
            .Should().Contain("[Example](https://example.com)");
    }

    [Fact]
    public void Convert_LinkWithNestedFormatting_StripsInnerTags()
    {
        _converter.Convert("<a href=\"https://example.com\"><strong>Bold Link</strong></a>")
            .Should().Contain("[Bold Link](https://example.com)");
    }

    // --- Images ---

    [Fact]
    public void Convert_ImageWithAlt_ProducesMarkdownImage()
    {
        _converter.Convert("<img src=\"photo.png\" alt=\"My Photo\"/>")
            .Should().Contain("![My Photo](photo.png)");
    }

    [Fact]
    public void Convert_ImageNoAlt_ProducesEmptyAlt()
    {
        _converter.Convert("<img src=\"photo.png\"/>")
            .Should().Contain("![](photo.png)");
    }

    // --- Lists ---

    [Fact]
    public void Convert_UnorderedList_ProducesDashItems()
    {
        var html = "<ul><li>Apple</li><li>Banana</li><li>Cherry</li></ul>";
        var result = _converter.Convert(html);
        result.Should().Contain("- Apple");
        result.Should().Contain("- Banana");
        result.Should().Contain("- Cherry");
    }

    [Fact]
    public void Convert_OrderedList_ProducesNumberedItems()
    {
        var html = "<ol><li>First</li><li>Second</li><li>Third</li></ol>";
        var result = _converter.Convert(html);
        result.Should().Contain("1. First");
        result.Should().Contain("2. Second");
        result.Should().Contain("3. Third");
    }

    [Fact]
    public void Convert_TaskListChecked_ProducesCheckedBox()
    {
        var html = "<ul><li><input type=\"checkbox\" checked/> Done</li></ul>";
        var result = _converter.Convert(html);
        result.Should().Contain("- [x] Done");
    }

    [Fact]
    public void Convert_TaskListUnchecked_ProducesUncheckedBox()
    {
        var html = "<ul><li><input type=\"checkbox\"/> Not done</li></ul>";
        var result = _converter.Convert(html);
        result.Should().Contain("- [ ] Not done");
    }

    // --- Tables ---

    [Fact]
    public void Convert_SimpleTable_ProducesMarkdownTable()
    {
        var html = "<table><tr><th>Name</th><th>Age</th></tr>" +
                   "<tr><td>Alice</td><td>30</td></tr>" +
                   "<tr><td>Bob</td><td>25</td></tr></table>";
        var result = _converter.Convert(html);
        result.Should().Contain("| Name | Age |");
        result.Should().Contain("| --- | --- |");
        result.Should().Contain("| Alice | 30 |");
        result.Should().Contain("| Bob | 25 |");
    }

    [Fact]
    public void Convert_TableWithThead_HandlesCorrectly()
    {
        var html = "<table><thead><tr><th>Col1</th><th>Col2</th></tr></thead>" +
                   "<tbody><tr><td>A</td><td>B</td></tr></tbody></table>";
        var result = _converter.Convert(html);
        result.Should().Contain("| Col1 | Col2 |");
        result.Should().Contain("| A | B |");
    }

    // --- Blockquotes ---

    [Fact]
    public void Convert_Blockquote_ProducesGreaterThan()
    {
        var html = "<blockquote><p>This is a quote.</p></blockquote>";
        var result = _converter.Convert(html);
        result.Should().Contain("> This is a quote.");
    }

    [Fact]
    public void Convert_NestedBlockquote_ProducesNestedMarkers()
    {
        var html = "<blockquote><p>Outer</p><blockquote><p>Inner</p></blockquote></blockquote>";
        var result = _converter.Convert(html);
        result.Should().Contain("> ");
        result.Should().Contain("> > Inner");
    }

    // --- Horizontal Rule ---

    [Fact]
    public void Convert_HrTag_ProducesThreeDashes()
    {
        _converter.Convert("<hr/>").Should().Contain("---");
    }

    [Fact]
    public void Convert_HrTagNoSlash_ProducesThreeDashes()
    {
        _converter.Convert("<hr>").Should().Contain("---");
    }

    // --- Paragraphs and Line Breaks ---

    [Fact]
    public void Convert_Paragraphs_SeparatedByBlankLines()
    {
        var html = "<p>First paragraph.</p><p>Second paragraph.</p>";
        var result = _converter.Convert(html);
        result.Should().Contain("First paragraph.");
        result.Should().Contain("Second paragraph.");
        // Should have some separation between paragraphs
        result.Should().Contain("\n");
    }

    [Fact]
    public void Convert_BrTag_ProducesNewline()
    {
        var html = "<p>Line one<br/>Line two</p>";
        var result = _converter.Convert(html);
        result.Should().Contain("Line one\nLine two");
    }

    // --- Container Stripping ---

    [Fact]
    public void Convert_DivAndSpan_StrippedKeepContent()
    {
        var html = "<div><span>Hello</span> <span>World</span></div>";
        var result = _converter.Convert(html);
        result.Should().Contain("Hello");
        result.Should().Contain("World");
        result.Should().NotContain("<div>");
        result.Should().NotContain("<span>");
    }

    [Fact]
    public void Convert_SectionAndArticle_StrippedKeepContent()
    {
        var html = "<section><article><p>Content</p></article></section>";
        var result = _converter.Convert(html);
        result.Should().Contain("Content");
        result.Should().NotContain("<section>");
        result.Should().NotContain("<article>");
    }

    // --- Script/Style Stripping ---

    [Fact]
    public void Convert_ScriptTag_StrippedEntirely()
    {
        var html = "<p>Before</p><script>alert('xss')</script><p>After</p>";
        var result = _converter.Convert(html);
        result.Should().Contain("Before");
        result.Should().Contain("After");
        result.Should().NotContain("alert");
        result.Should().NotContain("script");
    }

    [Fact]
    public void Convert_StyleTag_StrippedEntirely()
    {
        var html = "<style>.red { color: red; }</style><p>Content</p>";
        var result = _converter.Convert(html);
        result.Should().Contain("Content");
        result.Should().NotContain("color: red");
    }

    // --- HTML Entity Decoding ---

    [Fact]
    public void Convert_HtmlEntities_Decoded()
    {
        _converter.Convert("<p>A &amp; B &lt; C &gt; D</p>")
            .Should().Contain("A & B < C > D");
    }

    [Fact]
    public void Convert_NonBreakingSpace_DecodedToSpace()
    {
        var result = _converter.Convert("<p>Hello&nbsp;World</p>");
        // \u00A0 or regular space — just verify content is there
        result.Should().Contain("Hello");
        result.Should().Contain("World");
    }

    // --- Mixed Content / Realistic ---

    [Fact]
    public void Convert_ClaudeAiLikeResponse_ProducesCleanMarkdown()
    {
        var html =
            "<h2>Why the system got slow</h2>" +
            "<p>The slowness came from <strong>architectural</strong> root causes, not just code quality.</p>" +
            "<h3>PostgreSQL with proper indexing</h3>" +
            "<p>Every query goes through <a href=\"https://docs.microsoft.com\">IQueryable</a> " +
            "with <code>.Select()</code> projection.</p>" +
            "<pre><code class=\"language-csharp\">var results = await context.Users\n" +
            "    .Where(u => u.IsActive)\n" +
            "    .Select(u => new { u.Name, u.Email })\n" +
            "    .ToListAsync();</code></pre>" +
            "<h3>Key benefits</h3>" +
            "<ul>" +
            "<li>Async everywhere eliminates thread pool exhaustion</li>" +
            "<li>Connection pooling with <strong>Npgsql</strong></li>" +
            "<li>Schema isolation keeps queries simple</li>" +
            "</ul>";

        var result = _converter.Convert(html);

        result.Should().Contain("## Why the system got slow");
        result.Should().Contain("**architectural**");
        result.Should().Contain("### PostgreSQL with proper indexing");
        result.Should().Contain("[IQueryable](https://docs.microsoft.com)");
        result.Should().Contain("`.Select()`");
        result.Should().Contain("```csharp");
        result.Should().Contain("var results = await context.Users");
        result.Should().Contain("### Key benefits");
        result.Should().Contain("- Async everywhere eliminates thread pool exhaustion");
        result.Should().Contain("**Npgsql**");
    }

    // --- Blank Line Cleanup ---

    [Fact]
    public void Convert_ExcessiveBlankLines_CollapsedToTwo()
    {
        var html = "<p>A</p>\n\n\n\n\n<p>B</p>";
        var result = _converter.Convert(html);
        result.Should().NotContain("\n\n\n");
    }

    // --- HTML Comments ---

    [Fact]
    public void Convert_HtmlComments_Stripped()
    {
        var html = "<p>Before</p><!-- This is a comment --><p>After</p>";
        var result = _converter.Convert(html);
        result.Should().Contain("Before");
        result.Should().Contain("After");
        result.Should().NotContain("comment");
    }
}
