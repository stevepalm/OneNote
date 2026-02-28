namespace MDNote
{
    using MDNote.OneNote;
    using System;
    using System.Windows.Forms;

    internal static class RibbonHandler
    {
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
                    MessageBox.Show(
                        "No active page found.",
                        "MD Note",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                var title = interop.GetPageTitle(pageId);

                MessageBox.Show(
                    $"Page title: {title}\nPage ID: {pageId}",
                    "MD Note — Render Page (Session 1)",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error: {ex.Message}",
                    "MD Note — Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Stub for features not yet implemented.
        /// </summary>
        public static void ShowStub(string featureName, int sessionNumber)
        {
            MessageBox.Show(
                $"MD Note: {featureName} — Session {sessionNumber}",
                "MD Note",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
    }
}
