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
        /// Proof-of-life: Gets the current page title via COM interop
        /// and displays it in a MessageBox.
        /// </summary>
        public static void OnRenderPage(object oneNoteApp)
        {
            try
            {
                var interop = new OneNoteInterop(oneNoteApp);
                var pageId = interop.GetActivePageId();

                if (string.IsNullOrEmpty(pageId))
                {
                    ShowForegroundMessageBox(
                        "No active page found.",
                        "MD Note",
                        MessageBoxIcon.Warning);
                    return;
                }

                var title = interop.GetPageTitle(pageId);

                ShowForegroundMessageBox(
                    $"Page title: {title}\nPage ID: {pageId}",
                    "MD Note — Render Page (Session 1)",
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                ShowForegroundMessageBox(
                    $"Error: {ex.Message}",
                    "MD Note — Error",
                    MessageBoxIcon.Error);
            }
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
