namespace MDNote
{
    using MDNote.Core.Models;
    using MDNote.OneNote;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Windows.Forms;

    internal class ExportCommand
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        private readonly IOneNoteInterop _interop;

        public ExportCommand(IOneNoteInterop interop)
        {
            _interop = interop;
        }

        /// <summary>
        /// Copies markdown to clipboard. Uses stored source if available,
        /// otherwise performs best-effort reverse conversion.
        /// </summary>
        public void ExportToClipboard()
        {
            try
            {
                var pageId = _interop.GetActivePageId();
                if (string.IsNullOrEmpty(pageId))
                {
                    NotificationHelper.ShowWarning("No active page found.");
                    return;
                }

                var exporter = new MarkdownExporter(_interop);
                var markdown = exporter.GetMarkdown(pageId);
                if (string.IsNullOrEmpty(markdown))
                {
                    NotificationHelper.ShowWarning("No content to export.");
                    return;
                }

                SetClipboardText(markdown);
                NotificationHelper.ShowSuccess("Markdown copied to clipboard");
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleError("Export to clipboard failed.", ex);
            }
        }

        /// <summary>
        /// Exports markdown to a .md file via SaveFileDialog.
        /// Extracts embedded images to an /images/ subfolder.
        /// </summary>
        public void ExportToFile()
        {
            try
            {
                var pageId = _interop.GetActivePageId();
                if (string.IsNullOrEmpty(pageId))
                {
                    NotificationHelper.ShowWarning("No active page found.");
                    return;
                }

                var exporter = new MarkdownExporter(_interop);
                var markdown = exporter.GetMarkdown(pageId);
                if (string.IsNullOrEmpty(markdown))
                {
                    NotificationHelper.ShowWarning("No content to export.");
                    return;
                }

                var title = _interop.GetPageTitle(pageId) ?? "untitled";
                var safeName = SanitizeFileName(title);

                var dialog = new SaveFileDialog
                {
                    Title = "Export Markdown",
                    Filter = "Markdown files (*.md)|*.md|All files (*.*)|*.*",
                    FileName = $"{safeName}.md",
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

                var filePath = dialog.FileName;
                var fileDir = Path.GetDirectoryName(filePath);

                // Extract and save embedded images
                var pageXml = _interop.GetPageContent(pageId);
                if (!string.IsNullOrEmpty(pageXml))
                {
                    var parser = new PageXmlParser(pageXml);
                    var images = parser.GetImages();

                    if (images.Count > 0)
                    {
                        var imagesDir = Path.Combine(fileDir, "images");
                        if (!Directory.Exists(imagesDir))
                            Directory.CreateDirectory(imagesDir);

                        foreach (var img in images)
                        {
                            var imgPath = Path.Combine(imagesDir, img.FileName);
                            File.WriteAllBytes(imgPath, img.Data);
                        }

                        markdown = RewriteImageReferences(markdown, images);
                    }
                }

                File.WriteAllText(filePath, markdown, System.Text.Encoding.UTF8);
                NotificationHelper.ShowSuccess($"Exported to {Path.GetFileName(filePath)}");
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleError("Export to file failed.", ex);
            }
        }

        private static string RewriteImageReferences(
            string markdown, List<ImageInfo> images)
        {
            foreach (var img in images)
            {
                if (!string.IsNullOrEmpty(img.OriginalReference))
                {
                    markdown = markdown.Replace(
                        img.OriginalReference,
                        $"images/{img.FileName}");
                }
            }
            return markdown;
        }

        internal static string SanitizeFileName(string name)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var sanitized = new string(name.Select(c =>
                Array.IndexOf(invalid, c) >= 0 ? '_' : c).ToArray());
            return sanitized.Length > 50 ? sanitized.Substring(0, 50) : sanitized;
        }

        /// <summary>
        /// Sets clipboard text, handling STA thread requirements for dllhost.exe.
        /// </summary>
        private static void SetClipboardText(string text)
        {
            if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
            {
                Clipboard.SetText(text, TextDataFormat.UnicodeText);
            }
            else
            {
                var thread = new Thread(() =>
                    Clipboard.SetText(text, TextDataFormat.UnicodeText));
                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
                thread.Join();
            }
        }
    }
}
