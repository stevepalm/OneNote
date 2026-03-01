using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using MDNote.Core.Models;

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

        /// <summary>
        /// Extracts plain text from all outline elements, stripping HTML tags.
        /// Does NOT include title text.
        /// Multiple T elements within the same OE are concatenated (same line).
        /// Separate OE elements become separate lines.
        /// </summary>
        public string GetOutlinePlainText()
        {
            var lines = new List<string>();

            foreach (var oe in _page.Elements(OneNs + "Outline")
                .Descendants(OneNs + "OE"))
            {
                // T elements within the same OE are segments of the same line
                var tElements = oe.Elements(OneNs + "T");
                var lineText = string.Concat(
                    tElements.Select(t => StripHtmlTags(t.Value)));

                lines.Add(lineText);
            }

            return string.Join("\n", lines);
        }

        /// <summary>
        /// Extracts embedded image data from the page XML.
        /// </summary>
        public List<ImageInfo> GetImages()
        {
            var images = new List<ImageInfo>();
            int index = 0;

            foreach (var img in _page.Descendants(OneNs + "Image"))
            {
                var data = img.Element(OneNs + "Data")?.Value?.Trim();
                if (string.IsNullOrEmpty(data))
                    continue;

                var callbackId = img.Element(OneNs + "CallbackID")
                    ?.Attribute("callbackID")?.Value;
                var format = img.Attribute("format")?.Value ?? "png";

                var fileName = !string.IsNullOrEmpty(callbackId)
                    ? Path.GetFileName(callbackId)
                    : $"image-{index}.{format}";

                images.Add(new ImageInfo
                {
                    Id = img.Attribute("objectID")?.Value ?? $"img-{index}",
                    FileName = fileName,
                    Data = System.Convert.FromBase64String(data),
                    OriginalReference = callbackId
                });
                index++;
            }

            return images;
        }

        private static readonly Regex HtmlTagRegex = new Regex(
            @"<[^>]+>", RegexOptions.Compiled);

        private static string StripHtmlTags(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            var stripped = HtmlTagRegex.Replace(input, "");
            return WebUtility.HtmlDecode(stripped).Trim();
        }

        private static double ParseDouble(string value)
        {
            return double.TryParse(value, NumberStyles.Float,
                CultureInfo.InvariantCulture, out var d) ? d : 0.0;
        }
    }
}
