using FluentAssertions;
using MDNote.Core;
using Xunit;

namespace MDNote.Core.Tests;

public class MathRenderingTests
{
    private readonly IMarkdownConverter _converter = new MarkdownConverter();

    [Fact]
    public void Convert_InlineMath_StyledAsSerif()
    {
        var result = _converter.Convert("The equation $E=mc^2$ is famous.");

        result.Html.Should().Contain("Cambria Math");
        result.Html.Should().Contain("font-style:italic");
        result.Html.Should().Contain("E=mc^2");
    }

    [Fact]
    public void Convert_BlockMath_CenteredSerif()
    {
        var result = _converter.Convert("$$\nx = \\frac{-b \\pm \\sqrt{b^2 - 4ac}}{2a}\n$$");

        result.Html.Should().Contain("text-align:center");
        result.Html.Should().Contain("Cambria Math");
    }

    [Fact]
    public void Convert_InlineMath_NoClassAttribute()
    {
        var result = _converter.Convert("Math: $x+y$");

        // class="math" should be replaced with inline style
        result.Html.Should().NotContain("class=\"math\"");
    }

    [Fact]
    public void Convert_MalformedMath_DoesNotThrow()
    {
        // Unmatched dollar sign shouldn't crash
        var result = _converter.Convert("Price is $5 and $10 respectively.");

        result.Html.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Convert_NoMath_UnchangedOutput()
    {
        var result = _converter.Convert("No math here, just text.");

        result.Html.Should().Contain("No math here");
        result.Html.Should().NotContain("Cambria Math");
    }
}
