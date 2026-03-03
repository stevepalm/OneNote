using FluentAssertions;
using Xunit;

namespace MDNote.Core.Tests;

public class CfHtmlParserTests
{
    // --- ExtractFragment ---

    [Fact]
    public void ExtractFragment_Null_ReturnsNull()
    {
        CfHtmlParser.ExtractFragment(null).Should().BeNull();
    }

    [Fact]
    public void ExtractFragment_Empty_ReturnsNull()
    {
        CfHtmlParser.ExtractFragment("").Should().BeNull();
    }

    [Fact]
    public void ExtractFragment_StandardCfHtml_ExtractsFragment()
    {
        var cfHtml =
            "Version:0.9\r\n" +
            "StartHTML:000000097\r\n" +
            "EndHTML:000000300\r\n" +
            "StartFragment:000000131\r\n" +
            "EndFragment:000000264\r\n" +
            "SourceURL:https://claude.ai/chat\r\n" +
            "<html><body>\r\n" +
            "<!--StartFragment--><h2>Hello World</h2><p>This is a test.</p><!--EndFragment-->\r\n" +
            "</body></html>";

        var result = CfHtmlParser.ExtractFragment(cfHtml);
        result.Should().Be("<h2>Hello World</h2><p>This is a test.</p>");
    }

    [Fact]
    public void ExtractFragment_WithWhitespace_TrimsResult()
    {
        var cfHtml = "<!--StartFragment-->  <p>Hello</p>  <!--EndFragment-->";
        CfHtmlParser.ExtractFragment(cfHtml).Should().Be("<p>Hello</p>");
    }

    [Fact]
    public void ExtractFragment_NoMarkers_FallsBackToBody()
    {
        var html = "<html><head><title>Test</title></head><body><p>Content</p></body></html>";
        CfHtmlParser.ExtractFragment(html).Should().Be("<p>Content</p>");
    }

    [Fact]
    public void ExtractFragment_NoMarkersNoBody_FallsBackToFirstTag()
    {
        var html = "Version:0.9\r\nStartHTML:00\r\n<p>Just content</p>";
        CfHtmlParser.ExtractFragment(html).Should().Be("<p>Just content</p>");
    }

    [Fact]
    public void ExtractFragment_PlainHtml_ExtractsBody()
    {
        var html = "<html><body><strong>Bold text</strong></body></html>";
        CfHtmlParser.ExtractFragment(html).Should().Be("<strong>Bold text</strong>");
    }

    // --- ExtractSourceUrl ---

    [Fact]
    public void ExtractSourceUrl_Null_ReturnsNull()
    {
        CfHtmlParser.ExtractSourceUrl(null).Should().BeNull();
    }

    [Fact]
    public void ExtractSourceUrl_Empty_ReturnsNull()
    {
        CfHtmlParser.ExtractSourceUrl("").Should().BeNull();
    }

    [Fact]
    public void ExtractSourceUrl_WithUrl_ExtractsIt()
    {
        var cfHtml =
            "Version:0.9\r\n" +
            "SourceURL:https://claude.ai/chat/abc123\r\n" +
            "StartFragment:000\r\n";

        CfHtmlParser.ExtractSourceUrl(cfHtml).Should().Be("https://claude.ai/chat/abc123");
    }

    [Fact]
    public void ExtractSourceUrl_NoUrl_ReturnsNull()
    {
        var cfHtml = "Version:0.9\r\nStartFragment:000\r\n";
        CfHtmlParser.ExtractSourceUrl(cfHtml).Should().BeNull();
    }

    [Fact]
    public void ExtractSourceUrl_EmptyUrl_ReturnsNull()
    {
        var cfHtml = "SourceURL:\r\nStartFragment:000\r\n";
        CfHtmlParser.ExtractSourceUrl(cfHtml).Should().BeNull();
    }
}
