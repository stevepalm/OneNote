namespace MDNote
{
    using System;
    using System.Drawing;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Windows.Forms;

    /// <summary>
    /// Shows small auto-dismissing toast notifications at the top-right of the screen.
    /// Non-activating: won't steal focus from OneNote.
    /// </summary>
    internal static class NotificationHelper
    {
        public static void ShowSuccess(string message, int durationMs = 2000)
        {
            Show(message, Color.FromArgb(46, 125, 50), durationMs);
        }

        public static void ShowWarning(string message, int durationMs = 3000)
        {
            Show(message, Color.FromArgb(245, 124, 0), durationMs);
        }

        public static void ShowError(string message, int durationMs = 4000)
        {
            try
            {
                if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
                {
                    ShowErrorMessageBox(message);
                }
                else
                {
                    var thread = new Thread(() => ShowErrorMessageBox(message));
                    thread.SetApartmentState(ApartmentState.STA);
                    thread.IsBackground = true;
                    thread.Start();
                }
            }
            catch
            {
                // Never crash the add-in over a notification failure
            }
        }

        private static void ShowErrorMessageBox(string message)
        {
            try
            {
                var ownerHandle = GetForegroundWindow();
                var owner = new NativeWindow();
                try
                {
                    owner.AssignHandle(ownerHandle);
                    MessageBox.Show(owner, message, "MD Note",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    owner.ReleaseHandle();
                }
            }
            catch { }
        }

        private static void Show(string message, Color bgColor, int durationMs)
        {
            try
            {
                // Run each toast on its own STA thread with a message pump so
                // the auto-dismiss timer fires correctly even when called from
                // worker threads that have no message loop.
                var thread = new Thread(() =>
                {
                    try
                    {
                        var toast = new ToastForm(message, bgColor, durationMs);
                        toast.FormClosed += (s, e) => Application.ExitThread();
                        Application.Run(toast);
                    }
                    catch { }
                });
                thread.SetApartmentState(ApartmentState.STA);
                thread.IsBackground = true;
                thread.Start();
            }
            catch
            {
                // Never crash the add-in over a notification failure
            }
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        private class ToastForm : Form
        {
            private const int WS_EX_NOACTIVATE = 0x08000000;
            private const int WS_EX_TOOLWINDOW = 0x00000080;
            private const int WS_EX_TOPMOST = 0x00000008;

            private readonly System.Windows.Forms.Timer _timer;

            public ToastForm(string message, Color bgColor, int durationMs)
            {
                FormBorderStyle = FormBorderStyle.None;
                ShowInTaskbar = false;
                TopMost = true;
                StartPosition = FormStartPosition.Manual;
                BackColor = bgColor;
                Opacity = 0.95;

                var label = new Label
                {
                    Text = message,
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 10f, FontStyle.Regular),
                    AutoSize = true,
                    Padding = new Padding(12, 8, 12, 8),
                    Location = new Point(0, 0)
                };

                Controls.Add(label);

                // Size the form to fit the label
                label.PerformLayout();
                var textSize = TextRenderer.MeasureText(message,
                    label.Font, new Size(400, 0),
                    TextFormatFlags.WordBreak);
                ClientSize = new Size(
                    textSize.Width + 24,
                    textSize.Height + 16);
                label.Size = ClientSize;
                label.TextAlign = ContentAlignment.MiddleLeft;

                // Position top-right of the screen where OneNote (foreground window) is
                var fgHandle = GetForegroundWindow();
                var screen = fgHandle != IntPtr.Zero
                    ? Screen.FromHandle(fgHandle)
                    : Screen.PrimaryScreen;
                var workArea = screen.WorkingArea;
                Location = new Point(
                    workArea.Right - Width - 16,
                    workArea.Top + 16);

                _timer = new System.Windows.Forms.Timer { Interval = durationMs };
                _timer.Tick += (s, e) =>
                {
                    _timer.Stop();
                    Close();
                };
                _timer.Start();
            }

            protected override bool ShowWithoutActivation => true;

            protected override CreateParams CreateParams
            {
                get
                {
                    var cp = base.CreateParams;
                    cp.ExStyle |= WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW | WS_EX_TOPMOST;
                    return cp;
                }
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                    _timer?.Dispose();
                base.Dispose(disposing);
            }
        }
    }
}
