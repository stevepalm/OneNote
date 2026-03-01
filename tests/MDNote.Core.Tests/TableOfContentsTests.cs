using FluentAssertions;
using MDNote.Core;
using MDNote.Core.Models;
using Xunit;

namespace MDNote.Core.Tests;

public class TableOfContentsTests
{
    private readonly IMarkdownConverter _converter = new MarkdownConverter();

    [Fact]
    public void Convert_TocMarker_GeneratesToc()
    {
        var md = "[TOC]\n\n# First\n## Second\n### Third";
        var result = _converter.Convert(md);

        result.Html.Should().Contain("Table of Contents");
        result.Html.Should().Contain("<ul");
        result.Html.Should().Contain("First");
        result.Html.Should().Contain("Second");
        result.Html.Should().Contain("Third");
    }

    [Fact]
    public void Convert_HtmlTocComment_GeneratesToc()
    {
        var md = "<!-- toc -->\n\n# Heading One\n## Heading Two";
        var result = _converter.Convert(md);

        result.Html.Should().Contain("Table of Contents");
    }

    [Fact]
    public void Convert_TocDisabledAndNoMarker_NoToc()
    {
        var md = "# First\n## Second";
        var options = new ConversionOptions { EnableTableOfContents = false };
        var result = _converter.Convert(md, options);

        result.Html.Should().NotContain("Table of Contents");
    }

    [Fact]
    public void Convert_TocEnabledViaOptions_GeneratesToc()
    {
        var md = "# First\n## Second";
        var options = new ConversionOptions { EnableTableOfContents = true };
        var result = _converter.Convert(md, options);

        result.Html.Should().Contain("Table of Contents");
    }

    [Fact]
    public void Convert_TocWithNestedHeadings_ProducesNestedList()
    {
        var md = "[TOC]\n\n# H1\n## H2a\n### H3\n## H2b";
        var result = _converter.Convert(md);

        // Should have nested <ul> for the hierarchy
        var ulCount = System.Text.RegularExpressions.Regex.Matches(result.Html, "<ul").Count;
        ulCount.Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public void Convert_TocLinks_HaveHrefAnchors()
    {
        var md = "[TOC]\n\n# My Heading";
        var result = _converter.Convert(md);

        result.Html.Should().Contain("href=\"#");
    }
}
