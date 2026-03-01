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
        /// Loads existing page XML to preserve structure, clears old outlines,
        /// upserts metadata, and adds the new rendered outline.
        /// </summary>
        public void RenderMarkdownToPage(string pageId,
            ConversionResult result, string markdownSource)
        {
            var currentXml = _interop.GetPageContent(pageId);

            // Save state for undo on failure
            PageStateBackup.Save(pageId, currentXml);

            var builder = PageXmlBuilder.FromPageXml(currentXml);

            if (!string.IsNullOrEmpty(result.Title))
                builder.SetPageTitle(result.Title);

            // Upsert metadata (won't create duplicates on re-render)
            var metaEntries = MarkdownSourceStorage.CreateMetaEntries(markdownSource);
            foreach (var entry in metaEntries)
                builder.SetMeta(entry.Key, entry.Value);

            var converter = new HtmlToOneNoteConverter();
            var oneNoteHtml = converter.ConvertForOneNote(result.Html);

            // Replace content of existing outline in-place (preserves objectID).
            // UpdatePageContent is a merge — removing outlines from XML doesn't
            // delete them; we must replace their content to avoid duplicates.
            builder.ReplaceOrAddRenderedOutline(oneNoteHtml);

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
