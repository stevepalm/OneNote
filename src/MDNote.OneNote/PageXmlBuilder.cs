using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace MDNote.OneNote
{
    /// <summary>
    /// Builds OneNote page XML for use with UpdatePageContent.
    /// Fluent API: new PageXmlBuilder(pageId).SetPageTitle(...).AddMeta(...).AddOutline(...).Build()
    /// </summary>
    public class PageXmlBuilder
    {
        private static readonly XNamespace OneNs =
            "http://schemas.microsoft.com/office/onenote/2013/onenote";

        // Matches the start of block-level HTML elements for splitting into separate OEs.
        // Uses lookahead so the tag is preserved in the split output.
        private static readonly Regex BlockSplitRegex = new Regex(
            @"(?=<(?:p|table|div|ul|ol)[\s>])",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private readonly XElement _page;

        /// <summary>
        /// Creates a builder for a new page with the given ID.
        /// </summary>
        public PageXmlBuilder(string pageId)
        {
            if (string.IsNullOrEmpty(pageId))
                throw new ArgumentNullException(nameof(pageId));

            _page = new XElement(OneNs + "Page",
                new XAttribute("ID", pageId));
        }

        private PageXmlBuilder(XElement page)
        {
            _page = page;
        }

        /// <summary>
        /// Creates a builder from existing page XML (for update scenarios like ReplaceOutline).
        /// </summary>
        public static PageXmlBuilder FromPageXml(string xml)
        {
            if (string.IsNullOrEmpty(xml))
                throw new ArgumentNullException(nameof(xml));

            return new PageXmlBuilder(XElement.Parse(xml));
        }

        /// <summary>
        /// Sets the page title. Replaces any existing title.
        /// </summary>
        public PageXmlBuilder SetPageTitle(string title)
        {
            _page.Element(OneNs + "Title")?.Remove();

            var titleElement = new XElement(OneNs + "Title",
                new XElement(OneNs + "OE",
                    new XElement(OneNs + "T",
                        new XCData(title ?? ""))));

            _page.AddFirst(titleElement);
            return this;
        }

        /// <summary>
        /// Adds a Meta element. Inserted after Title and existing Meta elements,
        /// before Outline elements (OneNote requires strict element ordering).
        /// </summary>
        public PageXmlBuilder AddMeta(string name, string content)
        {
            var meta = new XElement(OneNs + "Meta",
                new XAttribute("name", name),
                new XAttribute("content", content ?? ""));

            // Insert after last existing Meta, or after Title, or at start
            var lastMeta = _page.Elements(OneNs + "Meta").LastOrDefault();
            if (lastMeta != null)
            {
                lastMeta.AddAfterSelf(meta);
            }
            else
            {
                var title = _page.Element(OneNs + "Title");
                if (title != null)
                    title.AddAfterSelf(meta);
                else
                    _page.AddFirst(meta);
            }

            return this;
        }

        /// <summary>
        /// Adds an Outline element containing the HTML content split into separate OE elements.
        /// </summary>
        public PageXmlBuilder AddOutline(string html,
            double left = 36.0, double top = 86.4, double width = 576.0)
        {
            var outline = new XElement(OneNs + "Outline",
                new XElement(OneNs + "Position",
                    new XAttribute("x", left.ToString(CultureInfo.InvariantCulture)),
                    new XAttribute("y", top.ToString(CultureInfo.InvariantCulture)),
                    new XAttribute("isSetByUser", "true")),
                new XElement(OneNs + "Size",
                    new XAttribute("width", width.ToString(CultureInfo.InvariantCulture)),
                    new XAttribute("isSetByUser", "true")));

            var oeChildren = BuildOEChildren(html);
            outline.Add(oeChildren);
            _page.Add(outline);
            return this;
        }

        /// <summary>
        /// Replaces the content of an existing outline identified by objectID or ID attribute.
        /// Requires using FromPageXml() to load existing page XML first.
        /// </summary>
        public PageXmlBuilder ReplaceOutline(string outlineId, string html)
        {
            var outline = _page.Elements(OneNs + "Outline")
                .FirstOrDefault(o =>
                    o.Attribute("objectID")?.Value == outlineId ||
                    o.Attribute("ID")?.Value == outlineId);

            if (outline == null)
                throw new InvalidOperationException(
                    $"Outline with ID '{outlineId}' not found.");

            outline.Element(OneNs + "OEChildren")?.Remove();
            outline.Add(BuildOEChildren(html));
            return this;
        }

        /// <summary>
        /// Builds the final XML string for use with UpdatePageContent.
        /// </summary>
        public string Build()
        {
            return _page.ToString(SaveOptions.DisableFormatting);
        }

        private XElement BuildOEChildren(string html)
        {
            var oeChildren = new XElement(OneNs + "OEChildren");
            var blocks = SplitHtmlBlocks(html);

            foreach (var block in blocks)
            {
                oeChildren.Add(
                    new XElement(OneNs + "OE",
                        new XElement(OneNs + "T",
                            new XCData(block))));
            }

            return oeChildren;
        }

        /// <summary>
        /// Splits HTML into block-level chunks. Each chunk becomes a separate OE element.
        /// Conservative: keeps content together when in doubt.
        /// </summary>
        internal static List<string> SplitHtmlBlocks(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
                return new List<string> { html ?? "" };

            var parts = BlockSplitRegex.Split(html);
            var blocks = new List<string>();

            foreach (var part in parts)
            {
                var trimmed = part.Trim();
                if (!string.IsNullOrEmpty(trimmed))
                    blocks.Add(trimmed);
            }

            return blocks.Count > 0 ? blocks : new List<string> { html };
        }
    }
}
