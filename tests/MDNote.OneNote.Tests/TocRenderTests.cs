using FluentAssertions;
using MDNote.Core;
using MDNote.Core.Models;
using MDNote.OneNote.Tests.Helpers;
using System.Xml.Linq;
using Xunit;

namespace MDNote.OneNote.Tests
{
    public class TocRenderTests
    {
        private static readonly XNamespace OneNs =
            "http://schemas.microsoft.com/office/onenote/2013/onenote";

        [Fact]
        public void RenderWithToc_PageXml_ContainsTocHtml()
        {
            var markdown = "# Heading 1\n\nSome text.\n\n## Heading 2\n\n### Heading 3";
            var options = new ConversionOptions { EnableTableOfContents = true };
            var converter = new MarkdownConverter();
            var result = converter.Convert(markdown, options);

            var fake = new FakeOneNoteInterop();
            fake.Pages["{page-1}"] = BuildMinimalPage("{page-1}");

            var writer = new PageWriter(fake);
            writer.RenderMarkdownToPage("{page-1}", result, markdown);

            fake.LastUpdatedXml.Should().NotBeNull();
            fake.LastUpdatedXml.Should().Contain("Table of Contents");
        }

        [Fact]
        public void RenderWithToc_HeadingsInOutput()
        {
            var markdown = "# Alpha\n## Beta";
            var options = new ConversionOptions { EnableTableOfContents = true };
            var converter = new MarkdownConverter();
            var result = converter.Convert(markdown, options);

            var fake = new FakeOneNoteInterop();
            fake.Pages["{page-1}"] = BuildMinimalPage("{page-1}");

            var writer = new PageWriter(fake);
            writer.RenderMarkdownToPage("{page-1}", result, markdown);

            fake.LastUpdatedXml.Should().Contain("Alpha");
            fake.LastUpdatedXml.Should().Contain("Beta");
        }

        [Fact]
        public void RenderWithoutToc_NoTocInOutput()
        {
            var markdown = "# Alpha\n## Beta";
            var options = new ConversionOptions { EnableTableOfContents = false };
            var converter = new MarkdownConverter();
            var result = converter.Convert(markdown, options);

            var fake = new FakeOneNoteInterop();
            fake.Pages["{page-1}"] = BuildMinimalPage("{page-1}");

            var writer = new PageWriter(fake);
            writer.RenderMarkdownToPage("{page-1}", result, markdown);

            fake.LastUpdatedXml.Should().NotContain("Table of Contents");
        }

        private static string BuildMinimalPage(string pageId)
        {
            var page = new XElement(OneNs + "Page",
                new XAttribute("ID", pageId),
                new XElement(OneNs + "Title",
                    new XElement(OneNs + "OE",
                        new XElement(OneNs + "T", "Test Page"))));
            return page.ToString();
        }
    }
}
