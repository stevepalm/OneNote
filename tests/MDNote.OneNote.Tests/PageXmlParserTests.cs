using System;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace MDNote.OneNote.Tests
{
    public class PageXmlParserTests
    {
        private const string SamplePageXml =
            "<Page xmlns='http://schemas.microsoft.com/office/onenote/2013/onenote' ID='{page-123}'>" +
            "  <Title><OE><T><![CDATA[Test Title]]></T></OE></Title>" +
            "  <Meta name='md-note-source' content='IyBIZWxsbw=='/>" +
            "  <Meta name='md-note-version' content='1.0'/>" +
            "  <Outline objectID='{outline-1}'>" +
            "    <Position x='36.0' y='86.4'/>" +
            "    <OEChildren>" +
            "      <OE><T><![CDATA[<p>First paragraph</p>]]></T></OE>" +
            "      <OE><T><![CDATA[<p>Second paragraph</p>]]></T></OE>" +
            "    </OEChildren>" +
            "  </Outline>" +
            "  <Outline objectID='{outline-2}'>" +
            "    <Position x='36.0' y='400.0'/>" +
            "    <OEChildren>" +
            "      <OE><T><![CDATA[<p>Another outline</p>]]></T></OE>" +
            "    </OEChildren>" +
            "  </Outline>" +
            "</Page>";

        [Fact]
        public void Constructor_NullXml_ThrowsArgumentNull()
        {
            Action act = () => new PageXmlParser(null);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_EmptyXml_ThrowsArgumentNull()
        {
            Action act = () => new PageXmlParser("");
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void GetPageId_ReturnsCorrectId()
        {
            var parser = new PageXmlParser(SamplePageXml);
            parser.GetPageId().Should().Be("{page-123}");
        }

        [Fact]
        public void GetTitle_ReturnsTitle()
        {
            var parser = new PageXmlParser(SamplePageXml);
            parser.GetTitle().Should().Be("Test Title");
        }

        [Fact]
        public void GetTitle_NoTitle_ReturnsNull()
        {
            var xml = "<Page xmlns='http://schemas.microsoft.com/office/onenote/2013/onenote' ID='p1'/>";
            var parser = new PageXmlParser(xml);
            parser.GetTitle().Should().BeNull();
        }

        [Fact]
        public void GetOutlines_ReturnsAll()
        {
            var parser = new PageXmlParser(SamplePageXml);
            var outlines = parser.GetOutlines();
            outlines.Should().HaveCount(2);
        }

        [Fact]
        public void GetOutlines_HaveCorrectPositions()
        {
            var parser = new PageXmlParser(SamplePageXml);
            var outlines = parser.GetOutlines();

            outlines[0].X.Should().Be(36.0);
            outlines[0].Y.Should().Be(86.4);
            outlines[1].Y.Should().Be(400.0);
        }

        [Fact]
        public void GetOutlines_HaveCorrectIds()
        {
            var parser = new PageXmlParser(SamplePageXml);
            var outlines = parser.GetOutlines();

            outlines[0].Id.Should().Be("{outline-1}");
            outlines[1].Id.Should().Be("{outline-2}");
        }

        [Fact]
        public void GetOutlines_HaveHtmlContent()
        {
            var parser = new PageXmlParser(SamplePageXml);
            var outlines = parser.GetOutlines();

            outlines[0].HtmlContent.Should().HaveCount(2);
            outlines[0].HtmlContent[0].Should().Contain("First paragraph");
            outlines[0].HtmlContent[1].Should().Contain("Second paragraph");
        }

        [Fact]
        public void FindOutlineById_Exists_ReturnsOutline()
        {
            var parser = new PageXmlParser(SamplePageXml);
            var outline = parser.FindOutlineById("{outline-1}");
            outline.Should().NotBeNull();
            outline.Id.Should().Be("{outline-1}");
        }

        [Fact]
        public void FindOutlineById_NotFound_ReturnsNull()
        {
            var parser = new PageXmlParser(SamplePageXml);
            parser.FindOutlineById("nonexistent").Should().BeNull();
        }

        [Fact]
        public void FindOutlineByContent_MatchFound_ReturnsOutline()
        {
            var parser = new PageXmlParser(SamplePageXml);
            var outline = parser.FindOutlineByContent("Another outline");
            outline.Should().NotBeNull();
            outline.Id.Should().Be("{outline-2}");
        }

        [Fact]
        public void FindOutlineByContent_NoMatch_ReturnsNull()
        {
            var parser = new PageXmlParser(SamplePageXml);
            parser.FindOutlineByContent("not here").Should().BeNull();
        }

        [Fact]
        public void GetMetaValue_Exists_ReturnsValue()
        {
            var parser = new PageXmlParser(SamplePageXml);
            parser.GetMetaValue("md-note-source").Should().Be("IyBIZWxsbw==");
        }

        [Fact]
        public void GetMetaValue_NotFound_ReturnsNull()
        {
            var parser = new PageXmlParser(SamplePageXml);
            parser.GetMetaValue("nonexistent").Should().BeNull();
        }

        [Fact]
        public void GetAllMeta_ReturnsAll()
        {
            var parser = new PageXmlParser(SamplePageXml);
            var meta = parser.GetAllMeta();
            meta.Should().HaveCount(2);
            meta.Should().ContainKey("md-note-source");
            meta.Should().ContainKey("md-note-version");
        }

        [Fact]
        public void RoundTrip_BuilderOutputParsedByParser()
        {
            var xml = new PageXmlBuilder("page-rt")
                .SetPageTitle("Round Trip Title")
                .AddMeta("md-note-version", "1.0")
                .AddOutline("<p>Round trip content</p>")
                .Build();

            var parser = new PageXmlParser(xml);
            parser.GetPageId().Should().Be("page-rt");
            parser.GetTitle().Should().Be("Round Trip Title");
            parser.GetMetaValue("md-note-version").Should().Be("1.0");
            parser.GetOutlines().Should().HaveCount(1);
            parser.GetOutlines()[0].HtmlContent.First()
                .Should().Contain("Round trip content");
        }
    }
}
