namespace MDNote
{
    using MDNote.Core;
    using MDNote.OneNote;
    using System;
    using System.Collections.Generic;
    using System.Net;

    internal enum ViewMode
    {
        Rendered,
        Source
    }

    internal class ToggleSourceCommand
    {
        private static readonly Dictionary<string, ViewMode> PageViewModes
            = new Dictionary<string, ViewMode>();

        private readonly IOneNoteInterop _interop;

        public ToggleSourceCommand(IOneNoteInterop interop)
        {
            _interop = interop;
        }

        public void Execute()
        {
            try
            {
                var pageId = _interop.GetActivePageId();
                if (string.IsNullOrEmpty(pageId))
                {
                    NotificationHelper.ShowWarning("No active page found.");
                    return;
                }

                var pageXml = _interop.GetPageContent(pageId);
                if (string.IsNullOrEmpty(pageXml))
                {
                    NotificationHelper.ShowWarning("Could not read page content.");
                    return;
                }

                var parser = new PageXmlParser(pageXml);
                var markdown = parser.GetStoredMarkdownSource();

                if (string.IsNullOrEmpty(markdown))
                {
                    NotificationHelper.ShowWarning(
                        "No stored markdown source. Render the page first (F5).");
                    return;
                }
                var currentMode = GetViewMode(pageId, parser);

                if (currentMode == ViewMode.Rendered)
                {
                    ShowSource(pageId, pageXml, markdown);
                    PageViewModes[pageId] = ViewMode.Source;
                    NotificationHelper.ShowSuccess("Source view");
                }
                else
                {
                    ShowRendered(pageId, markdown);
                    PageViewModes[pageId] = ViewMode.Rendered;
                    NotificationHelper.ShowSuccess("Rendered view");
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleError("Toggle source failed.", ex);
            }
        }

        /// <summary>
        /// Replace page content with raw markdown displayed in monospace font.
        /// Preserves metadata so the source can be recovered.
        /// </summary>
        private void ShowSource(string pageId, string currentXml, string markdown)
        {
            var builder = PageXmlBuilder.FromPageXml(currentXml);
            builder.ClearOutlines();

            // Render raw markdown as plain text with monospace styling
            var escapedMd = WebUtility.HtmlEncode(markdown)
                .Replace("\r\n", "<br/>")
                .Replace("\n", "<br/>");
            var sourceHtml =
                "<p style=\"font-family:Consolas,'Courier New',monospace;" +
                "font-size:10pt;white-space:pre-wrap;\">" +
                escapedMd + "</p>";
            builder.AddOutline(sourceHtml);

            builder.SetMeta("md-note-view-mode", "source");
            _interop.UpdatePageContent(builder.Build());
        }

        /// <summary>
        /// Re-render from stored source using the standard render pipeline.
        /// </summary>
        private void ShowRendered(string pageId, string markdown)
        {
            var converter = new MarkdownConverter();
            var result = converter.Convert(markdown, SettingsManager.Current.ToConversionOptions());
            var writer = new PageWriter(_interop);
            writer.RenderMarkdownToPage(pageId, result, markdown);

            // Set view mode meta (requires a second read/write since RenderMarkdownToPage
            // already called UpdatePageContent)
            var updatedXml = _interop.GetPageContent(pageId);
            var builder = PageXmlBuilder.FromPageXml(updatedXml);
            builder.SetMeta("md-note-view-mode", "rendered");
            _interop.UpdatePageContent(builder.Build());
        }

        private static ViewMode GetViewMode(string pageId, PageXmlParser parser)
        {
            // Check in-memory state first (fastest)
            if (PageViewModes.TryGetValue(pageId, out var mode))
                return mode;

            // Fall back to persisted metadata
            var metaMode = parser.GetMetaValue("md-note-view-mode");
            if (metaMode == "source")
                return ViewMode.Source;

            return ViewMode.Rendered;
        }

        /// <summary>
        /// Clears the in-memory view mode tracking. Called on add-in disconnect.
        /// </summary>
        internal static void Reset()
        {
            PageViewModes.Clear();
        }
    }
}
