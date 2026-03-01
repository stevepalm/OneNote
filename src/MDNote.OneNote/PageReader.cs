using System.Collections.Generic;
using MDNote.Core;

namespace MDNote.OneNote
{
    /// <summary>
    /// Reads and parses OneNote page content into structured data.
    /// </summary>
    public class PageReader
    {
        private readonly IOneNoteInterop _interop;

        public PageReader(IOneNoteInterop interop)
        {
            _interop = interop;
        }

        public List<OutlineInfo> GetPageOutlines(string pageId)
        {
            var xml = _interop.GetPageContent(pageId);
            if (string.IsNullOrEmpty(xml))
                return new List<OutlineInfo>();

            return new PageXmlParser(xml).GetOutlines();
        }

        public string GetStoredMarkdownSource(string pageId)
        {
            var xml = _interop.GetPageContent(pageId);
            if (string.IsNullOrEmpty(xml))
                return null;

            var parser = new PageXmlParser(xml);
            var encoded = parser.GetMetaValue(MarkdownSourceStorage.MetaSource);
            if (string.IsNullOrEmpty(encoded))
                return null;

            return MarkdownSourceStorage.DecodeSource(encoded);
        }

        public bool HasStoredSource(string pageId)
        {
            var xml = _interop.GetPageContent(pageId);
            if (string.IsNullOrEmpty(xml))
                return false;

            var parser = new PageXmlParser(xml);
            return !string.IsNullOrEmpty(
                parser.GetMetaValue(MarkdownSourceStorage.MetaSource));
        }

        public string GetPageTitle(string pageId)
        {
            var xml = _interop.GetPageContent(pageId);
            if (string.IsNullOrEmpty(xml))
                return null;

            return new PageXmlParser(xml).GetTitle();
        }
    }
}
