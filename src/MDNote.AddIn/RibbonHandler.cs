namespace MDNote
{
    using MDNote.OneNote;
    using System;
    using System.Runtime.InteropServices;
    using System.Windows.Forms;

    internal static class RibbonHandler
    {
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        /// <summary>
        /// Delegate set by AddIn.RibbonLoaded to allow commands to invalidate ribbon controls.
        /// </summary>
        internal static Action<string> InvalidateControl;

        /// <summary>
        /// Renders the entire active page from Markdown to OneNote rich text.
        /// </summary>
        public static void OnRenderPage(object oneNoteApp)
        {
            var interop = new OneNoteInterop(oneNoteApp);
            var command = new RenderCommand(interop);
            command.RenderPage();
        }

        /// <summary>
        /// Renders only the selected text on the active page.
        /// Falls back to full-page render if no selection is found.
        /// </summary>
        public static void OnRenderSelection(object oneNoteApp)
        {
            var interop = new OneNoteInterop(oneNoteApp);
            var command = new RenderCommand(interop);
            command.RenderSelection();
        }

        public static void OnToggleSource(object oneNoteApp)
        {
            var interop = new OneNoteInterop(oneNoteApp);
            var command = new ToggleSourceCommand(interop);
            command.Execute();
        }

        public static void OnExportClipboard(object oneNoteApp)
        {
            var interop = new OneNoteInterop(oneNoteApp);
            var command = new ExportCommand(interop);
            command.ExportToClipboard();
        }

        public static void OnExportFile(object oneNoteApp)
        {
            var interop = new OneNoteInterop(oneNoteApp);
            var command = new ExportCommand(interop);
            command.ExportToFile();
        }

        public static void OnImportMarkdown(object oneNoteApp)
        {
            var interop = new OneNoteInterop(oneNoteApp);
            var command = new ImportCommand(interop);
            command.ImportMarkdownFile();
        }

        public static void OnPasteRender(object oneNoteApp)
        {
            var interop = new OneNoteInterop(oneNoteApp);
            var command = new ImportCommand(interop);
            command.PasteAndRender();
        }

        public static void OnToggleLiveMode(object oneNoteApp, LiveModeManager manager)
        {
            manager.Toggle(oneNoteApp);
            var active = manager.IsActive;
            NotificationHelper.ShowSuccess(active ? "Live Mode ON" : "Live Mode OFF");
            InvalidateControl?.Invoke("btnLiveMode");
        }

        public static void OnInsertToc(object oneNoteApp)
        {
            var interop = new OneNoteInterop(oneNoteApp);
            var command = new TocCommand(interop);
            command.Execute();
        }

        public static void OnOpenSettings(object oneNoteApp)
        {
            SettingsForm.ShowSettingsDialog();
        }

        public static void OnShowAbout()
        {
            AboutDialog.ShowAboutDialog();
        }

        private static void ShowForegroundMessageBox(string text, string caption, MessageBoxIcon icon)
        {
            var ownerHandle = GetForegroundWindow();
            var owner = new NativeWindow();
            try
            {
                owner.AssignHandle(ownerHandle);
                MessageBox.Show(owner, text, caption, MessageBoxButtons.OK, icon);
            }
            finally
            {
                owner.ReleaseHandle();
            }
        }
    }
}
