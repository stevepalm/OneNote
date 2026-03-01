using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;

namespace MDNote.OneNote
{
    /// <summary>
    /// Parses OneNote page XML to extract structured content.
    /// </summary>
    public class PageXmlParser
    {
        private static readonly XNamespace OneNs =
            "http://schemas.microsoft.com/office/onenote/2013/onenote";

        private readonly XElement _page;

        public PageXmlParser(string pageXml)
        {
            if (string.IsNullOrEmpty(pageXml))
                throw new ArgumentNullException(nameof(pageXml));

            _page = XElement.Parse(pageXml);
        }

        public string GetPageId()
        {
            return _page.Attribute("ID")?.Value;
        }

        public string GetTitle()
        {
            return _page
                .Elements(OneNs + "Title")
                .Elements(OneNs + "OE")
                .Elements(OneNs + "T")
                .Select(t => t.Value)
                .FirstOrDefault();
        }

        public List<OutlineInfo> GetOutlines()
        {
            return _page.Elements(OneNs + "Outline")
                .Select(outline =>
                {
                    var pos = outline.Element(OneNs + "Position");

                    return new OutlineInfo
                    {
                        Id = outline.Attribute("objectID")?.Value
                             ?? outline.Attribute("ID")?.Value,
                        X = ParseDouble(pos?.Attribute("x")?.Value),
                        Y = ParseDouble(pos?.Attribute("y")?.Value),
                        HtmlContent = outline
                            .Descendants(OneNs + "T")
                            .Select(t => t.Value)
                            .ToList()
                    };
                })
                .ToList();
        }

        public OutlineInfo FindOutlineById(string outlineId)
        {
            return GetOutlines().FirstOrDefault(o => o.Id == outlineId);
        }

        public OutlineInfo FindOutlineByContent(string searchText)
        {
            return GetOutlines().FirstOrDefault(o =>
                o.HtmlContent.Any(h =>
                    h.IndexOf(searchText, StringComparison.Ordinal) >= 0));
        }

        public string GetMetaValue(string name)
        {
            return _page.Elements(OneNs + "Meta")
                .FirstOrDefault(m => m.Attribute("name")?.Value == name)
                ?.Attribute("content")?.Value;
        }

        public Dictionary<string, string> GetAllMeta()
        {
            return _page.Elements(OneNs + "Meta")
                .ToDictionary(
                    m => m.Attribute("name")?.Value ?? "",
                    m => m.Attribute("content")?.Value ?? "");
        }

        private static double ParseDouble(string value)
        {
            return double.TryParse(value, NumberStyles.Float,
                CultureInfo.InvariantCulture, out var d) ? d : 0.0;
        }
    }
}
