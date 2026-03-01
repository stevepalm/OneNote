namespace MDNote
{
    using MDNote.Core;
    using MDNote.Core.Models;
    using MDNote.OneNote;
    using System;

    /// <summary>
    /// Inserts a Table of Contents at the top of a rendered markdown page.
    /// Re-renders with EnableTableOfContents=true using the existing pipeline.
    /// </summary>
    internal class TocCommand
    {
        private readonly IOneNoteInterop _interop;

        public TocCommand(IOneNoteInterop interop)
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

                // Build options with TOC enabled
                var options = SettingsManager.Current.ToConversionOptions();
                options.EnableTableOfContents = true;

                var converter = new MarkdownConverter();
                var result = converter.Convert(markdown, options);

                if (result.Headings == null || result.Headings.Count == 0)
                {
                    NotificationHelper.ShowWarning("No headings found in document.");
                    return;
                }

                var writer = new PageWriter(_interop);
                writer.RenderMarkdownToPage(pageId, result, markdown);

                NotificationHelper.ShowSuccess("Table of Contents inserted");
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleError("Insert TOC failed.", ex);
            }
        }
    }
}
