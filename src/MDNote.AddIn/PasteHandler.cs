namespace MDNote
{
    using MDNote.Core;
    using MDNote.OneNote;
    using System;

    /// <summary>
    /// Decides what to do when clipboard markdown is detected, based on PasteMode setting.
    /// Auto → render immediately, Prompt → show popup, Off → do nothing.
    /// </summary>
    internal static class PasteHandler
    {
        public static void Handle(object oneNoteApp, string markdownText)
        {
            try
            {
                var mode = SettingsManager.Current.PasteMode;

                switch (mode)
                {
                    case PasteMode.Auto:
                        CommandRunner.TryRunCommand(
                            () => RenderMarkdown(oneNoteApp, markdownText),
                            "Paste Render");
                        break;

                    case PasteMode.Prompt:
                        PromptNotification.Show(
                            onRender: () => CommandRunner.TryRunCommand(
                                () => RenderMarkdown(oneNoteApp, markdownText),
                                "Paste Render"),
                            onAlwaysRender: () =>
                            {
                                SettingsManager.Current.PasteMode = PasteMode.Auto;
                                SettingsManager.Instance.Save();
                                CommandRunner.TryRunCommand(
                                    () => RenderMarkdown(oneNoteApp, markdownText),
                                    "Paste Render");
                            });
                        break;

                    case PasteMode.Off:
                        return;
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleError("Auto-paste failed.", ex);
            }
        }

        private static void RenderMarkdown(object oneNoteApp, string markdownText)
        {
            try
            {
                var interop = new OneNoteInterop(oneNoteApp);
                var pageId = interop.GetActivePageId();

                if (string.IsNullOrEmpty(pageId))
                {
                    ErrorHandler.LogWarning("Auto-paste: no active page.");
                    return;
                }

                // Skip if the page is in source view mode
                var pageXml = interop.GetPageContent(pageId);
                if (!string.IsNullOrEmpty(pageXml))
                {
                    var parser = new PageXmlParser(pageXml);
                    var viewMode = parser.GetMetaValue("md-note-view-mode");
                    if (viewMode == "source")
                    {
                        ErrorHandler.Log("Auto-paste skipped: page is in source mode.");
                        return;
                    }
                }

                var converter = new MarkdownConverter();
                var result = converter.Convert(markdownText, SettingsManager.Current.ToConversionOptions());

                var writer = new PageWriter(interop);
                writer.RenderMarkdownToPage(pageId, result, markdownText);

                NotificationHelper.ShowSuccess("Auto-rendered pasted markdown");
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleError("Auto-render failed.", ex);
            }
        }
    }
}
