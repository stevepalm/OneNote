using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using FluentAssertions;
using MDNote.Core;
using MDNote.Core.Models;
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
            firstT.Value.Should().Contain("md-note-rendered");
            // Marker must be a hidden span, not an HTML comment (OneNote rejects comments)
            firstT.Value.Should().NotContain("<!--");
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

        // --- CDATA validation: full pipeline XML ---

        // Tags that OneNote accepts inside CDATA HTML.
        // NOTE: table, tr, td are intentionally excluded — they must use
        // native one:Table XML, never CDATA.
        private static readonly HashSet<string> CdataAllowedTags =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "p", "br", "span", "div", "a", "ul", "ol", "li",
                "h1", "h2", "h3", "h4", "h5", "h6",
                "b", "em", "strong", "i", "u", "del", "sup", "sub", "cite", "img"
            };

        private static readonly Regex CdataTagRegex = new Regex(
            @"</?([a-zA-Z][a-zA-Z0-9]*)\b", RegexOptions.Compiled);

        /// <summary>
        /// Validates that every CDATA section in the final XML contains only
        /// OneNote-compatible HTML — no comments, no unsupported tags.
        /// </summary>
        private static void AssertAllCdataValid(string xml)
        {
            // Extract all CDATA content from the XML string
            var cdataRegex = new Regex(@"<!\[CDATA\[([\s\S]*?)\]\]>");
            foreach (Match m in cdataRegex.Matches(xml))
            {
                var cdata = m.Groups[1].Value;

                // No HTML comments
                cdata.Should().NotContain("<!--",
                    "OneNote CDATA must not contain HTML comments");

                // Only whitelisted tags
                foreach (Match tag in CdataTagRegex.Matches(cdata))
                {
                    var tagName = tag.Groups[1].Value;
                    CdataAllowedTags.Should().Contain(tagName,
                        $"CDATA contains unsupported tag <{tagName}>: ...{cdata.Substring(0, Math.Min(200, cdata.Length))}...");
                }
            }
        }

        [Fact]
        public void FullXml_SimpleMarkdown_AllCdataValid()
        {
            var md = "# Hello World\n\nA paragraph with **bold** and `code`.\n";
            var converter = new MarkdownConverter();
            var result = converter.Convert(md, new ConversionOptions
            {
                EnableSyntaxHighlighting = true,
                Theme = "dark",
                InlineAllStyles = true
            });

            var htmlConverter = new HtmlToOneNoteConverter();
            var oneNoteHtml = htmlConverter.ConvertForOneNote(result.Html);
            var sourceTag = MarkdownSourceStorage.BuildHiddenSourceHtml(md);

            var builder = new PageXmlBuilder("page-1")
                .SetPageTitle(result.Title ?? "Test");
            builder.AddRenderedOutline(oneNoteHtml);
            builder.AppendSourceToOutline(sourceTag);
            var xml = builder.Build();

            AssertAllCdataValid(xml);
        }

        [Fact]
        public void FullXml_CodeBlocksAndTables_AllCdataValid()
        {
            var md = @"# Code and Tables

```csharp
public class Foo<T> { }
```

```python
def hello():
    print('world')
```

| Col1 | Col2 |
|------|------|
| A    | B    |

> A blockquote with **bold**.

Normal paragraph.
";
            var converter = new MarkdownConverter();
            var result = converter.Convert(md, new ConversionOptions
            {
                EnableSyntaxHighlighting = true,
                Theme = "dark",
                InlineAllStyles = true
            });

            var htmlConverter = new HtmlToOneNoteConverter();
            var oneNoteHtml = htmlConverter.ConvertForOneNote(result.Html);
            var sourceTag = MarkdownSourceStorage.BuildHiddenSourceHtml(md);

            var builder = new PageXmlBuilder("page-1")
                .SetPageTitle(result.Title ?? "Test");
            builder.AddRenderedOutline(oneNoteHtml);
            builder.AppendSourceToOutline(sourceTag);
            var xml = builder.Build();

            AssertAllCdataValid(xml);

            // Verify no raw table/pre/code tags leaked into CDATA
            // (tables should be native one:Table elements, not HTML)
            var page = XElement.Parse(xml);
            var cdataContents = page.Descendants(OneNs + "T")
                .Select(t => t.Value).ToList();

            foreach (var cdata in cdataContents)
            {
                cdata.Should().NotContain("<pre>", "pre tags must be converted");
                cdata.Should().NotContain("<code>", "code tags must be converted");
                cdata.Should().NotContain("<blockquote>", "blockquotes must be converted");
            }
        }

        [Fact]
        public void FullXml_LargeDocument_AllCdataValid()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("# Large Document");
            for (int i = 0; i < 50; i++)
            {
                sb.AppendLine($"\n## Section {i}");
                sb.AppendLine($"Text with **bold**, *italic*, `code`, and [link](http://example.com).");
                sb.AppendLine($"\n```csharp\npublic class S{i} {{ public int V {{ get; set; }} }}\n```");
                sb.AppendLine($"\n| A | B |\n|---|---|\n| {i} | {i + 1} |");
                sb.AppendLine($"\n> Quote {i}");
            }
            var md = sb.ToString();

            var converter = new MarkdownConverter();
            var result = converter.Convert(md, new ConversionOptions
            {
                EnableSyntaxHighlighting = true,
                Theme = "dark",
                InlineAllStyles = true
            });

            var htmlConverter = new HtmlToOneNoteConverter();
            var oneNoteHtml = htmlConverter.ConvertForOneNote(result.Html);
            var sourceTag = MarkdownSourceStorage.BuildHiddenSourceHtml(md);

            var builder = new PageXmlBuilder("page-1")
                .SetPageTitle(result.Title ?? "Test");
            builder.AddRenderedOutline(oneNoteHtml);
            builder.AppendSourceToOutline(sourceTag);
            var xml = builder.Build();

            AssertAllCdataValid(xml);
        }

        // --- Source separation tests ---

        [Fact]
        public void FullXml_SourceInDedicatedOE_NotMixedWithContent()
        {
            var md = "# Hello\n\nParagraph with **bold** and `code`.";
            var converter = new MarkdownConverter();
            var result = converter.Convert(md, new ConversionOptions
            {
                EnableSyntaxHighlighting = true,
                Theme = "dark",
                InlineAllStyles = true
            });

            var htmlConverter = new HtmlToOneNoteConverter();
            var oneNoteHtml = htmlConverter.ConvertForOneNote(result.Html);
            var sourceTag = MarkdownSourceStorage.BuildHiddenSourceHtml(md);

            var builder = new PageXmlBuilder("page-1")
                .SetPageTitle(result.Title ?? "Test");
            builder.AddRenderedOutline(oneNoteHtml);
            builder.AppendSourceToOutline(sourceTag);
            var xml = builder.Build();

            var page = XElement.Parse(xml);
            var oes = page.Descendants(OneNs + "OE").ToList();

            // Source should be in the last OE
            var lastOe = oes.Last();
            lastOe.Element(OneNs + "T").Value.Should().Contain("mdsrc-gz:");

            // No other OE should contain source data
            foreach (var oe in oes.Take(oes.Count - 1))
            {
                var value = oe.Element(OneNs + "T")?.Value ?? "";
                value.Should().NotContain("mdsrc-gz:", "source must be isolated in dedicated OE");
                value.Should().NotContain("mdsrc:", "source must be isolated in dedicated OE");
            }
        }

        [Fact]
        public void BuildOEChildren_NoTableTagsInCdata()
        {
            var html = "<p>Before</p><table><tr><td>Cell</td></tr></table><p>After</p>";
            var xml = new PageXmlBuilder("page-1")
                .AddOutline(html)
                .Build();
            var page = XElement.Parse(xml);

            var cdataContents = page.Descendants(OneNs + "T")
                .Select(t => t.Value).ToList();

            foreach (var cdata in cdataContents)
            {
                cdata.Should().NotMatchRegex(@"</?table\b",
                    "table tags must not appear in CDATA");
                cdata.Should().NotMatchRegex(@"</?tr\b",
                    "tr tags must not appear in CDATA");
                cdata.Should().NotMatchRegex(@"</?td\b",
                    "td tags must not appear in CDATA");
            }
        }

        [Fact]
        public void BuildOEChildren_TableBuildFailure_FallsBackToText()
        {
            // A malformed table that BuildNativeTable cannot parse (no rows)
            var html = "<table><div>Not a real table</div></table>";
            var xml = new PageXmlBuilder("page-1")
                .AddOutline(html)
                .Build();

            // Should not throw and should not contain table tags in CDATA
            var page = XElement.Parse(xml);
            foreach (var t in page.Descendants(OneNs + "T"))
            {
                t.Value.Should().NotMatchRegex(@"</?table\b");
            }
        }

        [Fact]
        public void FullXml_600LineDocument_AllCdataValid()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("# Session Notes - Large Document");
            sb.AppendLine();
            for (int i = 0; i < 60; i++)
            {
                sb.AppendLine($"## Topic {i}");
                sb.AppendLine($"Discussion about topic {i} with **emphasis** and `inline code`.");
                sb.AppendLine();
                sb.AppendLine("```csharp");
                sb.AppendLine($"public class Topic{i}");
                sb.AppendLine("{");
                sb.AppendLine($"    public string Name {{ get; set; }} = \"Topic {i}\";");
                sb.AppendLine($"    public int Priority {{ get; set; }} = {i};");
                sb.AppendLine("}");
                sb.AppendLine("```");
                sb.AppendLine();
                sb.AppendLine("| Property | Value |");
                sb.AppendLine("|----------|-------|");
                sb.AppendLine($"| Name     | Topic {i} |");
                sb.AppendLine("| Status   | Active |");
                sb.AppendLine();
            }
            var md = sb.ToString();

            // Verify document is large enough to be representative
            md.Split('\n').Length.Should().BeGreaterThan(500);

            var converter = new MarkdownConverter();
            var result = converter.Convert(md, new ConversionOptions
            {
                EnableSyntaxHighlighting = true,
                Theme = "dark",
                InlineAllStyles = true
            });

            var htmlConverter = new HtmlToOneNoteConverter();
            var oneNoteHtml = htmlConverter.ConvertForOneNote(result.Html);
            var sourceTag = MarkdownSourceStorage.BuildHiddenSourceHtml(md);

            var builder = new PageXmlBuilder("page-1")
                .SetPageTitle(result.Title ?? "Test");
            builder.AddRenderedOutline(oneNoteHtml);
            builder.AppendSourceToOutline(sourceTag);
            var xml = builder.Build();

            AssertAllCdataValid(xml);

            // Verify source is compressed and in dedicated OE
            var page = XElement.Parse(xml);
            var lastOeValue = page.Descendants(OneNs + "OE").Last()
                .Element(OneNs + "T").Value;
            lastOeValue.Should().Contain("mdsrc-gz:");

            // Verify round-trip: source can be extracted
            var extracted = MarkdownSourceStorage.ExtractMarkdownSource(lastOeValue);
            extracted.Should().Be(md);
        }

        [Fact]
        public void ClearRenderedOutlines_LegacyCommentMarker_StillDetected()
        {
            // Simulate a page saved with the old <!-- comment --> marker
            var existingXml =
                "<Page xmlns='http://schemas.microsoft.com/office/onenote/2013/onenote' ID='page-1'>" +
                "<Outline objectID='outline-1'>" +
                "<Position x='36' y='86.4'/><Size width='576' height='200'/>" +
                "<OEChildren><OE><T><![CDATA[<!-- md-note-rendered --><p>Old</p>]]></T></OE></OEChildren>" +
                "</Outline></Page>";

            var builder = PageXmlBuilder.FromPageXml(existingXml);
            var removed = builder.ClearRenderedOutlines();
            removed.Should().BeTrue("legacy comment marker should still be detected");
        }

        // --- Native list generation ---

        [Fact]
        public void BuildOEChildren_BulletMarker_ProducesNativeList()
        {
            var html = "<p style=\"margin-left:28px\">" +
                       "<span style=\"display:none\">list-bullet:0</span>Item text</p>";
            var xml = new PageXmlBuilder("page-1")
                .AddOutline(html)
                .Build();
            var page = XElement.Parse(xml);

            var oe = page.Descendants(OneNs + "OE")
                .FirstOrDefault(o => o.Element(OneNs + "List") != null);
            oe.Should().NotBeNull("list marker should produce a native list OE");
            oe.Element(OneNs + "List")
              .Element(OneNs + "Bullet").Should().NotBeNull();
            oe.Element(OneNs + "T").Value.Should().Contain("Item text");
            oe.Element(OneNs + "T").Value.Should().NotContain("list-bullet");
        }

        [Fact]
        public void BuildOEChildren_NumberMarker_ProducesNativeList()
        {
            var html = "<p style=\"margin-left:28px\">" +
                       "<span style=\"display:none\">list-number:0</span>First item</p>";
            var xml = new PageXmlBuilder("page-1")
                .AddOutline(html)
                .Build();
            var page = XElement.Parse(xml);

            var oe = page.Descendants(OneNs + "OE")
                .FirstOrDefault(o => o.Element(OneNs + "List") != null);
            oe.Should().NotBeNull("list marker should produce a native list OE");
            var number = oe.Element(OneNs + "List")
              .Element(OneNs + "Number");
            number.Should().NotBeNull();
            number.Attribute("numberFormat").Should().NotBeNull("OneNote requires numberFormat");
            number.Attribute("numberFormat").Value.Should().Be("##.");
            oe.Element(OneNs + "T").Value.Should().Contain("First item");
            oe.Element(OneNs + "T").Value.Should().NotContain("list-number");

            // Verify XML contains numberFormat (what OneNote validates)
            xml.Should().Contain("numberFormat");
        }

        // --- Table header shading ---

        [Fact]
        public void BuildNativeTable_HeaderWithBackground_HasShadingColor()
        {
            var html = "<table style=\"border-collapse:collapse;margin:8px 0\">" +
                       "<tr><td style=\"border:1px solid #ccc;padding:6px 10px;" +
                       "font-weight:bold;border-bottom:2px solid #999;" +
                       "background-color:#DEEBF6\">Header</td></tr>" +
                       "<tr><td style=\"border:1px solid #ccc;padding:6px 10px\">Data</td></tr></table>";
            var xml = new PageXmlBuilder("page-1")
                .AddOutline(html)
                .Build();
            var page = XElement.Parse(xml);

            var cells = page.Descendants(OneNs + "Cell").ToList();
            cells.Should().HaveCountGreaterThanOrEqualTo(2);
            cells[0].Attribute("shadingColor")?.Value.Should().Be("#DEEBF6");
            cells[1].Attribute("shadingColor").Should().BeNull();
        }

        // --- List spacing ---

        [Fact]
        public void BuildOEChildren_ListToNonList_InsertsSpacerOE()
        {
            var html = "<p style=\"margin-left:28px\">" +
                       "<span style=\"display:none\">list-bullet:0</span>Item</p>" +
                       "<p style=\"font-family:Calibri;font-size:11pt\">Body text</p>";
            var xml = new PageXmlBuilder("page-1")
                .AddOutline(html)
                .Build();
            var page = XElement.Parse(xml);

            var oes = page.Element(OneNs + "Outline")
                         .Element(OneNs + "OEChildren")
                         .Elements(OneNs + "OE").ToList();

            // Should have: list OE, spacer OE, body OE = at least 3
            oes.Count.Should().BeGreaterThanOrEqualTo(3);
            // Spacer OE should contain &nbsp;
            oes[1].Element(OneNs + "T").Value.Should().Contain("&nbsp;");
        }
    }
}
