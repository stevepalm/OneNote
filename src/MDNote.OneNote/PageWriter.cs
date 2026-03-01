using MDNote.Core;
using MDNote.Core.Models;

namespace MDNote.OneNote
{
    /// <summary>
    /// Writes rendered markdown content to OneNote pages.
    /// </summary>
    public class PageWriter
    {
        private readonly IOneNoteInterop _interop;

        public PageWriter(IOneNoteInterop interop)
        {
            _interop = interop;
        }

        /// <summary>
        /// Full render: writes the complete markdown conversion result to the page.
        /// Sets title, stores markdown source as metadata, and adds the HTML outline.
        /// </summary>
        public void RenderMarkdownToPage(string pageId,
            ConversionResult result, string markdownSource)
        {
            var builder = new PageXmlBuilder(pageId);

            if (!string.IsNullOrEmpty(result.Title))
                builder.SetPageTitle(result.Title);

            var metaEntries = MarkdownSourceStorage.CreateMetaEntries(markdownSource);
            foreach (var entry in metaEntries)
                builder.AddMeta(entry.Key, entry.Value);

            var converter = new HtmlToOneNoteConverter();
            var oneNoteHtml = converter.ConvertForOneNote(result.Html);

            builder.AddOutline(oneNoteHtml);

            _interop.UpdatePageContent(builder.Build());
        }

        /// <summary>
        /// Updates a specific outline on the page without affecting other content.
        /// </summary>
        public void UpdateOutline(string pageId, string outlineId, string html)
        {
            var currentXml = _interop.GetPageContent(pageId);
            var builder = PageXmlBuilder.FromPageXml(currentXml);

            var converter = new HtmlToOneNoteConverter();
            var oneNoteHtml = converter.ConvertForOneNote(html);

            builder.ReplaceOutline(outlineId, oneNoteHtml);
            _interop.UpdatePageContent(builder.Build());
        }
    }
}
