using FluentAssertions;
using Xunit;

namespace MDNote.Core.Tests;

public class MarkdownSourceStorageTests
{
    // --- Legacy encode/decode (still used for backward compat) ---

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

    // --- GZip compression ---

    [Fact]
    public void CompressSource_SimpleMarkdown_RoundTrips()
    {
        var original = "# Hello\n\nWorld";
        var compressed = MarkdownSourceStorage.CompressSource(original);
        var decompressed = MarkdownSourceStorage.DecompressSource(compressed);
        decompressed.Should().Be(original);
    }

    [Fact]
    public void CompressSource_LargeContent_SmallerThanBase64()
    {
        // 100KB of repetitive markdown (representative of real documents)
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < 2000; i++)
            sb.AppendLine($"## Section {i}\nParagraph with **bold** and `code`.");
        var original = sb.ToString();

        var uncompressed = MarkdownSourceStorage.EncodeSource(original);
        var compressed = MarkdownSourceStorage.CompressSource(original);

        compressed.Length.Should().BeLessThan(uncompressed.Length / 2,
            "GZip should achieve at least 50% compression on repetitive markdown");
    }

    [Fact]
    public void CompressSource_Null_ReturnsEmpty()
    {
        MarkdownSourceStorage.CompressSource(null).Should().BeEmpty();
    }

    [Fact]
    public void CompressSource_Empty_ReturnsEmpty()
    {
        MarkdownSourceStorage.CompressSource("").Should().BeEmpty();
    }

    [Fact]
    public void DecompressSource_Null_ReturnsEmpty()
    {
        MarkdownSourceStorage.DecompressSource(null).Should().BeEmpty();
    }

    [Fact]
    public void DecompressSource_Empty_ReturnsEmpty()
    {
        MarkdownSourceStorage.DecompressSource("").Should().BeEmpty();
    }

    [Fact]
    public void CompressSource_UnicodeContent_RoundTrips()
    {
        var original = "## Umlaut: \u00fc\u00f6\u00e4  Emoji: \ud83d\ude80  CJK: \u4f60\u597d";
        var decompressed = MarkdownSourceStorage.DecompressSource(
            MarkdownSourceStorage.CompressSource(original));
        decompressed.Should().Be(original);
    }

    [Fact]
    public void CompressSource_LargeContent_RoundTrips()
    {
        var original = new string('x', 100_000);
        var decompressed = MarkdownSourceStorage.DecompressSource(
            MarkdownSourceStorage.CompressSource(original));
        decompressed.Should().Be(original);
    }

    // --- BuildHiddenSourceHtml (now accepts raw markdown, compresses internally) ---

    [Fact]
    public void BuildHiddenSourceHtml_UsesCompressedPrefix()
    {
        var html = MarkdownSourceStorage.BuildHiddenSourceHtml("# Test");
        html.Should().Contain("title=\"mdsrc-gz:");
        html.Should().Contain("display:none");
    }

    [Fact]
    public void BuildHiddenSourceHtml_Null_ReturnsEmpty()
    {
        MarkdownSourceStorage.BuildHiddenSourceHtml(null).Should().BeEmpty();
    }

    [Fact]
    public void BuildHiddenSourceHtml_Empty_ReturnsEmpty()
    {
        MarkdownSourceStorage.BuildHiddenSourceHtml("").Should().BeEmpty();
    }

    // --- ExtractMarkdownSource (handles all 3 formats) ---

    [Fact]
    public void ExtractMarkdownSource_CompressedFormat_ReturnsMarkdown()
    {
        var markdown = "# Test\n\n**Bold** paragraph";
        var html = MarkdownSourceStorage.BuildHiddenSourceHtml(markdown);
        var extracted = MarkdownSourceStorage.ExtractMarkdownSource(html);
        extracted.Should().Be(markdown);
    }

    [Fact]
    public void ExtractMarkdownSource_UncompressedFormat_ReturnsMarkdown()
    {
        // Old format: mdsrc: prefix with plain Base64
        var encoded = MarkdownSourceStorage.EncodeSource("# Legacy");
        var html = "<span title=\"mdsrc:" + encoded + "\" style=\"display:none\"></span>";
        var extracted = MarkdownSourceStorage.ExtractMarkdownSource(html);
        extracted.Should().Be("# Legacy");
    }

    [Fact]
    public void ExtractMarkdownSource_LegacyDataAttribute_ReturnsMarkdown()
    {
        var encoded = MarkdownSourceStorage.EncodeSource("# Very Legacy");
        var html = "<span data-md-source=\"" + encoded + "\" style=\"display:none\"></span>";
        var extracted = MarkdownSourceStorage.ExtractMarkdownSource(html);
        extracted.Should().Be("# Very Legacy");
    }

    [Fact]
    public void ExtractMarkdownSource_Null_ReturnsNull()
    {
        MarkdownSourceStorage.ExtractMarkdownSource(null).Should().BeNull();
    }

    [Fact]
    public void ExtractMarkdownSource_NoTag_ReturnsNull()
    {
        MarkdownSourceStorage.ExtractMarkdownSource("<p>Hello</p>").Should().BeNull();
    }

    [Fact]
    public void ExtractMarkdownSource_LargeDocument_RoundTrips()
    {
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < 200; i++)
            sb.AppendLine($"## Section {i}\nContent with `code` and **bold**.");
        var markdown = sb.ToString();

        var html = MarkdownSourceStorage.BuildHiddenSourceHtml(markdown);
        var extracted = MarkdownSourceStorage.ExtractMarkdownSource(html);
        extracted.Should().Be(markdown);
    }

    // --- Legacy ExtractHiddenSource (backward compat) ---

#pragma warning disable CS0612, CS0618 // Suppress obsolete warnings for legacy API tests
    [Fact]
    public void ExtractHiddenSource_UncompressedFormat_StillWorks()
    {
        var encoded = MarkdownSourceStorage.EncodeSource("# Test");
        var html = "<span title=\"mdsrc:" + encoded + "\" style=\"display:none\"></span>";
        var extracted = MarkdownSourceStorage.ExtractHiddenSource(html);
        extracted.Should().Be(encoded);
        MarkdownSourceStorage.DecodeSource(extracted).Should().Be("# Test");
    }

    [Fact]
    public void ExtractHiddenSource_LegacyFormat_StillWorks()
    {
        var encoded = MarkdownSourceStorage.EncodeSource("# Legacy");
        var legacyHtml = "<span data-md-source=\"" + encoded + "\" style=\"display:none\"></span>";
        var extracted = MarkdownSourceStorage.ExtractHiddenSource(legacyHtml);
        extracted.Should().Be(encoded);
        MarkdownSourceStorage.DecodeSource(extracted).Should().Be("# Legacy");
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
#pragma warning restore CS0612, CS0618

    // --- StripHiddenSource ---

    [Fact]
    public void StripHiddenSource_RemovesCompressedFormat()
    {
        var sourceHtml = MarkdownSourceStorage.BuildHiddenSourceHtml("# Test");
        var fullHtml = "<p>Content</p>" + sourceHtml;
        var stripped = MarkdownSourceStorage.StripHiddenSource(fullHtml);
        stripped.Should().Be("<p>Content</p>");
    }

    [Fact]
    public void StripHiddenSource_RemovesUncompressedFormat()
    {
        var encoded = MarkdownSourceStorage.EncodeSource("# Test");
        var html = "<p>Content</p><span title=\"mdsrc:" + encoded + "\" style=\"display:none\"></span>";
        var stripped = MarkdownSourceStorage.StripHiddenSource(html);
        stripped.Should().Be("<p>Content</p>");
    }

    [Fact]
    public void StripHiddenSource_RemovesLegacyFormat()
    {
        var encoded = MarkdownSourceStorage.EncodeSource("# Legacy");
        var legacyHtml = "<p>Content</p><span data-md-source=\"" + encoded + "\" style=\"display:none\"></span>";
        var stripped = MarkdownSourceStorage.StripHiddenSource(legacyHtml);
        stripped.Should().Be("<p>Content</p>");
    }
}
