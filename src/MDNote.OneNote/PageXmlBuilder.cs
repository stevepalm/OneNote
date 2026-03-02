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

        // Elements that UpdatePageContent does not accept.
        // Its schema only allows: Title?, (Outline|Image|InkDrawing|InsertedFile|MediaFile)*.
        // All other elements (Meta, TagDef, QuickStyleDef, PageSettings, etc.)
        // are preserved by OneNote on the server side — omitting them is safe.
        private static readonly HashSet<string> StrippedElements =
            new HashSet<string>
            {
                "TagDef", "QuickStyleDef", "XPSFile",
                "Meta", "MediaPlaylist", "MeetingInfo", "PageSettings"
            };

        /// <summary>
        /// Creates a builder from existing page XML (for update scenarios like ReplaceOutline).
        /// </summary>
        public static PageXmlBuilder FromPageXml(string xml)
        {
            if (string.IsNullOrEmpty(xml))
                throw new ArgumentNullException(nameof(xml));

            var page = XElement.Parse(xml);

            // Strip read-only attributes that OneNote returns but rejects on update
            foreach (var attr in page.Descendants()
                .Attributes("isSetByUser").ToList())
            {
                attr.Remove();
            }

            // Strip elements we don't modify. UpdatePageContent is a merge —
            // omitting them preserves them on the page while avoiding schema
            // validation errors from GetPageContent's non-standard element order.
            foreach (var el in page.Elements()
                .Where(e => StrippedElements.Contains(e.Name.LocalName)).ToList())
            {
                el.Remove();
            }

            return new PageXmlBuilder(page);
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

        // OneNote Page schema element order — Meta must appear after these:
        // (TagDef, QuickStyleDef, XPSFile are stripped in FromPageXml but kept
        // here for correctness if elements are ever added manually)
        private static readonly string[] BeforeMeta =
            { "Title", "TagDef", "QuickStyleDef", "XPSFile" };

        // ... and before these (content elements):
        private static readonly string[] AfterMeta =
            { "Outline", "Image", "InkDrawing", "InsertedFile", "MediaFile" };

        /// <summary>
        /// Adds a Meta element in the correct schema position.
        /// OneNote requires: Title, TagDef*, QuickStyleDef*, XPSFile*, Meta*,
        /// MediaPlaylist*, MeetingInfo*, PageSettings?, Outline/Image/etc.
        /// </summary>
        public PageXmlBuilder AddMeta(string name, string content)
        {
            var meta = new XElement(OneNs + "Meta",
                new XAttribute("name", name),
                new XAttribute("content", content ?? ""));

            // Best case: insert after last existing Meta
            var lastMeta = _page.Elements(OneNs + "Meta").LastOrDefault();
            if (lastMeta != null)
            {
                lastMeta.AddAfterSelf(meta);
                return this;
            }

            // No existing Meta — find correct position per schema.
            // Insert before the first element that must come AFTER Meta.
            foreach (var tag in AfterMeta)
            {
                var first = _page.Element(OneNs + tag);
                if (first != null)
                {
                    first.AddBeforeSelf(meta);
                    return this;
                }
            }

            // Nothing after Meta position found — insert after the last
            // element that must come BEFORE Meta, or append to page.
            for (int i = BeforeMeta.Length - 1; i >= 0; i--)
            {
                var last = _page.Elements(OneNs + BeforeMeta[i]).LastOrDefault();
                if (last != null)
                {
                    last.AddAfterSelf(meta);
                    return this;
                }
            }

            // Empty page — just append
            _page.Add(meta);
            return this;
        }

        /// <summary>
        /// Adds an Outline element containing the HTML content split into separate OE elements.
        /// </summary>
        public PageXmlBuilder AddOutline(string html,
            double left = 36.0, double top = 86.4, double width = 576.0, double height = 200.0)
        {
            // Note: do NOT include isSetByUser — it's a read-only attribute
            // that OneNote returns but rejects on UpdatePageContent.
            // Both width and height are required by the OneNote XML schema.
            // OneNote will auto-adjust height to fit the actual content.
            var outline = new XElement(OneNs + "Outline",
                new XElement(OneNs + "Position",
                    new XAttribute("x", left.ToString(CultureInfo.InvariantCulture)),
                    new XAttribute("y", top.ToString(CultureInfo.InvariantCulture))),
                new XElement(OneNs + "Size",
                    new XAttribute("width", width.ToString(CultureInfo.InvariantCulture)),
                    new XAttribute("height", height.ToString(CultureInfo.InvariantCulture))));

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
        /// Sets a Meta element value. Updates existing if name matches, otherwise adds new.
        /// </summary>
        public PageXmlBuilder SetMeta(string name, string content)
        {
            var existing = _page.Elements(OneNs + "Meta")
                .FirstOrDefault(m => m.Attribute("name")?.Value == name);

            if (existing != null)
            {
                existing.SetAttributeValue("content", content ?? "");
            }
            else
            {
                AddMeta(name, content);
            }

            return this;
        }

        /// <summary>
        /// Removes all Outline elements from the page.
        /// </summary>
        public PageXmlBuilder ClearOutlines()
        {
            _page.Elements(OneNs + "Outline").Remove();
            return this;
        }

        /// <summary>
        /// Removes only outlines that contain the md-note-rendered marker in their CDATA.
        /// Preserves ink, drawings, embedded files, and user-created outlines.
        /// Returns true if any outlines were removed.
        /// </summary>
        public bool ClearRenderedOutlines()
        {
            var outlines = _page.Elements(OneNs + "Outline").ToList();
            bool removed = false;

            foreach (var outline in outlines)
            {
                var hasMarker = outline.Descendants(OneNs + "T")
                    .Any(t => t.Value.Contains("md-note-rendered"));
                if (hasMarker)
                {
                    outline.Remove();
                    removed = true;
                }
            }

            return removed;
        }

        // Marker span used to tag outlines produced by MD Note.
        // Uses a hidden <span> instead of an HTML comment because OneNote's
        // CDATA parser does not accept <!-- --> comments.
        private const string RenderedMarker =
            "<span style=\"display:none\">md-note-rendered</span>";

        /// <summary>
        /// Adds an outline with the md-note-rendered marker so it can be identified
        /// for selective clearing on re-render.
        /// </summary>
        public PageXmlBuilder AddRenderedOutline(string html,
            double left = 36.0, double top = 86.4, double width = 576.0, double height = 200.0)
        {
            return AddOutline(RenderedMarker + html, left, top, width, height);
        }

        /// <summary>
        /// Replaces the OEChildren of the first outline that has the md-note-rendered marker,
        /// or the first outline if no marker is found. Preserves the outline's objectID so
        /// OneNote updates in-place rather than creating a duplicate.
        /// UpdatePageContent is a merge operation — removing outlines from XML doesn't delete
        /// them from the page; we must replace content of existing outlines.
        /// Returns true if an existing outline was replaced; false if a new one was added.
        /// </summary>
        public bool ReplaceOrAddRenderedOutline(string html)
        {
            var markedHtml = RenderedMarker + html;

            // First try: find outline with the md-note-rendered marker (re-render).
            // Search for plain text "md-note-rendered" so we also find outlines
            // saved with the legacy <!-- comment --> format.
            var target = _page.Elements(OneNs + "Outline")
                .FirstOrDefault(o => o.Descendants(OneNs + "T")
                    .Any(t => t.Value.Contains("md-note-rendered")));

            // Second try: use the first outline (first render — raw markdown outline)
            if (target == null)
                target = _page.Elements(OneNs + "Outline").FirstOrDefault();

            if (target != null)
            {
                // Replace content in-place, preserving objectID and position
                target.Element(OneNs + "OEChildren")?.Remove();
                target.Add(BuildOEChildren(markedHtml));
                return true;
            }

            // No existing outline — add new one
            AddOutline(markedHtml);
            return false;
        }

        // OneNote Page schema element order for UpdatePageContent validation.
        // GetPageContent may return elements out of this order, so we
        // must normalize before sending back via UpdatePageContent.
        private static readonly string[] SchemaOrder =
        {
            "Title", "TagDef", "QuickStyleDef", "XPSFile", "Meta",
            "MediaPlaylist", "MeetingInfo", "PageSettings",
            "Outline", "Image", "InkDrawing", "InsertedFile", "MediaFile"
        };

        /// <summary>
        /// Builds the final XML string for use with UpdatePageContent.
        /// Normalizes child element order to match the OneNote schema,
        /// since GetPageContent may return elements in non-schema order.
        /// </summary>
        public string Build()
        {
            NormalizeElementOrder();
            return _page.ToString(SaveOptions.DisableFormatting);
        }

        private void NormalizeElementOrder()
        {
            var children = _page.Elements().ToList();
            if (children.Count <= 1)
                return;

            var orderMap = new Dictionary<string, int>();
            for (int i = 0; i < SchemaOrder.Length; i++)
                orderMap[SchemaOrder[i]] = i;

            // Unknown elements sort to the end (after MediaFile)
            int unknownOrder = SchemaOrder.Length;

            var sorted = children
                .OrderBy(e =>
                {
                    var localName = e.Name.LocalName;
                    return orderMap.TryGetValue(localName, out var idx)
                        ? idx
                        : unknownOrder;
                })
                .ToList();

            // Only rewrite if order actually changed
            bool orderChanged = false;
            for (int i = 0; i < children.Count; i++)
            {
                if (children[i] != sorted[i])
                {
                    orderChanged = true;
                    break;
                }
            }

            if (!orderChanged)
                return;

            foreach (var child in children)
                child.Remove();

            foreach (var child in sorted)
                _page.Add(child);
        }

        // Regex to extract table rows
        private static readonly Regex TableRowRegex = new Regex(
            @"<tr[^>]*>([\s\S]*?)</tr>",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Regex to extract table cells (td)
        private static readonly Regex TableCellRegex = new Regex(
            @"<td[^>]*>([\s\S]*?)</td>",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private XElement BuildOEChildren(string html)
        {
            var oeChildren = new XElement(OneNs + "OEChildren");
            var blocks = SplitHtmlBlocks(html);

            foreach (var block in blocks)
            {
                if (block.StartsWith("<table", StringComparison.OrdinalIgnoreCase))
                {
                    // Convert HTML table to OneNote native table XML
                    var tableOe = BuildNativeTable(block);
                    if (tableOe != null)
                    {
                        oeChildren.Add(tableOe);
                        continue;
                    }
                }

                oeChildren.Add(
                    new XElement(OneNs + "OE",
                        new XElement(OneNs + "T",
                            new XCData(block))));
            }

            return oeChildren;
        }

        /// <summary>
        /// Converts an HTML table to a OneNote native Table element wrapped in an OE.
        /// OneNote does not support HTML table tags in CDATA — tables must use
        /// one:Table/one:Row/one:Cell native XML structure.
        /// </summary>
        private XElement BuildNativeTable(string tableHtml)
        {
            var rows = TableRowRegex.Matches(tableHtml);
            if (rows.Count == 0)
                return null;

            // Parse all rows to determine column count and cell contents
            var tableData = new List<List<string>>();
            int maxCols = 0;

            foreach (Match rowMatch in rows)
            {
                var cells = TableCellRegex.Matches(rowMatch.Groups[1].Value);
                var row = new List<string>();
                foreach (Match cellMatch in cells)
                {
                    row.Add(cellMatch.Groups[1].Value.Trim());
                }
                if (row.Count > maxCols)
                    maxCols = row.Count;
                tableData.Add(row);
            }

            if (maxCols == 0)
                return null;

            // Build one:Table
            var table = new XElement(OneNs + "Table",
                new XAttribute("bordersVisible", "true"));

            // Column definitions — distribute width evenly
            var colWidth = Math.Max(40.0, 540.0 / maxCols);
            var columns = new XElement(OneNs + "Columns");
            for (int i = 0; i < maxCols; i++)
            {
                columns.Add(new XElement(OneNs + "Column",
                    new XAttribute("index", i.ToString()),
                    new XAttribute("width", colWidth.ToString(CultureInfo.InvariantCulture)),
                    new XAttribute("isLocked", "false")));
            }
            table.Add(columns);

            // Rows
            foreach (var rowData in tableData)
            {
                var row = new XElement(OneNs + "Row");
                for (int c = 0; c < maxCols; c++)
                {
                    var cellContent = c < rowData.Count ? rowData[c] : "";
                    var cell = new XElement(OneNs + "Cell",
                        new XElement(OneNs + "OEChildren",
                            new XElement(OneNs + "OE",
                                new XElement(OneNs + "T",
                                    new XCData(cellContent)))));
                    row.Add(cell);
                }
                table.Add(row);
            }

            return new XElement(OneNs + "OE", table);
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
