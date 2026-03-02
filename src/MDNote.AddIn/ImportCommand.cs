namespace MDNote
{
    using MDNote.Core;
    using MDNote.OneNote;
    using System;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Windows.Forms;

    internal class ImportCommand
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        private readonly IOneNoteInterop _interop;

        public ImportCommand(IOneNoteInterop interop)
        {
            _interop = interop;
        }

        /// <summary>
        /// Opens a file dialog, reads a .md file, creates a new OneNote page,
        /// renders the markdown, and navigates to the new page.
        /// </summary>
        public void ImportMarkdownFile()
        {
            try
            {
                var dialog = new OpenFileDialog
                {
                    Title = "Import Markdown File",
                    Filter = "Markdown files (*.md;*.markdown;*.txt)|*.md;*.markdown;*.txt|All files (*.*)|*.*",
                    DefaultExt = "md"
                };

                var ownerHandle = GetForegroundWindow();
                var owner = new NativeWindow();
                DialogResult dialogResult;
                try
                {
                    owner.AssignHandle(ownerHandle);
                    dialogResult = dialog.ShowDialog(owner);
                }
                finally
                {
                    owner.ReleaseHandle();
                }

                if (dialogResult != DialogResult.OK)
                    return;

                var markdown = File.ReadAllText(dialog.FileName, System.Text.Encoding.UTF8);
                if (string.IsNullOrWhiteSpace(markdown))
                {
                    NotificationHelper.ShowWarning("File is empty.");
                    return;
                }

                var sectionId = _interop.GetCurrentSectionId();
                if (string.IsNullOrEmpty(sectionId))
                {
                    NotificationHelper.ShowWarning("Could not determine current section.");
                    return;
                }

                var newPageId = _interop.CreateNewPage(sectionId);
                if (string.IsNullOrEmpty(newPageId))
                {
                    NotificationHelper.ShowWarning("Could not create new page.");
                    return;
                }

                var converter = new MarkdownConverter();
                var result = converter.Convert(markdown, SettingsManager.Current.ToConversionOptions());

                var writer = new PageWriter(_interop);
                try
                {
                    writer.RenderMarkdownToPage(newPageId, result, markdown);
                }
                catch (COMException comEx)
                    when ((uint)comEx.ErrorCode == OneNoteInterop.HR_INSERTING_HTML)
                {
                    // Content too large with source — retry without embedded source
                    try
                    {
                        writer.RenderMarkdownToPageWithoutSource(newPageId, result);
                        _interop.NavigateToPage(newPageId);
                        NotificationHelper.ShowWarning(
                            $"Imported: {Path.GetFileName(dialog.FileName)}\n" +
                            "(Source not stored \u2014 file too large for round-trip editing)");
                        return;
                    }
                    catch (Exception)
                    {
                        // Even without source it failed — clean up and rethrow
                        TryDeletePage(newPageId);
                        throw;
                    }
                }
                catch (Exception)
                {
                    TryDeletePage(newPageId);
                    throw;
                }

                _interop.NavigateToPage(newPageId);

                NotificationHelper.ShowSuccess(
                    $"Imported: {Path.GetFileName(dialog.FileName)}");
            }
            catch (Exception ex)
            {
                var diagInfo = !string.IsNullOrEmpty(PageWriter.LastDiagnosticPath)
                    ? $"\n\nDiagnostic dump saved to:\n{PageWriter.LastDiagnosticPath}"
                    : "";
                ErrorHandler.HandleError($"Import failed.{diagInfo}", ex);
            }
        }

        /// <summary>
        /// Reads clipboard text, detects markdown, and renders on current page.
        /// Shows confirmation if text doesn't appear to be markdown.
        /// </summary>
        public void PasteAndRender()
        {
            try
            {
                var text = GetClipboardText();
                if (string.IsNullOrWhiteSpace(text))
                {
                    NotificationHelper.ShowWarning("Clipboard has no text content.");
                    return;
                }

                var detection = MarkdownDetector.Detect(text);
                if (!detection.IsMarkdown)
                {
                    var answer = ShowConfirmation(
                        $"Clipboard text doesn't appear to be Markdown " +
                        $"(score: {detection.Score:F2}).\n\nRender anyway?");
                    if (answer != DialogResult.Yes)
                        return;
                }

                var pageId = _interop.GetActivePageId();
                if (string.IsNullOrEmpty(pageId))
                {
                    NotificationHelper.ShowWarning("No active page found.");
                    return;
                }

                var converter = new MarkdownConverter();
                var result = converter.Convert(text, SettingsManager.Current.ToConversionOptions());

                var writer = new PageWriter(_interop);
                writer.RenderMarkdownToPage(pageId, result, text);

                NotificationHelper.ShowSuccess("Pasted & rendered");
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleError("Paste & render failed.", ex);
            }
        }

        private static string GetClipboardText()
        {
            string text = null;

            if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
            {
                text = Clipboard.ContainsText()
                    ? Clipboard.GetText(TextDataFormat.UnicodeText)
                    : null;
            }
            else
            {
                var thread = new Thread(() =>
                {
                    text = Clipboard.ContainsText()
                        ? Clipboard.GetText(TextDataFormat.UnicodeText)
                        : null;
                });
                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
                thread.Join();
            }

            return text;
        }

        private void TryDeletePage(string pageId)
        {
            try { _interop.DeletePage(pageId); }
            catch { /* best-effort cleanup */ }
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
