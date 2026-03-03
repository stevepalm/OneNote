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
            CommandRunner.RunCommand(() =>
            {
                var interop = new OneNoteInterop(oneNoteApp);
                var command = new RenderCommand(interop);
                command.RenderPage();
            }, "Render Page");
        }

        /// <summary>
        /// Renders only the selected text on the active page.
        /// Falls back to full-page render if no selection is found.
        /// </summary>
        public static void OnRenderSelection(object oneNoteApp)
        {
            CommandRunner.RunCommand(() =>
            {
                var interop = new OneNoteInterop(oneNoteApp);
                var command = new RenderCommand(interop);
                command.RenderSelection();
            }, "Render Selection");
        }

        public static void OnToggleSource(object oneNoteApp)
        {
            CommandRunner.RunCommand(() =>
            {
                var interop = new OneNoteInterop(oneNoteApp);
                var command = new ToggleSourceCommand(interop);
                command.Execute();
            }, "Toggle Source");
        }

        public static void OnExportClipboard(object oneNoteApp)
        {
            CommandRunner.RunCommand(() =>
            {
                var interop = new OneNoteInterop(oneNoteApp);
                var command = new ExportCommand(interop);
                command.ExportToClipboard();
            }, "Export Clipboard");
        }

        public static void OnExportFile(object oneNoteApp)
        {
            CommandRunner.RunCommand(() =>
            {
                var interop = new OneNoteInterop(oneNoteApp);
                var command = new ExportCommand(interop);
                command.ExportToFile();
            }, "Export File");
        }

        public static void OnImportMarkdown(object oneNoteApp)
        {
            CommandRunner.RunCommand(() =>
            {
                var interop = new OneNoteInterop(oneNoteApp);
                var command = new ImportCommand(interop);
                command.ImportMarkdownFile();
            }, "Import Markdown");
        }

        public static void OnPasteRender(object oneNoteApp)
        {
            CommandRunner.RunCommand(() =>
            {
                var interop = new OneNoteInterop(oneNoteApp);
                var command = new ImportCommand(interop);
                command.PasteAndRender();
            }, "Paste & Render");
        }

        public static void OnToggleLiveMode(object oneNoteApp, LiveModeManager manager)
        {
            CommandRunner.RunCommand(() =>
            {
                manager.Toggle(oneNoteApp);
                var active = manager.IsActive;
                NotificationHelper.ShowSuccess(active ? "Live Mode ON" : "Live Mode OFF");
                InvalidateControl?.Invoke("btnLiveMode");
            }, "Toggle Live Mode");
        }

        public static void OnPasteFormatted(object oneNoteApp)
        {
            CommandRunner.RunCommand(() =>
            {
                var interop = new OneNoteInterop(oneNoteApp);
                var command = new FormatCommand(interop);
                command.PasteFormatted();
            }, "Paste Formatted");
        }

        public static void OnFormatPage(object oneNoteApp)
        {
            CommandRunner.RunCommand(() =>
            {
                var interop = new OneNoteInterop(oneNoteApp);
                var command = new FormatCommand(interop);
                command.FormatPage();
            }, "Format Page");
        }

        public static void OnInsertToc(object oneNoteApp)
        {
            CommandRunner.RunCommand(() =>
            {
                var interop = new OneNoteInterop(oneNoteApp);
                var command = new TocCommand(interop);
                command.Execute();
            }, "Insert TOC");
        }

        public static void OnOpenSettings(object oneNoteApp)
        {
            CommandRunner.RunDialog(() =>
            {
                SettingsForm.ShowSettingsDialog();
            });
        }

        public static void OnShowAbout()
        {
            CommandRunner.RunDialog(() =>
            {
                AboutDialog.ShowAboutDialog();
            });
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
