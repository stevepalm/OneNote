namespace MDNote
{
    using MDNote.OneNote;
    using System;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using System.Windows.Forms;

    internal static class RibbonHandler
    {
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

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

        /// <summary>
        /// Stub for features not yet implemented.
        /// </summary>
        public static void ShowStub(string featureName, int sessionNumber)
        {
            ShowForegroundMessageBox(
                $"MD Note: {featureName} — Session {sessionNumber}",
                "MD Note",
                MessageBoxIcon.Information);
        }

        private static void ShowForegroundMessageBox(string text, string caption, MessageBoxIcon icon)
        {
            // dllhost.exe runs out-of-process; use NativeWindow as owner so
            // the MessageBox appears in front of OneNote.
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
