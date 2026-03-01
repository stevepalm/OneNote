namespace MDNote
{
    using MDNote.Core;
    using System;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Windows.Forms;

    /// <summary>
    /// Monitors the system clipboard for markdown text via AddClipboardFormatListener.
    /// When markdown is detected on the clipboard while OneNote is foreground,
    /// invokes a callback with the detected text.
    /// </summary>
    internal class ClipboardMonitor : IDisposable
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool AddClipboardFormatListener(IntPtr hwnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RemoveClipboardFormatListener(IntPtr hwnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern int GetWindowThreadProcessId(IntPtr hWnd, out int processId);

        private const int WM_CLIPBOARDUPDATE = 0x031D;
        private const double ShortTextThreshold = 0.5;

        private ClipboardWindow _window;
        private Action<string> _onMarkdownDetected;
        private string _lastClipboardHash;
        private bool _disposed;

        /// <summary>
        /// Creates the hidden message window and starts listening for clipboard changes.
        /// </summary>
        public void Start(Action<string> onMarkdownDetected)
        {
            _onMarkdownDetected = onMarkdownDetected;
            _window = new ClipboardWindow(OnClipboardChanged);

            if (!AddClipboardFormatListener(_window.Handle))
                ErrorHandler.LogWarning("Failed to register clipboard listener.");
            else
                ErrorHandler.Log("Clipboard monitor started.");
        }

        private void OnClipboardChanged()
        {
            try
            {
                if (!IsOneNoteForeground())
                    return;

                if (MdNoteSettings.Current.PasteMode == PasteMode.Off)
                    return;

                ReadClipboard(out string text, out bool hasHtml);

                // Skip HTML-format pastes (browser copies set both text and HTML)
                if (hasHtml)
                    return;

                if (string.IsNullOrWhiteSpace(text))
                    return;

                // Deduplicate rapid or repeated clipboard events
                var hash = text.GetHashCode().ToString();
                if (hash == _lastClipboardHash)
                    return;
                _lastClipboardHash = hash;

                var detection = MarkdownDetector.Detect(text);

                // Short pastes (<3 non-empty lines) require higher confidence
                var lines = text.Split(new[] { '\r', '\n' },
                    StringSplitOptions.RemoveEmptyEntries);
                double threshold = lines.Length < 3 ? ShortTextThreshold : 0.15;

                if (detection.Score < threshold)
                    return;

                _onMarkdownDetected?.Invoke(text);
            }
            catch (Exception ex)
            {
                ErrorHandler.LogError("Clipboard monitor", ex);
            }
        }

        private static void ReadClipboard(out string text, out bool hasHtml)
        {
            string t = null;
            bool h = false;

            if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
            {
                h = Clipboard.ContainsData(DataFormats.Html);
                t = Clipboard.ContainsText()
                    ? Clipboard.GetText(TextDataFormat.UnicodeText)
                    : null;
            }
            else
            {
                var thread = new Thread(() =>
                {
                    h = Clipboard.ContainsData(DataFormats.Html);
                    t = Clipboard.ContainsText()
                        ? Clipboard.GetText(TextDataFormat.UnicodeText)
                        : null;
                });
                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
                thread.Join(1000);
            }

            text = t;
            hasHtml = h;
        }

        private static bool IsOneNoteForeground()
        {
            try
            {
                var foreground = GetForegroundWindow();
                if (foreground == IntPtr.Zero)
                    return false;

                GetWindowThreadProcessId(foreground, out int pid);
                var process = Process.GetProcessById(pid);
                return process.ProcessName.IndexOf(
                    "ONENOTE", StringComparison.OrdinalIgnoreCase) >= 0;
            }
            catch
            {
                return false;
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;

            if (_window != null)
            {
                RemoveClipboardFormatListener(_window.Handle);
                _window.DestroyHandle();
                _window = null;
                ErrorHandler.Log("Clipboard monitor stopped.");
            }
        }

        /// <summary>
        /// Hidden message-only window to receive WM_CLIPBOARDUPDATE messages.
        /// </summary>
        private class ClipboardWindow : NativeWindow
        {
            private readonly Action _onClipboardChanged;

            public ClipboardWindow(Action onClipboardChanged)
            {
                _onClipboardChanged = onClipboardChanged;
                CreateHandle(new CreateParams
                {
                    Caption = "MDNote Clipboard Window",
                    Style = 0,
                    ExStyle = 0x80 // WS_EX_TOOLWINDOW
                });
            }

            protected override void WndProc(ref Message m)
            {
                if (m.Msg == WM_CLIPBOARDUPDATE)
                {
                    _onClipboardChanged?.Invoke();
                }
                base.WndProc(ref m);
            }
        }
    }
}
