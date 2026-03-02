using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace MDNote.OneNote.Tests.Helpers
{
    /// <summary>
    /// Manual mock of IOneNoteInterop for unit testing commands
    /// without a live OneNote COM connection.
    /// </summary>
    internal class FakeOneNoteInterop : IOneNoteInterop
    {
        private static readonly XNamespace OneNs =
            "http://schemas.microsoft.com/office/onenote/2013/onenote";

        public string ActivePageId { get; set; } = "{page-1}";
        public Dictionary<string, string> Pages { get; } = new Dictionary<string, string>();
        public string LastUpdatedXml { get; private set; }
        public string CurrentSectionIdValue { get; set; } = "{section-1}";
        public string NextNewPageId { get; set; } = "{new-page-1}";
        public string LastNavigatedPageId { get; private set; }

        public string GetActivePageId() => ActivePageId;

        public string GetPageContent(string pageId)
        {
            return Pages.TryGetValue(pageId, out var xml) ? xml : null;
        }

        public string GetPageTitle(string pageId)
        {
            var xml = GetPageContent(pageId);
            if (string.IsNullOrEmpty(xml))
                return "(no content)";

            var page = XElement.Parse(xml);
            var title = page
                .Elements(OneNs + "Title")
                .Elements(OneNs + "OE")
                .Elements(OneNs + "T")
                .Select(t => t.Value)
                .FirstOrDefault();

            return title ?? "(untitled)";
        }

        public string GetPagePlainText(string pageId)
        {
            var xml = GetPageContent(pageId);
            if (string.IsNullOrEmpty(xml))
                return string.Empty;

            var page = XElement.Parse(xml);
            var texts = page
                .Descendants(OneNs + "T")
                .Select(t => t.Value);

            return string.Join("\n", texts);
        }

        public void UpdatePageContent(string xml)
        {
            LastUpdatedXml = xml;

            // Store back into Pages by extracting page ID from XML
            if (!string.IsNullOrEmpty(xml))
            {
                var page = XElement.Parse(xml);
                var id = page.Attribute("ID")?.Value;
                if (!string.IsNullOrEmpty(id))
                    Pages[id] = xml;
            }
        }

        public void NavigateToPage(string pageId)
        {
            LastNavigatedPageId = pageId;
        }

        public string GetCurrentSectionId() => CurrentSectionIdValue;

        public string CreateNewPage(string sectionId)
        {
            if (string.IsNullOrEmpty(sectionId))
                return null;

            // Create a minimal empty page XML
            var pageXml = new XElement(OneNs + "Page",
                new XAttribute("ID", NextNewPageId),
                new XElement(OneNs + "Title",
                    new XElement(OneNs + "OE",
                        new XElement(OneNs + "T", "")))).ToString();

            Pages[NextNewPageId] = pageXml;
            return NextNewPageId;
        }

        public List<string> DeletedPageIds { get; } = new List<string>();

        public void DeletePage(string pageId)
        {
            if (!string.IsNullOrEmpty(pageId))
            {
                Pages.Remove(pageId);
                DeletedPageIds.Add(pageId);
            }
        }
    }
}
