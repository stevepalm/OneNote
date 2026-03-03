namespace MDNote
{
    using MDNote.Core;
    using MDNote.OneNote;
    using System;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Windows.Forms;

    internal class FormatCommand
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        private readonly IOneNoteInterop _interop;

        public FormatCommand(IOneNoteInterop interop)
        {
            _interop = interop;
        }

        /// <summary>
        /// Reads HTML from clipboard (CF_HTML format), converts to Markdown via
        /// WebHtmlToMarkdown, then renders through the standard MD Note pipeline.
        /// Falls back to plain text clipboard if no HTML is available.
        /// </summary>
        public void PasteFormatted()
        {
            try
            {
                string htmlClipboard = null;
                string plainText = null;
                GetClipboardContent(out htmlClipboard, out plainText);

                string markdown;

                if (!string.IsNullOrEmpty(htmlClipboard))
                {
                    // Extract the HTML fragment from CF_HTML format
                    var fragment = CfHtmlParser.ExtractFragment(htmlClipboard);
                    if (string.IsNullOrWhiteSpace(fragment))
                    {
                        NotificationHelper.ShowWarning("Could not extract HTML from clipboard.");
                        return;
                    }

                    // Convert web HTML → Markdown
                    var webConverter = new WebHtmlToMarkdown();
                    markdown = webConverter.Convert(fragment);
                }
                else if (!string.IsNullOrEmpty(plainText))
                {
                    // Fallback: treat plain text as markdown
                    markdown = plainText;
                }
                else
                {
                    NotificationHelper.ShowWarning("Clipboard has no content.");
                    return;
                }

                if (string.IsNullOrWhiteSpace(markdown))
                {
                    NotificationHelper.ShowWarning("No content could be extracted from clipboard.");
                    return;
                }

                var pageId = _interop.GetActivePageId();
                if (string.IsNullOrEmpty(pageId))
                {
                    NotificationHelper.ShowWarning("No active page found.");
                    return;
                }

                // Save page state for potential undo
                var pageXml = _interop.GetPageContent(pageId);
                PageStateBackup.Save(pageId, pageXml);

                var converter = new MarkdownConverter();
                var result = converter.Convert(markdown, SettingsManager.Current.ToConversionOptions());

                var writer = new PageWriter(_interop);
                try
                {
                    writer.RenderMarkdownToPage(pageId, result, markdown);
                }
                catch (COMException comEx)
                    when ((uint)comEx.ErrorCode == OneNoteInterop.HR_INSERTING_HTML)
                {
                    // Content too large with source — retry without embedded source
                    writer.RenderMarkdownToPageWithoutSource(pageId, result);
                    NotificationHelper.ShowWarning(
                        "Pasted & formatted\n(Source not stored \u2014 content too large)");
                    return;
                }

                NotificationHelper.ShowSuccess("Pasted & formatted");
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleError("Paste formatted failed.", ex);
            }
        }

        /// <summary>
        /// Extracts CDATA HTML from the current page's outlines, converts to Markdown
        /// via WebHtmlToMarkdown, then re-renders through the standard MD Note pipeline.
        /// If the page was already rendered by MD Note (has stored source), re-renders
        /// from the stored source instead.
        /// </summary>
        public void FormatPage()
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
                var parser = new PageXmlParser(pageXml);

                // Check for stored markdown source (already rendered by MD Note)
                var storedSource = parser.GetStoredMarkdownSource();
                string markdown;

                if (!string.IsNullOrEmpty(storedSource))
                {
                    // Re-render from stored source
                    markdown = storedSource;
                }
                else
                {
                    // Extract CDATA HTML from page outlines
                    var cdataHtml = parser.GetOutlineCdataHtml();
                    if (string.IsNullOrWhiteSpace(cdataHtml))
                    {
                        NotificationHelper.ShowWarning("Page has no content to format.");
                        return;
                    }

                    // Convert OneNote-flavored HTML → Markdown
                    // CDATA HTML uses inline styles (font-size, font-weight, border-left)
                    // rather than semantic tags (<h2>, <strong>, <blockquote>),
                    // so use the OneNote-specific reverse converter.
                    var oneNoteConverter = new OneNoteHtmlToMarkdown();
                    markdown = oneNoteConverter.Convert(cdataHtml);

                    if (string.IsNullOrWhiteSpace(markdown))
                    {
                        NotificationHelper.ShowWarning("Could not extract formattable content.");
                        return;
                    }
                }

                // Save page state for potential undo
                PageStateBackup.Save(pageId, pageXml);

                var converter = new MarkdownConverter();
                var result = converter.Convert(markdown, SettingsManager.Current.ToConversionOptions());

                var writer = new PageWriter(_interop);
                try
                {
                    writer.RenderMarkdownToPage(pageId, result, markdown);
                }
                catch (COMException comEx)
                    when ((uint)comEx.ErrorCode == OneNoteInterop.HR_INSERTING_HTML)
                {
                    writer.RenderMarkdownToPageWithoutSource(pageId, result);
                    NotificationHelper.ShowWarning(
                        "Page formatted\n(Source not stored \u2014 content too large)");
                    return;
                }

                NotificationHelper.ShowSuccess("Page formatted");
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleError("Format page failed.", ex);
            }
        }

        private static void GetClipboardContent(out string htmlContent, out string plainText)
        {
            string html = null;
            string text = null;

            if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
            {
                html = Clipboard.ContainsText(TextDataFormat.Html)
                    ? Clipboard.GetText(TextDataFormat.Html)
                    : null;
                text = Clipboard.ContainsText(TextDataFormat.UnicodeText)
                    ? Clipboard.GetText(TextDataFormat.UnicodeText)
                    : null;
            }
            else
            {
                var thread = new Thread(() =>
                {
                    html = Clipboard.ContainsText(TextDataFormat.Html)
                        ? Clipboard.GetText(TextDataFormat.Html)
                        : null;
                    text = Clipboard.ContainsText(TextDataFormat.UnicodeText)
                        ? Clipboard.GetText(TextDataFormat.UnicodeText)
                        : null;
                });
                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
                thread.Join();
            }

            htmlContent = html;
            plainText = text;
        }
    }
}
