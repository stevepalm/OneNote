namespace MDNote
{
    using System;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using System.Windows.Forms;

    /// <summary>
    /// Manages global hotkeys via RegisterHotKey/UnregisterHotKey.
    /// Only fires when OneNote is the foreground window.
    /// </summary>
    internal class HotkeyManager : IDisposable
    {
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern int GetWindowThreadProcessId(IntPtr hWnd, out int processId);

        private const int WM_HOTKEY = 0x0312;

        // Hotkey IDs
        private const int HK_RENDER_PAGE = 1;
        private const int HK_EXPORT = 2;
        private const int HK_TOGGLE_SOURCE = 3;

        // Virtual key codes
        private const uint VK_F5 = 0x74;
        private const uint VK_F8 = 0x77;
        private const uint VK_OEM_COMMA = 0xBC;

        // Modifiers
        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_NOREPEAT = 0x4000;

        private HotkeyWindow _window;
        private Action _onRenderPage;
        private Action _onExport;
        private Action _onToggleSource;
        private bool _disposed;

        /// <summary>
        /// Registers global hotkeys and starts listening.
        /// F5 → RenderPage, F8 → Export, Ctrl+, → ToggleSource.
        /// </summary>
        public void Register(Action onRenderPage, Action onExport, Action onToggleSource)
        {
            _onRenderPage = onRenderPage;
            _onExport = onExport;
            _onToggleSource = onToggleSource;

            _window = new HotkeyWindow(OnHotkeyPressed);

            if (!RegisterHotKey(_window.Handle, HK_RENDER_PAGE, MOD_NOREPEAT, VK_F5))
                ErrorHandler.LogWarning("Failed to register F5 hotkey (may be in use by another app).");

            if (!RegisterHotKey(_window.Handle, HK_EXPORT, MOD_NOREPEAT, VK_F8))
                ErrorHandler.LogWarning("Failed to register F8 hotkey (may be in use by another app).");

            if (!RegisterHotKey(_window.Handle, HK_TOGGLE_SOURCE, MOD_CONTROL | MOD_NOREPEAT, VK_OEM_COMMA))
                ErrorHandler.LogWarning("Failed to register Ctrl+, hotkey (may be in use by another app).");

            ErrorHandler.Log("Hotkeys registered.");
        }

        private void OnHotkeyPressed(int hotkeyId)
        {
            try
            {
                if (!IsOneNoteForeground())
                    return;

                switch (hotkeyId)
                {
                    case HK_RENDER_PAGE:
                        _onRenderPage?.Invoke();
                        break;
                    case HK_EXPORT:
                        _onExport?.Invoke();
                        break;
                    case HK_TOGGLE_SOURCE:
                        _onToggleSource?.Invoke();
                        break;
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.LogError("Hotkey handler", ex);
            }
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
                UnregisterHotKey(_window.Handle, HK_RENDER_PAGE);
                UnregisterHotKey(_window.Handle, HK_EXPORT);
                UnregisterHotKey(_window.Handle, HK_TOGGLE_SOURCE);
                _window.DestroyHandle();
                _window = null;
                ErrorHandler.Log("Hotkeys unregistered.");
            }
        }

        /// <summary>
        /// Hidden message-only window to receive WM_HOTKEY messages.
        /// </summary>
        private class HotkeyWindow : NativeWindow
        {
            private readonly Action<int> _onHotkey;

            public HotkeyWindow(Action<int> onHotkey)
            {
                _onHotkey = onHotkey;
                CreateHandle(new CreateParams
                {
                    Caption = "MDNote Hotkey Window",
                    Style = 0,
                    ExStyle = 0x80 // WS_EX_TOOLWINDOW
                });
            }

            protected override void WndProc(ref Message m)
            {
                if (m.Msg == WM_HOTKEY)
                {
                    _onHotkey?.Invoke(m.WParam.ToInt32());
                }
                base.WndProc(ref m);
            }
        }
    }
}
