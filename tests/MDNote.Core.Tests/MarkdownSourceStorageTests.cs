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
    public void CreateMetaEntries_ContainsAllKeys()
    {
        var meta = MarkdownSourceStorage.CreateMetaEntries("# Test");
        meta.Should().ContainKey("md-note-source");
        meta.Should().ContainKey("md-note-version");
        meta.Should().ContainKey("md-note-rendered");
        meta.Should().HaveCount(3);
    }

    [Fact]
    public void CreateMetaEntries_VersionIsCurrentVersion()
    {
        var meta = MarkdownSourceStorage.CreateMetaEntries("# Test");
        meta["md-note-version"].Should().Be(MarkdownSourceStorage.CurrentVersion);
    }

    [Fact]
    public void CreateMetaEntries_RenderedIsIso8601()
    {
        var meta = MarkdownSourceStorage.CreateMetaEntries("# Test");
        meta["md-note-rendered"].Should().Contain("T");
    }

    [Fact]
    public void CreateMetaEntries_SourceIsDecodable()
    {
        var markdown = "# Test\n\nParagraph with **bold**";
        var meta = MarkdownSourceStorage.CreateMetaEntries(markdown);
        var decoded = MarkdownSourceStorage.DecodeSource(meta["md-note-source"]);
        decoded.Should().Be(markdown);
    }
}
