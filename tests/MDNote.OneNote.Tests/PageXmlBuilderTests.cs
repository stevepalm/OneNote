using System;
using System.Linq;
using System.Xml.Linq;
using FluentAssertions;
using Xunit;

namespace MDNote.OneNote.Tests
{
    public class PageXmlBuilderTests
    {
        private static readonly XNamespace OneNs =
            "http://schemas.microsoft.com/office/onenote/2013/onenote";

        [Fact]
        public void Build_EmptyPage_HasPageElement()
        {
            var xml = new PageXmlBuilder("page-1").Build();
            var page = XElement.Parse(xml);
            page.Name.Should().Be(OneNs + "Page");
            page.Attribute("ID").Value.Should().Be("page-1");
        }

        [Fact]
        public void Constructor_NullPageId_ThrowsArgumentNull()
        {
            Action act = () => new PageXmlBuilder(null);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Build_ProducesValidXml()
        {
            var xml = new PageXmlBuilder("page-1")
                .SetPageTitle("Test")
                .AddMeta("key", "value")
                .AddOutline("<p>Content</p>")
                .Build();

            // Should be parseable without throwing
            var page = XElement.Parse(xml);
            page.Should().NotBeNull();
        }

        [Fact]
        public void SetPageTitle_ContainsTitleOET()
        {
            var xml = new PageXmlBuilder("page-1")
                .SetPageTitle("My Title")
                .Build();
            var page = XElement.Parse(xml);

            var title = page.Element(OneNs + "Title");
            title.Should().NotBeNull();
            var oe = title.Element(OneNs + "OE");
            oe.Should().NotBeNull();
            var t = oe.Element(OneNs + "T");
            t.Should().NotBeNull();
            t.Value.Should().Be("My Title");
        }

        [Fact]
        public void SetPageTitle_TitleInCdata()
        {
            var xml = new PageXmlBuilder("page-1")
                .SetPageTitle("Test <Title>")
                .Build();

            // CDATA wraps the content — the raw XML should contain CDATA
            xml.Should().Contain("<![CDATA[");
        }

        [Fact]
        public void SetPageTitle_Twice_ReplacesFirst()
        {
            var xml = new PageXmlBuilder("page-1")
                .SetPageTitle("First")
                .SetPageTitle("Second")
                .Build();
            var page = XElement.Parse(xml);

            page.Elements(OneNs + "Title").Should().HaveCount(1);
            page.Element(OneNs + "Title")
                .Element(OneNs + "OE")
                .Element(OneNs + "T").Value
                .Should().Be("Second");
        }

        [Fact]
        public void AddMeta_SingleEntry_HasMetaElement()
        {
            var xml = new PageXmlBuilder("page-1")
                .AddMeta("key", "value")
                .Build();
            var page = XElement.Parse(xml);

            var meta = page.Elements(OneNs + "Meta").Single();
            meta.Attribute("name").Value.Should().Be("key");
            meta.Attribute("content").Value.Should().Be("value");
        }

        [Fact]
        public void AddMeta_MultipleEntries_AllPresent()
        {
            var xml = new PageXmlBuilder("page-1")
                .AddMeta("a", "1")
                .AddMeta("b", "2")
                .AddMeta("c", "3")
                .Build();
            var page = XElement.Parse(xml);

            page.Elements(OneNs + "Meta").Should().HaveCount(3);
        }

        [Fact]
        public void Build_CorrectElementOrder_TitleBeforeMetaBeforeOutline()
        {
            var xml = new PageXmlBuilder("page-1")
                .SetPageTitle("Title")
                .AddMeta("key", "val")
                .AddOutline("<p>Content</p>")
                .Build();
            var page = XElement.Parse(xml);

            var children = page.Elements().ToList();
            var titleIdx = children.FindIndex(e => e.Name == OneNs + "Title");
            var metaIdx = children.FindIndex(e => e.Name == OneNs + "Meta");
            var outlineIdx = children.FindIndex(e => e.Name == OneNs + "Outline");

            titleIdx.Should().BeLessThan(metaIdx);
            metaIdx.Should().BeLessThan(outlineIdx);
        }

        [Fact]
        public void AddOutline_SingleParagraph_HasOutlineStructure()
        {
            var xml = new PageXmlBuilder("page-1")
                .AddOutline("<p>Hello</p>")
                .Build();
            var page = XElement.Parse(xml);

            var outline = page.Element(OneNs + "Outline");
            outline.Should().NotBeNull();
            outline.Element(OneNs + "Position").Should().NotBeNull();
            outline.Element(OneNs + "Size").Should().NotBeNull();
            outline.Element(OneNs + "OEChildren").Should().NotBeNull();

            var oe = outline.Element(OneNs + "OEChildren")
                            .Element(OneNs + "OE");
            oe.Should().NotBeNull();
            oe.Element(OneNs + "T").Value.Should().Contain("Hello");
        }

        [Fact]
        public void AddOutline_DefaultPosition_Is36x86()
        {
            var xml = new PageXmlBuilder("page-1")
                .AddOutline("<p>Content</p>")
                .Build();
            var page = XElement.Parse(xml);

            var pos = page.Element(OneNs + "Outline")
                          .Element(OneNs + "Position");
            pos.Attribute("x").Value.Should().Be("36");
            pos.Attribute("y").Value.Should().Be("86.4");
        }

        [Fact]
        public void AddOutline_CustomPosition_UsesProvidedValues()
        {
            var xml = new PageXmlBuilder("page-1")
                .AddOutline("<p>Content</p>", left: 100.5, top: 200.0, width: 400.0)
                .Build();
            var page = XElement.Parse(xml);

            var pos = page.Element(OneNs + "Outline")
                          .Element(OneNs + "Position");
            pos.Attribute("x").Value.Should().Be("100.5");
            pos.Attribute("y").Value.Should().Be("200");

            var size = page.Element(OneNs + "Outline")
                           .Element(OneNs + "Size");
            size.Attribute("width").Value.Should().Be("400");
        }

        [Fact]
        public void AddOutline_MultipleBlocks_SplitsIntoSeparateOEs()
        {
            var html = "<p>First</p><p>Second</p><p>Third</p>";
            var xml = new PageXmlBuilder("page-1")
                .AddOutline(html)
                .Build();
            var page = XElement.Parse(xml);

            var oes = page.Element(OneNs + "Outline")
                          .Element(OneNs + "OEChildren")
                          .Elements(OneNs + "OE")
                          .ToList();

            oes.Should().HaveCount(3);
        }

        [Fact]
        public void AddOutline_TableBlock_ConvertedToNativeTable()
        {
            var html = "<table><tr><td>Cell1</td><td>Cell2</td></tr></table>";
            var xml = new PageXmlBuilder("page-1")
                .AddOutline(html)
                .Build();
            var page = XElement.Parse(xml);

            var oe = page.Element(OneNs + "Outline")
                         .Element(OneNs + "OEChildren")
                         .Element(OneNs + "OE");

            // Should contain a native one:Table, not HTML table in CDATA
            var table = oe.Element(OneNs + "Table");
            table.Should().NotBeNull("HTML tables must be converted to native OneNote tables");
            table.Attribute("bordersVisible").Value.Should().Be("true");

            var columns = table.Element(OneNs + "Columns").Elements(OneNs + "Column").ToList();
            columns.Should().HaveCount(2);

            var rows = table.Elements(OneNs + "Row").ToList();
            rows.Should().HaveCount(1);

            var cells = rows[0].Elements(OneNs + "Cell").ToList();
            cells.Should().HaveCount(2);

            // Cell content should be in OEChildren/OE/T CDATA
            var cellText = cells[0].Element(OneNs + "OEChildren")
                                   .Element(OneNs + "OE")
                                   .Element(OneNs + "T").Value;
            cellText.Should().Be("Cell1");
        }

        [Fact]
        public void AddOutline_EmptyHtml_DoesNotThrow()
        {
            var xml = new PageXmlBuilder("page-1")
                .AddOutline("")
                .Build();
            var page = XElement.Parse(xml);
            page.Element(OneNs + "Outline").Should().NotBeNull();
        }

        [Fact]
        public void AddOutline_MultipleOutlines_AllAdded()
        {
            var xml = new PageXmlBuilder("page-1")
                .AddOutline("<p>First outline</p>", top: 86.4)
                .AddOutline("<p>Second outline</p>", top: 400.0)
                .Build();
            var page = XElement.Parse(xml);

            page.Elements(OneNs + "Outline").Should().HaveCount(2);
        }

        [Fact]
        public void FromPageXml_NullXml_ThrowsArgumentNull()
        {
            Action act = () => PageXmlBuilder.FromPageXml(null);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void ReplaceOutline_ExistingOutline_ReplacesContent()
        {
            var existingXml =
                "<Page xmlns='http://schemas.microsoft.com/office/onenote/2013/onenote' ID='page-1'>" +
                "<Outline objectID='outline-1'>" +
                "<Position x='36' y='86.4'/>" +
                "<OEChildren><OE><T><![CDATA[Old content]]></T></OE></OEChildren>" +
                "</Outline></Page>";

            var xml = PageXmlBuilder.FromPageXml(existingXml)
                .ReplaceOutline("outline-1", "<p>New content</p>")
                .Build();
            var page = XElement.Parse(xml);

            var t = page.Element(OneNs + "Outline")
                        .Element(OneNs + "OEChildren")
                        .Element(OneNs + "OE")
                        .Element(OneNs + "T");
            t.Value.Should().Contain("New content");
            t.Value.Should().NotContain("Old content");
        }

        [Fact]
        public void ReplaceOutline_NonExistentId_ThrowsInvalidOperation()
        {
            var existingXml =
                "<Page xmlns='http://schemas.microsoft.com/office/onenote/2013/onenote' ID='page-1'>" +
                "<Outline objectID='outline-1'>" +
                "<Position x='36' y='86.4'/>" +
                "<OEChildren><OE><T><![CDATA[Content]]></T></OE></OEChildren>" +
                "</Outline></Page>";

            var builder = PageXmlBuilder.FromPageXml(existingXml);
            Action act = () => builder.ReplaceOutline("nonexistent", "<p>New</p>");
            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void SplitHtmlBlocks_MixedContent_SplitsCorrectly()
        {
            var html = "<p>Para1</p><table><tr><td>T</td></tr></table><p>Para2</p>";
            var blocks = PageXmlBuilder.SplitHtmlBlocks(html);
            blocks.Should().HaveCount(3);
            blocks[0].Should().StartWith("<p>");
            blocks[1].Should().StartWith("<table>");
            blocks[2].Should().StartWith("<p>");
        }

        [Fact]
        public void SplitHtmlBlocks_NullInput_ReturnsSingleEmpty()
        {
            var blocks = PageXmlBuilder.SplitHtmlBlocks(null);
            blocks.Should().HaveCount(1);
        }

        // --- SetMeta tests ---

        [Fact]
        public void SetMeta_NewKey_AddsMetaElement()
        {
            var xml = new PageXmlBuilder("page-1")
                .SetMeta("mykey", "myvalue")
                .Build();
            var page = XElement.Parse(xml);

            var meta = page.Elements(OneNs + "Meta").Single();
            meta.Attribute("name").Value.Should().Be("mykey");
            meta.Attribute("content").Value.Should().Be("myvalue");
        }

        [Fact]
        public void SetMeta_ExistingKey_UpdatesValue()
        {
            var xml = new PageXmlBuilder("page-1")
                .AddMeta("key", "original")
                .SetMeta("key", "updated")
                .Build();
            var page = XElement.Parse(xml);

            page.Elements(OneNs + "Meta").Should().HaveCount(1);
            page.Elements(OneNs + "Meta").Single()
                .Attribute("content").Value.Should().Be("updated");
        }

        [Fact]
        public void SetMeta_MixedAddAndSet_NoduplicatesForSameKey()
        {
            var xml = new PageXmlBuilder("page-1")
                .SetMeta("a", "1")
                .SetMeta("b", "2")
                .SetMeta("a", "3")
                .Build();
            var page = XElement.Parse(xml);

            page.Elements(OneNs + "Meta").Should().HaveCount(2);
            page.Elements(OneNs + "Meta")
                .First(m => m.Attribute("name").Value == "a")
                .Attribute("content").Value.Should().Be("3");
        }

        [Fact]
        public void SetMeta_PreservesElementOrder()
        {
            var xml = new PageXmlBuilder("page-1")
                .SetPageTitle("Title")
                .SetMeta("key", "val")
                .AddOutline("<p>Content</p>")
                .Build();
            var page = XElement.Parse(xml);

            var children = page.Elements().ToList();
            var titleIdx = children.FindIndex(e => e.Name == OneNs + "Title");
            var metaIdx = children.FindIndex(e => e.Name == OneNs + "Meta");
            var outlineIdx = children.FindIndex(e => e.Name == OneNs + "Outline");

            titleIdx.Should().BeLessThan(metaIdx);
            metaIdx.Should().BeLessThan(outlineIdx);
        }

        // --- ClearOutlines tests ---

        [Fact]
        public void ClearOutlines_RemovesAllOutlines()
        {
            var xml = new PageXmlBuilder("page-1")
                .AddOutline("<p>First</p>")
                .AddOutline("<p>Second</p>")
                .ClearOutlines()
                .Build();
            var page = XElement.Parse(xml);

            page.Elements(OneNs + "Outline").Should().BeEmpty();
        }

        [Fact]
        public void ClearOutlines_PreservesTitleAndMeta()
        {
            var xml = new PageXmlBuilder("page-1")
                .SetPageTitle("Keep Me")
                .AddMeta("key", "value")
                .AddOutline("<p>Remove Me</p>")
                .ClearOutlines()
                .Build();
            var page = XElement.Parse(xml);

            page.Element(OneNs + "Title").Should().NotBeNull();
            page.Elements(OneNs + "Meta").Should().HaveCount(1);
            page.Elements(OneNs + "Outline").Should().BeEmpty();
        }

        [Fact]
        public void ClearOutlines_EmptyPage_NoOp()
        {
            var xml = new PageXmlBuilder("page-1")
                .ClearOutlines()
                .Build();
            var page = XElement.Parse(xml);

            page.Elements(OneNs + "Outline").Should().BeEmpty();
        }

        // --- ClearRenderedOutlines tests ---

        [Fact]
        public void ClearRenderedOutlines_RemovesOnlyMarkedOutlines()
        {
            var builder = new PageXmlBuilder("page-1")
                .AddRenderedOutline("<p>Rendered content</p>")
                .AddOutline("<p>User content</p>");

            builder.ClearRenderedOutlines();
            var xml = builder.Build();
            var page = XElement.Parse(xml);

            var outlines = page.Elements(OneNs + "Outline").ToList();
            outlines.Should().HaveCount(1);
            outlines[0].Descendants(OneNs + "T").First().Value.Should().Contain("User content");
        }

        [Fact]
        public void ClearRenderedOutlines_PreservesUnmarkedOutlines()
        {
            var builder = new PageXmlBuilder("page-1")
                .AddOutline("<p>Ink/drawing content</p>")
                .AddOutline("<p>Another user outline</p>");

            var removed = builder.ClearRenderedOutlines();
            var xml = builder.Build();
            var page = XElement.Parse(xml);

            removed.Should().BeFalse();
            page.Elements(OneNs + "Outline").Should().HaveCount(2);
        }

        [Fact]
        public void ClearRenderedOutlines_NoMarkedOutlines_ReturnsFalse()
        {
            var builder = new PageXmlBuilder("page-1")
                .AddOutline("<p>Regular content</p>");

            var removed = builder.ClearRenderedOutlines();
            removed.Should().BeFalse();
        }

        [Fact]
        public void AddRenderedOutline_ContainsMarker()
        {
            var xml = new PageXmlBuilder("page-1")
                .AddRenderedOutline("<p>Content</p>")
                .Build();
            var page = XElement.Parse(xml);

            var firstT = page.Descendants(OneNs + "T").First();
            firstT.Value.Should().Contain("<!-- md-note-rendered -->");
        }

        [Fact]
        public void ClearRenderedOutlines_MultipleMarked_RemovesAll()
        {
            var builder = new PageXmlBuilder("page-1")
                .AddRenderedOutline("<p>Rendered 1</p>")
                .AddRenderedOutline("<p>Rendered 2</p>")
                .AddOutline("<p>Preserved</p>");

            var removed = builder.ClearRenderedOutlines();
            var xml = builder.Build();
            var page = XElement.Parse(xml);

            removed.Should().BeTrue();
            page.Elements(OneNs + "Outline").Should().HaveCount(1);
        }
    }
}
