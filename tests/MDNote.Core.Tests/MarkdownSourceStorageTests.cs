using FluentAssertions;
using Xunit;

namespace MDNote.Core.Tests;

public class MarkdownSourceStorageTests
{
    [Fact]
    public void EncodeSource_SimpleMarkdown_RoundTrips()
    {
        var original = "# Hello\n\nWorld";
        var encoded = MarkdownSourceStorage.EncodeSource(original);
        var decoded = MarkdownSourceStorage.DecodeSource(encoded);
        decoded.Should().Be(original);
    }

    [Fact]
    public void EncodeSource_Null_ReturnsEmpty()
    {
        MarkdownSourceStorage.EncodeSource(null).Should().BeEmpty();
    }

    [Fact]
    public void EncodeSource_Empty_ReturnsEmpty()
    {
        MarkdownSourceStorage.EncodeSource("").Should().BeEmpty();
    }

    [Fact]
    public void DecodeSource_Null_ReturnsEmpty()
    {
        MarkdownSourceStorage.DecodeSource(null).Should().BeEmpty();
    }

    [Fact]
    public void DecodeSource_Empty_ReturnsEmpty()
    {
        MarkdownSourceStorage.DecodeSource("").Should().BeEmpty();
    }

    [Fact]
    public void EncodeSource_UnicodeContent_RoundTrips()
    {
        var original = "## Umlaut: \u00fc\u00f6\u00e4  Emoji: \ud83d\ude80  CJK: \u4f60\u597d";
        var decoded = MarkdownSourceStorage.DecodeSource(
            MarkdownSourceStorage.EncodeSource(original));
        decoded.Should().Be(original);
    }

    [Fact]
    public void EncodeSource_LargeContent_RoundTrips()
    {
        var original = new string('x', 100_000);
        var decoded = MarkdownSourceStorage.DecodeSource(
            MarkdownSourceStorage.EncodeSource(original));
        decoded.Should().Be(original);
    }

    [Fact]
    public void EncodeSource_SpecialCharacters_RoundTrips()
    {
        var original = "Line1\r\nLine2\n\tTabbed";
        var decoded = MarkdownSourceStorage.DecodeSource(
            MarkdownSourceStorage.EncodeSource(original));
        decoded.Should().Be(original);
    }

    [Fact]
    public void EncodeSource_ProducesValidBase64()
    {
        var encoded = MarkdownSourceStorage.EncodeSource("# Test");
        encoded.Should().MatchRegex(@"^[A-Za-z0-9+/=]+$");
    }

    [Fact]
    public void BuildHiddenSourceHtml_ContainsEncodedData()
    {
        var encoded = MarkdownSourceStorage.EncodeSource("# Test");
        var html = MarkdownSourceStorage.BuildHiddenSourceHtml(encoded);
        html.Should().Contain("data-md-source=");
        html.Should().Contain("display:none");
        html.Should().Contain(encoded);
    }

    [Fact]
    public void ExtractHiddenSource_RoundTrips()
    {
        var markdown = "# Test\n\nParagraph with **bold**";
        var encoded = MarkdownSourceStorage.EncodeSource(markdown);
        var html = MarkdownSourceStorage.BuildHiddenSourceHtml(encoded);
        var extracted = MarkdownSourceStorage.ExtractHiddenSource(html);
        extracted.Should().Be(encoded);
        MarkdownSourceStorage.DecodeSource(extracted).Should().Be(markdown);
    }

    [Fact]
    public void ExtractHiddenSource_Null_ReturnsNull()
    {
        MarkdownSourceStorage.ExtractHiddenSource(null).Should().BeNull();
    }

    [Fact]
    public void ExtractHiddenSource_NoTag_ReturnsNull()
    {
        MarkdownSourceStorage.ExtractHiddenSource("<p>Hello</p>").Should().BeNull();
    }

    [Fact]
    public void StripHiddenSource_RemovesTag()
    {
        var encoded = MarkdownSourceStorage.EncodeSource("# Test");
        var html = "<p>Content</p>" + MarkdownSourceStorage.BuildHiddenSourceHtml(encoded);
        var stripped = MarkdownSourceStorage.StripHiddenSource(html);
        stripped.Should().Be("<p>Content</p>");
    }
}
