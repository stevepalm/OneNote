using FluentAssertions;
using MDNote.Core;
using Xunit;

namespace MDNote.Core.Tests;

public class LinkAndImageTests
{
    private readonly IMarkdownConverter _converter = new MarkdownConverter();

    [Fact]
    public void Convert_InlineLink_ProducesAnchorTag()
    {
        var result = _converter.Convert("[Click here](https://example.com)");

        result.Html.Should().Contain("<a href=\"https://example.com\"");
        result.Html.Should().Contain("Click here</a>");
    }

    [Fact]
    public void Convert_LinkWithTitle_IncludesTitle()
    {
        var result = _converter.Convert("[Click](https://example.com \"A title\")");

        result.Html.Should().Contain("title=\"A title\"");
    }

    [Fact]
    public void Convert_AutoLink_ProducesLink()
    {
        var result = _converter.Convert("Visit https://example.com for info.");

        result.Html.Should().Contain("<a href=\"https://example.com\"");
    }

    [Fact]
    public void Convert_Image_ProducesImgTag()
    {
        var result = _converter.Convert("![Alt text](https://example.com/image.png)");

        result.Html.Should().Contain("<img");
        result.Html.Should().Contain("src=\"https://example.com/image.png\"");
        result.Html.Should().Contain("alt=\"Alt text\"");
    }

    [Fact]
    public void Convert_ImageWithTitle_IncludesTitle()
    {
        var result = _converter.Convert("![Alt](img.png \"My Image\")");

        result.Html.Should().Contain("title=\"My Image\"");
    }
}
