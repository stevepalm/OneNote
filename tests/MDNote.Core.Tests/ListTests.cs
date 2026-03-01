using FluentAssertions;
using MDNote.Core;
using Xunit;

namespace MDNote.Core.Tests;

public class ListTests
{
    private readonly IMarkdownConverter _converter = new MarkdownConverter();

    [Fact]
    public void Convert_UnorderedList_ProducesUlLi()
    {
        var md = "- Item A\n- Item B\n- Item C";
        var result = _converter.Convert(md);

        result.Html.Should().Contain("<ul>");
        result.Html.Should().Contain("<li>Item A</li>");
        result.Html.Should().Contain("<li>Item B</li>");
        result.Html.Should().Contain("<li>Item C</li>");
    }

    [Fact]
    public void Convert_OrderedList_ProducesOlLi()
    {
        var md = "1. First\n2. Second\n3. Third";
        var result = _converter.Convert(md);

        result.Html.Should().Contain("<ol>");
        result.Html.Should().Contain("<li>First</li>");
        result.Html.Should().Contain("<li>Second</li>");
    }

    [Fact]
    public void Convert_NestedList_ThreeLevels()
    {
        var md = "- Level 1\n  - Level 2\n    - Level 3";
        var result = _converter.Convert(md);

        result.Html.Should().Contain("<ul>");
        result.Html.Should().Contain("Level 1");
        result.Html.Should().Contain("Level 2");
        result.Html.Should().Contain("Level 3");

        // Should have nested <ul> elements
        var ulCount = System.Text.RegularExpressions.Regex.Matches(result.Html, "<ul>").Count;
        ulCount.Should().BeGreaterThanOrEqualTo(3);
    }

    [Fact]
    public void Convert_TaskList_ProducesCheckboxes()
    {
        var md = "- [x] Done task\n- [ ] Pending task";
        var result = _converter.Convert(md);

        result.Html.Should().Contain("type=\"checkbox\"");
        result.Html.Should().Contain("checked");
        result.Html.Should().Contain("Done task");
        result.Html.Should().Contain("Pending task");
    }

    [Fact]
    public void Convert_MixedList_OrderedWithNested()
    {
        var md = "1. First\n   - Sub A\n   - Sub B\n2. Second";
        var result = _converter.Convert(md);

        result.Html.Should().Contain("<ol>");
        result.Html.Should().Contain("<ul>");
        result.Html.Should().Contain("First");
        result.Html.Should().Contain("Sub A");
    }
}
