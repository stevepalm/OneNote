using System.Linq;
using MDNote.Core;

namespace MDNote.OneNote
{
    /// <summary>
    /// Extracts markdown from a OneNote page. Uses stored source when available,
    /// falls back to reverse conversion from rendered HTML.
    /// </summary>
    public class MarkdownExporter
    {
        private readonly IOneNoteInterop _interop;

        public MarkdownExporter(IOneNoteInterop interop)
        {
            _interop = interop;
        }

        /// <summary>
        /// Gets the markdown text for a page. Returns stored source if available,
        /// otherwise performs best-effort reverse conversion.
        /// </summary>
        public string GetMarkdown(string pageId)
        {
            var reader = new PageReader(_interop);
            var stored = reader.GetStoredMarkdownSource(pageId);
            if (!string.IsNullOrEmpty(stored))
                return stored;

            return ReverseConvert(pageId);
        }

        /// <summary>
        /// Performs reverse conversion from OneNote HTML to markdown.
        /// Always uses OneNoteHtmlToMarkdown regardless of stored source.
        /// </summary>
        public string ReverseConvert(string pageId)
        {
            var pageXml = _interop.GetPageContent(pageId);
            if (string.IsNullOrEmpty(pageXml))
                return null;

            var parser = new PageXmlParser(pageXml);
            var outlines = parser.GetOutlines();
            if (outlines.Count == 0)
                return null;

            var allHtml = outlines.SelectMany(o => o.HtmlContent).ToList();
            var reverseConverter = new OneNoteHtmlToMarkdown();
            return reverseConverter.Convert(allHtml);
        }
    }
}
