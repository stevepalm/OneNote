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
        /// Source is stored in a dedicated OE element, separate from content,
        /// to avoid CDATA size issues with large documents.
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

            var converter = new HtmlToOneNoteConverter();
            var oneNoteHtml = converter.ConvertForOneNote(result.Html);

            // Replace content of existing outline in-place (preserves objectID).
            // UpdatePageContent is a merge — removing outlines from XML doesn't
            // delete them; we must replace their content to avoid duplicates.
            builder.ReplaceOrAddRenderedOutline(oneNoteHtml);

            // Embed markdown source as a dedicated OE at the end of the outline.
            // Kept separate from content to avoid bloating individual CDATA sections.
            var sourceTag = MarkdownSourceStorage.BuildHiddenSourceHtml(markdownSource);
            builder.AppendSourceToOutline(sourceTag);

            _interop.UpdatePageContent(builder.Build());
        }

        /// <summary>
        /// Renders markdown content to the page WITHOUT embedding the source.
        /// Used as a fallback when source storage causes CDATA overflow (0x80042009).
        /// </summary>
        public void RenderMarkdownToPageWithoutSource(string pageId,
            ConversionResult result)
        {
            var currentXml = _interop.GetPageContent(pageId);
            PageStateBackup.Save(pageId, currentXml);

            var builder = PageXmlBuilder.FromPageXml(currentXml);

            if (!string.IsNullOrEmpty(result.Title))
                builder.SetPageTitle(result.Title);

            var converter = new HtmlToOneNoteConverter();
            var oneNoteHtml = converter.ConvertForOneNote(result.Html);

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

        /// <summary>
        /// Updates a specific outline and appends source storage as a separate OE.
        /// </summary>
        public void UpdateOutlineWithSource(string pageId, string outlineId,
            string html, string markdownSource)
        {
            var currentXml = _interop.GetPageContent(pageId);
            var builder = PageXmlBuilder.FromPageXml(currentXml);

            var converter = new HtmlToOneNoteConverter();
            var oneNoteHtml = converter.ConvertForOneNote(html);

            builder.ReplaceOutline(outlineId, oneNoteHtml);

            if (!string.IsNullOrEmpty(markdownSource))
            {
                var sourceTag = MarkdownSourceStorage.BuildHiddenSourceHtml(markdownSource);
                builder.AppendSourceToOutline(sourceTag);
            }

            _interop.UpdatePageContent(builder.Build());
        }
    }
}
