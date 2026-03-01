namespace MDNote
{
    using System;
    using System.Drawing;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Windows.Forms;

    internal class AboutDialog : Form
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        public AboutDialog()
        {
            Text = "About MD Note";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(360, 310);
            BackColor = Color.FromArgb(30, 30, 30);
            ForeColor = Color.White;

            var version = Assembly.GetExecutingAssembly().GetName().Version;
            var addinPath = Assembly.GetExecutingAssembly().Location;
            var clrVersion = Environment.Version;

            int y = 16;

            var lblTitle = new Label
            {
                Text = "MD Note",
                Font = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(20, y)
            };
            Controls.Add(lblTitle);

            y += 40;
            AddLabel($"Version {version.Major}.{version.Minor}.{version.Build}", 20, y,
                Color.FromArgb(180, 180, 180));

            y += 24;
            AddLabel("Markdown rendering for OneNote Desktop", 20, y,
                Color.FromArgb(180, 180, 180));

            y += 36;
            AddLabel("Credits:", 20, y, Color.White, FontStyle.Bold);
            y += 22;
            AddLabel("  Markdig \u2014 Markdown parsing", 20, y, Color.FromArgb(160, 160, 160));
            y += 20;
            AddLabel("  ColorCode \u2014 Syntax highlighting", 20, y, Color.FromArgb(160, 160, 160));
            y += 20;
            AddLabel("  Mermaid.js \u2014 Diagram support", 20, y, Color.FromArgb(160, 160, 160));

            y += 32;
            AddLabel("System:", 20, y, Color.White, FontStyle.Bold);
            y += 22;
            AddLabel($"  Add-in: {addinPath}", 20, y, Color.FromArgb(140, 140, 140));
            y += 20;
            AddLabel($"  .NET CLR: {clrVersion}", 20, y, Color.FromArgb(140, 140, 140));

            var btnOk = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f),
                Size = new Size(80, 30),
                Location = new Point(140, 270),
                Cursor = Cursors.Hand
            };
            btnOk.FlatAppearance.BorderColor = Color.FromArgb(80, 80, 80);
            Controls.Add(btnOk);
            AcceptButton = btnOk;
        }

        private void AddLabel(string text, int x, int y, Color foreColor,
            FontStyle style = FontStyle.Regular)
        {
            var lbl = new Label
            {
                Text = text,
                ForeColor = foreColor,
                Font = new Font("Segoe UI", 9.5f, style),
                AutoSize = true,
                Location = new Point(x, y),
                MaximumSize = new Size(320, 0)
            };
            Controls.Add(lbl);
        }

        public static void ShowAboutDialog()
        {
            var ownerHandle = GetForegroundWindow();
            var owner = new NativeWindow();
            try
            {
                owner.AssignHandle(ownerHandle);
                using (var dlg = new AboutDialog())
                {
                    dlg.ShowDialog(owner);
                }
            }
            finally
            {
                owner.ReleaseHandle();
            }
        }
    }
}
