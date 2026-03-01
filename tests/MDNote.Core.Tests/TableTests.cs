using FluentAssertions;
using MDNote.Core;
using Xunit;

namespace MDNote.Core.Tests;

public class TableTests
{
    private readonly IMarkdownConverter _converter = new MarkdownConverter();

    [Fact]
    public void Convert_SimpleTable_ProducesTableHtml()
    {
        var md = "| Name | Age |\n|------|-----|\n| Alice | 30 |\n| Bob | 25 |";
        var result = _converter.Convert(md);

        result.Html.Should().Contain("<table");
        result.Html.Should().Contain("<th>Name</th>");
        result.Html.Should().Contain("<th>Age</th>");
        result.Html.Should().Contain("<td>Alice</td>");
        result.Html.Should().Contain("<td>Bob</td>");
    }

    [Fact]
    public void Convert_TableWithAlignment_HasAlignAttributes()
    {
        var md = "| Left | Center | Right |\n|:-----|:------:|------:|\n| a | b | c |";
        var result = _converter.Convert(md);

        result.Html.Should().Contain("<table");
        // Markdig uses style="text-align: ..." for alignment
        result.Html.Should().Contain("text-align");
    }

    [Fact]
    public void Convert_TableWithHeaders_HasThead()
    {
        var md = "| H1 | H2 |\n|----|----|\n| d1 | d2 |";
        var result = _converter.Convert(md);

        result.Html.Should().Contain("<thead>");
        result.Html.Should().Contain("<tbody>");
    }
}
