using System;
using FluentAssertions;
using Xunit;

namespace MDNote.OneNote.Tests
{
    public class PageXmlParserCdataHtmlTests
    {
        [Fact]
        public void GetOutlineCdataHtml_EmptyPage_ReturnsEmpty()
        {
            var xml =
                "<Page xmlns='http://schemas.microsoft.com/office/onenote/2013/onenote' ID='{p1}'>" +
                "  <Title><OE><T><![CDATA[Test Title]]></T></OE></Title>" +
                "</Page>";

            var parser = new PageXmlParser(xml);
            parser.GetOutlineCdataHtml().Should().BeEmpty();
        }

        [Fact]
        public void GetOutlineCdataHtml_SingleOutline_ReturnsHtml()
        {
            var xml =
                "<Page xmlns='http://schemas.microsoft.com/office/onenote/2013/onenote' ID='{p1}'>" +
                "  <Outline>" +
                "    <OEChildren>" +
                "      <OE><T><![CDATA[<b>Hello</b> World]]></T></OE>" +
                "    </OEChildren>" +
                "  </Outline>" +
                "</Page>";

            var parser = new PageXmlParser(xml);
            var result = parser.GetOutlineCdataHtml();
            result.Should().Contain("<b>Hello</b> World");
        }

        [Fact]
        public void GetOutlineCdataHtml_MultipleOutlines_ConcatenatesAll()
        {
            var xml =
                "<Page xmlns='http://schemas.microsoft.com/office/onenote/2013/onenote' ID='{p1}'>" +
                "  <Outline>" +
                "    <OEChildren>" +
                "      <OE><T><![CDATA[<p>First</p>]]></T></OE>" +
                "      <OE><T><![CDATA[<p>Second</p>]]></T></OE>" +
                "    </OEChildren>" +
                "  </Outline>" +
                "  <Outline>" +
                "    <OEChildren>" +
                "      <OE><T><![CDATA[<p>Third</p>]]></T></OE>" +
                "    </OEChildren>" +
                "  </Outline>" +
                "</Page>";

            var parser = new PageXmlParser(xml);
            var result = parser.GetOutlineCdataHtml();
            result.Should().Contain("<p>First</p>");
            result.Should().Contain("<p>Second</p>");
            result.Should().Contain("<p>Third</p>");
        }

        [Fact]
        public void GetOutlineCdataHtml_SkipsRenderedMarker()
        {
            var xml =
                "<Page xmlns='http://schemas.microsoft.com/office/onenote/2013/onenote' ID='{p1}'>" +
                "  <Outline>" +
                "    <OEChildren>" +
                "      <OE><T><![CDATA[<span style=\"display:none\">md-note-rendered</span><b>Content</b>]]></T></OE>" +
                "    </OEChildren>" +
                "  </Outline>" +
                "</Page>";

            var parser = new PageXmlParser(xml);
            var result = parser.GetOutlineCdataHtml();
            result.Should().Contain("<b>Content</b>");
            result.Should().NotContain("md-note-rendered");
        }

        [Fact]
        public void GetOutlineCdataHtml_SkipsSourceStorageSpans()
        {
            // A hidden span with mdsrc: prefix is used to store markdown source
            var xml =
                "<Page xmlns='http://schemas.microsoft.com/office/onenote/2013/onenote' ID='{p1}'>" +
                "  <Outline>" +
                "    <OEChildren>" +
                "      <OE><T><![CDATA[<p>Visible content</p>]]></T></OE>" +
                "      <OE><T><![CDATA[<span title=\"mdsrc:SGVsbG8=\" style=\"display:none\"></span>]]></T></OE>" +
                "    </OEChildren>" +
                "  </Outline>" +
                "</Page>";

            var parser = new PageXmlParser(xml);
            var result = parser.GetOutlineCdataHtml();
            result.Should().Contain("<p>Visible content</p>");
            result.Should().NotContain("mdsrc:");
        }

        [Fact]
        public void GetOutlineCdataHtml_PreservesFormattingTags()
        {
            var xml =
                "<Page xmlns='http://schemas.microsoft.com/office/onenote/2013/onenote' ID='{p1}'>" +
                "  <Outline>" +
                "    <OEChildren>" +
                "      <OE><T><![CDATA[<a href=\"https://example.com\">Link</a> and <i>italic</i>]]></T></OE>" +
                "    </OEChildren>" +
                "  </Outline>" +
                "</Page>";

            var parser = new PageXmlParser(xml);
            var result = parser.GetOutlineCdataHtml();
            result.Should().Contain("<a href=\"https://example.com\">Link</a>");
            result.Should().Contain("<i>italic</i>");
        }
    }
}
