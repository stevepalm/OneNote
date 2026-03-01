using FluentAssertions;
using MDNote.Core;
using Xunit;

namespace MDNote.Core.Tests;

public class MermaidTests
{
    private readonly IMarkdownConverter _converter = new MarkdownConverter();

    [Fact]
    public void Convert_MermaidBlock_ExtractedFromResult()
    {
        var md = "# Title\n\n```mermaid\ngraph TD;\n  A-->B;\n```\n\nMore text.";
        var result = _converter.Convert(md);

        result.MermaidBlocks.Should().HaveCount(1);
        result.MermaidBlocks[0].Id.Should().Be("mermaid-0");
        result.MermaidBlocks[0].Definition.Should().Contain("graph TD");
        result.MermaidBlocks[0].Definition.Should().Contain("A-->B");
    }

    [Fact]
    public void Convert_MermaidBlock_ReplacedWithPlaceholder()
    {
        var md = "```mermaid\ngraph LR;\n  A-->B;\n```";
        var result = _converter.Convert(md);

        result.Html.Should().Contain("Mermaid diagram");
        result.Html.Should().NotContain("graph LR");
    }

    [Fact]
    public void Convert_MultipleMermaidBlocks_AllExtracted()
    {
        var md = "```mermaid\ngraph TD;\n  A-->B;\n```\n\nText\n\n```mermaid\nsequenceDiagram\n  A->>B: Hello\n```";
        var result = _converter.Convert(md);

        result.MermaidBlocks.Should().HaveCount(2);
        result.MermaidBlocks[0].Id.Should().Be("mermaid-0");
        result.MermaidBlocks[1].Id.Should().Be("mermaid-1");
    }

    [Fact]
    public void Convert_NoMermaid_EmptyList()
    {
        var result = _converter.Convert("# Just text\n\nNo diagrams here.");

        result.MermaidBlocks.Should().BeEmpty();
    }

    [Fact]
    public void Convert_MermaidCaseInsensitive_Extracted()
    {
        var md = "```Mermaid\ngraph TD;\n  A-->B;\n```";
        var result = _converter.Convert(md);

        result.MermaidBlocks.Should().HaveCount(1);
    }
}
