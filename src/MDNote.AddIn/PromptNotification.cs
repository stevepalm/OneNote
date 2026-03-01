namespace MDNote
{
    using System;
    using System.Drawing;
    using System.Windows.Forms;

    /// <summary>
    /// Shows a non-activating prompt popup with Render / Ignore / Always buttons.
    /// Auto-dismisses after 5 seconds (same as Ignore).
    /// </summary>
    internal static class PromptNotification
    {
        public static void Show(Action onRender, Action onAlwaysRender)
        {
            try
            {
                var form = new PromptForm(onRender, onAlwaysRender);
                form.Show();
            }
            catch
            {
                // Never crash the add-in over a notification failure
            }
        }

        private class PromptForm : Form
        {
            private const int WS_EX_NOACTIVATE = 0x08000000;
            private const int WS_EX_TOOLWINDOW = 0x00000080;
            private const int WS_EX_TOPMOST = 0x00000008;

            private readonly Timer _timer;
            private readonly Action _onRender;
            private readonly Action _onAlwaysRender;

            public PromptForm(Action onRender, Action onAlwaysRender)
            {
                _onRender = onRender;
                _onAlwaysRender = onAlwaysRender;

                FormBorderStyle = FormBorderStyle.None;
                ShowInTaskbar = false;
                TopMost = true;
                StartPosition = FormStartPosition.Manual;
                BackColor = Color.FromArgb(33, 33, 33);
                Opacity = 0.95;

                var label = new Label
                {
                    Text = "Markdown detected!",
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 10f, FontStyle.Regular),
                    AutoSize = false,
                    Size = new Size(220, 24),
                    Location = new Point(12, 10),
                    TextAlign = ContentAlignment.MiddleLeft
                };
                Controls.Add(label);

                var btnRender = CreateButton("Render",
                    Color.FromArgb(46, 125, 50), new Point(12, 40));
                btnRender.Click += (s, e) =>
                {
                    _timer.Stop();
                    Close();
                    _onRender?.Invoke();
                };
                Controls.Add(btnRender);

                var btnIgnore = CreateButton("Ignore",
                    Color.FromArgb(117, 117, 117), new Point(88, 40));
                btnIgnore.Click += (s, e) =>
                {
                    _timer.Stop();
                    Close();
                };
                Controls.Add(btnIgnore);

                var btnAlways = CreateButton("Always",
                    Color.FromArgb(21, 101, 192), new Point(164, 40));
                btnAlways.Click += (s, e) =>
                {
                    _timer.Stop();
                    Close();
                    _onAlwaysRender?.Invoke();
                };
                Controls.Add(btnAlways);

                ClientSize = new Size(244, 74);

                // Position top-right, offset below the toast notification area
                var workArea = Screen.PrimaryScreen.WorkingArea;
                Location = new Point(
                    workArea.Right - Width - 16,
                    workArea.Top + 60);

                _timer = new Timer { Interval = 5000 };
                _timer.Tick += (s, e) =>
                {
                    _timer.Stop();
                    Close();
                };
                _timer.Start();
            }

            private static Button CreateButton(string text, Color bgColor, Point location)
            {
                var btn = new Button
                {
                    Text = text,
                    ForeColor = Color.White,
                    BackColor = bgColor,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 8.5f),
                    Size = new Size(70, 26),
                    Location = location,
                    Cursor = Cursors.Hand,
                    TabStop = false
                };
                btn.FlatAppearance.BorderSize = 0;
                return btn;
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
