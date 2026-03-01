using FluentAssertions;
using MDNote.Core;
using Xunit;

namespace MDNote.Core.Tests;

public class FrontMatterTests
{
    private readonly IMarkdownConverter _converter = new MarkdownConverter();

    [Fact]
    public void Convert_YamlFrontMatter_ParsesIntoDict()
    {
        var md = "---\ntitle: My Document\nauthor: John Doe\ndate: 2024-01-15\n---\n\n# Content";
        var result = _converter.Convert(md);

        result.FrontMatter.Should().ContainKey("title");
        result.FrontMatter["title"].Should().Be("My Document");
        result.FrontMatter.Should().ContainKey("author");
        result.FrontMatter["author"].Should().Be("John Doe");
        result.FrontMatter.Should().ContainKey("date");
    }

    [Fact]
    public void Convert_FrontMatterTitle_TakesPriorityOverH1()
    {
        var md = "---\ntitle: FM Title\n---\n\n# H1 Title";
        var result = _converter.Convert(md);

        result.Title.Should().Be("FM Title");
    }

    [Fact]
    public void Convert_NoFrontMatter_EmptyDict()
    {
        var result = _converter.Convert("# Just a heading");

        result.FrontMatter.Should().BeEmpty();
    }

    [Fact]
    public void Convert_FrontMatter_CaseInsensitiveKeys()
    {
        var md = "---\nTitle: Test\n---\n\nBody";
        var result = _converter.Convert(md);

        result.FrontMatter.Should().ContainKey("title");
        result.FrontMatter["Title"].Should().Be("Test");
    }

    [Fact]
    public void Convert_FrontMatter_NotRenderedInHtml()
    {
        var md = "---\ntitle: Hidden\n---\n\nVisible content";
        var result = _converter.Convert(md);

        result.Html.Should().NotContain("title: Hidden");
        result.Html.Should().Contain("Visible content");
    }
}
