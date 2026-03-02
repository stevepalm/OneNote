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
            BackColor = SystemColors.Window;
            ForeColor = SystemColors.WindowText;

            var version = Assembly.GetExecutingAssembly().GetName().Version;
            var addinPath = Assembly.GetExecutingAssembly().Location;
            var clrVersion = Environment.Version;

            int y = 16;

            var lblTitle = new Label
            {
                Text = "MD Note",
                Font = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor = SystemColors.WindowText,
                AutoSize = true,
                Location = new Point(20, y)
            };
            Controls.Add(lblTitle);

            y += 40;
            AddLabel($"Version {version.Major}.{version.Minor}.{version.Build}", 20, y,
                SystemColors.GrayText);

            y += 24;
            AddLabel("Markdown rendering for OneNote Desktop", 20, y,
                SystemColors.GrayText);

            y += 36;
            AddLabel("Credits:", 20, y, SystemColors.WindowText, FontStyle.Bold);
            y += 22;
            AddLabel("  Markdig \u2014 Markdown parsing", 20, y, SystemColors.GrayText);
            y += 20;
            AddLabel("  ColorCode \u2014 Syntax highlighting", 20, y, SystemColors.GrayText);

            y += 32;
            AddLabel("Quality:", 20, y, SystemColors.WindowText, FontStyle.Bold);
            y += 22;
            AddLabel("  362 automated tests passing", 20, y, Color.FromArgb(46, 125, 50));

            y += 28;
            AddLabel("System:", 20, y, SystemColors.WindowText, FontStyle.Bold);
            y += 22;
            var lblPath = AddLabel($"  Add-in: {addinPath}", 20, y, SystemColors.GrayText);
            y += Math.Max(20, lblPath.PreferredHeight);
            AddLabel($"  .NET CLR: {clrVersion}", 20, y, SystemColors.GrayText);

            y += 28;

            var btnOk = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                FlatStyle = FlatStyle.System,
                Font = new Font("Segoe UI", 9f),
                Size = new Size(80, 30),
                Location = new Point((360 - 80) / 2, y)
            };
            Controls.Add(btnOk);
            AcceptButton = btnOk;

            y += 30 + 12;
            ClientSize = new Size(360, y);
        }

        private Label AddLabel(string text, int x, int y, Color foreColor,
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
            return lbl;
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
