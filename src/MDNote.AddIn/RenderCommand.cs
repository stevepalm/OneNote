namespace MDNote
{
    using MDNote.Core;
    using MDNote.OneNote;
    using System;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Windows.Forms;
    using System.Xml.Linq;

    /// <summary>
    /// Orchestrates the markdown render pipeline:
    /// extract text → detect markdown → store source → convert → write back.
    /// </summary>
    internal class RenderCommand
    {
        private static readonly XNamespace OneNs =
            "http://schemas.microsoft.com/office/onenote/2013/onenote";

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        private readonly IOneNoteInterop _interop;

        public RenderCommand(IOneNoteInterop interop)
        {
            _interop = interop;
        }

        /// <summary>
        /// Renders the entire active page. If markdown source is already stored
        /// in metadata (re-render), uses the stored source. Otherwise extracts
        /// text, detects markdown, and performs initial render.
        /// </summary>
        public void RenderPage()
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
                var storedSource = parser.GetMetaValue(MarkdownSourceStorage.MetaSource);

                string markdown;

                if (!string.IsNullOrEmpty(storedSource))
                {
                    // Re-render from stored source
                    markdown = MarkdownSourceStorage.DecodeSource(storedSource);
                    ErrorHandler.Log("Re-rendering from stored markdown source.");
                }
                else
                {
                    // First render: extract text and detect markdown
                    var plainText = parser.GetOutlinePlainText();
                    if (string.IsNullOrWhiteSpace(plainText))
                    {
                        NotificationHelper.ShowWarning("Page has no text content.");
                        return;
                    }

                    var detection = MarkdownDetector.Detect(plainText);
                    if (!detection.IsMarkdown)
                    {
                        var answer = ShowConfirmation(
                            $"This page doesn't appear to contain Markdown " +
                            $"(score: {detection.Score:F2}).\n\nRender anyway?");
                        if (answer != DialogResult.Yes)
                            return;
                    }

                    markdown = plainText;
                    ErrorHandler.Log($"First render. Detection score: {detection.Score:F2}");
                }

                var converter = new MarkdownConverter();
                var result = converter.Convert(markdown);

                var writer = new PageWriter(_interop);
                writer.RenderMarkdownToPage(pageId, result, markdown);

                NotificationHelper.ShowSuccess("Rendered");
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleError("Render failed. Your content is safe.", ex);
            }
        }

        /// <summary>
        /// Renders only the selected text on the active page.
        /// Falls back to full-page render if no selection is found.
        /// </summary>
        public void RenderSelection()
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

                var page = XElement.Parse(pageXml);

                // Find selected outlines or OE elements
                var selectedOutline = page.Elements(OneNs + "Outline")
                    .FirstOrDefault(o => o.Descendants()
                        .Any(d => d.Attribute("selected")?.Value == "all"));

                if (selectedOutline == null)
                {
                    // No selection found — fall back to full page render
                    ErrorHandler.Log("No selection found, falling back to full page render.");
                    RenderPage();
                    return;
                }

                // Extract text from selected elements
                var selectedTexts = selectedOutline
                    .Descendants(OneNs + "OE")
                    .Where(oe => oe.Attribute("selected")?.Value == "all")
                    .SelectMany(oe => oe.Elements(OneNs + "T"))
                    .Select(t => StripHtmlTags(t.Value));

                var selectedText = string.Join("\n", selectedTexts);

                if (string.IsNullOrWhiteSpace(selectedText))
                {
                    // If no OE-level selection, try all T elements in the selected outline
                    selectedTexts = selectedOutline
                        .Descendants(OneNs + "T")
                        .Select(t => StripHtmlTags(t.Value));
                    selectedText = string.Join("\n", selectedTexts);
                }

                if (string.IsNullOrWhiteSpace(selectedText))
                {
                    NotificationHelper.ShowWarning("Selected text is empty.");
                    return;
                }

                var outlineId = selectedOutline.Attribute("objectID")?.Value
                                ?? selectedOutline.Attribute("ID")?.Value;

                // Store source in page metadata before overwriting
                var parser = new PageXmlParser(pageXml);
                if (string.IsNullOrEmpty(parser.GetMetaValue(MarkdownSourceStorage.MetaSource)))
                {
                    // Store the full page text as source on first render
                    var fullText = parser.GetOutlinePlainText();
                    StoreSourceMeta(pageId, pageXml, fullText);
                }

                var converter = new MarkdownConverter();
                var result = converter.Convert(selectedText);

                var writer = new PageWriter(_interop);
                writer.UpdateOutline(pageId, outlineId, result.Html);

                NotificationHelper.ShowSuccess("Selection rendered");
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleError("Selection render failed. Your content is safe.", ex);
            }
        }

        private void StoreSourceMeta(string pageId, string currentXml, string markdown)
        {
            var builder = PageXmlBuilder.FromPageXml(currentXml);
            var metaEntries = MarkdownSourceStorage.CreateMetaEntries(markdown);
            foreach (var entry in metaEntries)
                builder.SetMeta(entry.Key, entry.Value);
            _interop.UpdatePageContent(builder.Build());
        }

        private static string StripHtmlTags(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            var stripped = System.Text.RegularExpressions.Regex.Replace(
                input, @"<[^>]+>", "");
            return System.Net.WebUtility.HtmlDecode(stripped).Trim();
        }

        private static DialogResult ShowConfirmation(string message)
        {
            var ownerHandle = GetForegroundWindow();
            var owner = new NativeWindow();
            try
            {
                owner.AssignHandle(ownerHandle);
                return MessageBox.Show(owner, message, "MD Note",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            }
            finally
            {
                owner.ReleaseHandle();
            }
        }
    }
}
