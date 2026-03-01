using FluentAssertions;
using MDNote.Core;
using MDNote.OneNote.Tests.Helpers;
using Xunit;

namespace MDNote.OneNote.Tests
{
    public class MarkdownExporterTests
    {
        private const string Ns = "http://schemas.microsoft.com/office/onenote/2013/onenote";

        private static string BuildPageXml(string pageId, string markdown = null, string htmlContent = null)
        {
            var metaXml = "";
            if (markdown != null)
            {
                var encoded = MarkdownSourceStorage.EncodeSource(markdown);
                metaXml =
                    $"<Meta name='md-note-source' content='{encoded}'/>" +
                    "<Meta name='md-note-version' content='1.0'/>";
            }

            var outlineXml = "";
            if (htmlContent != null)
            {
                outlineXml =
                    "<Outline objectID='o1'><OEChildren>" +
                    $"<OE><T><![CDATA[{htmlContent}]]></T></OE>" +
                    "</OEChildren></Outline>";
            }

            return
                $"<Page xmlns='{Ns}' ID='{pageId}'>" +
                $"<Title><OE><T><![CDATA[Test]]></T></OE></Title>" +
                metaXml + outlineXml + "</Page>";
        }

        [Fact]
        public void GetMarkdown_WithStoredSource_ReturnsStoredSource()
        {
            var fake = new FakeOneNoteInterop();
            fake.Pages["{page-1}"] = BuildPageXml("{page-1}",
                markdown: "# Hello\n\nWorld",
                htmlContent: "<p style=\"font-size:20pt;font-weight:bold\">Hello</p><p>World</p>");

            var exporter = new MarkdownExporter(fake);
            var result = exporter.GetMarkdown("{page-1}");

            result.Should().Be("# Hello\n\nWorld");
        }

        [Fact]
        public void GetMarkdown_WithoutStoredSource_ReturnsReverseConverted()
        {
            var fake = new FakeOneNoteInterop();
            fake.Pages["{page-1}"] = BuildPageXml("{page-1}",
                markdown: null,
                htmlContent: "<p><strong>Bold text</strong></p>");

            var exporter = new MarkdownExporter(fake);
            var result = exporter.GetMarkdown("{page-1}");

            result.Should().Contain("**Bold text**");
        }

        [Fact]
        public void GetMarkdown_EmptyPage_ReturnsNull()
        {
            var fake = new FakeOneNoteInterop();
            fake.Pages["{page-1}"] = BuildPageXml("{page-1}");

            var exporter = new MarkdownExporter(fake);
            exporter.GetMarkdown("{page-1}").Should().BeNull();
        }

        [Fact]
        public void GetMarkdown_NoPageContent_ReturnsNull()
        {
            var fake = new FakeOneNoteInterop();
            // No page stored

            var exporter = new MarkdownExporter(fake);
            exporter.GetMarkdown("{page-1}").Should().BeNull();
        }

        [Fact]
        public void ReverseConvert_WithContent_ReturnsMarkdown()
        {
            var fake = new FakeOneNoteInterop();
            fake.Pages["{page-1}"] = BuildPageXml("{page-1}",
                markdown: "# Stored source",
                htmlContent: "<p><em>italic</em></p>");

            var exporter = new MarkdownExporter(fake);
            // ReverseConvert always ignores stored source
            var result = exporter.ReverseConvert("{page-1}");

            result.Should().Contain("*italic*");
            result.Should().NotContain("# Stored source");
        }
    }
}
