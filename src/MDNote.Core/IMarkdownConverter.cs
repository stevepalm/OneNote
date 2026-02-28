namespace MDNote.Core
{
    /// <summary>
    /// Converts between Markdown text and OneNote XML.
    /// Stub for Session 1 — implementation in Session 2+.
    /// </summary>
    public interface IMarkdownConverter
    {
        /// <summary>
        /// Converts Markdown text to OneNote page XML fragment.
        /// </summary>
        string MarkdownToOneNoteXml(string markdown);

        /// <summary>
        /// Converts OneNote page XML to Markdown text.
        /// </summary>
        string OneNoteXmlToMarkdown(string pageXml);
    }
}
