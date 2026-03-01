namespace MDNote
{
    using MDNote.Core;
    using System;
    using System.Drawing;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Windows.Forms;

    internal class SettingsForm : Form
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        private readonly TabControl _tabs;

        // Rendering tab controls
        private ComboBox _cmbTheme;
        private CheckBox _chkSyntaxHighlighting;
        private CheckBox _chkToc;
        private TextBox _txtFontFamily;
        private NumericUpDown _nudFontSize;

        // Behavior tab controls
        private ComboBox _cmbPasteMode;
        private CheckBox _chkLiveMode;
        private NumericUpDown _nudLiveDelay;

        // Export tab controls
        private TextBox _txtExportPath;
        private CheckBox _chkIncludeImages;

        public SettingsForm()
        {
            Text = "MD Note Settings";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(440, 380);
            BackColor = Color.FromArgb(30, 30, 30);
            ForeColor = Color.White;

            _tabs = new TabControl
            {
                Location = new Point(8, 8),
                Size = new Size(424, 320),
                Appearance = TabAppearance.Normal
            };
            Controls.Add(_tabs);

            BuildRenderingTab();
            BuildBehaviorTab();
            BuildExportTab();
            BuildMermaidTab();
            BuildAboutTab();

            BuildBottomButtons();

            LoadSettingsToUI();
        }

        private void BuildRenderingTab()
        {
            var tab = CreateTab("Rendering");
            int y = 16;

            AddLabel(tab, "Theme:", 16, y);
            _cmbTheme = new ComboBox
            {
                Location = new Point(160, y - 2),
                Size = new Size(200, 24),
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(55, 55, 55),
                ForeColor = Color.White
            };
            _cmbTheme.Items.AddRange(new object[] { "Dark", "Light" });
            tab.Controls.Add(_cmbTheme);

            y += 36;
            _chkSyntaxHighlighting = CreateCheckBox(tab, "Enable syntax highlighting", 16, y);

            y += 32;
            _chkToc = CreateCheckBox(tab, "Enable table of contents", 16, y);

            y += 40;
            AddLabel(tab, "Font family:", 16, y);
            _txtFontFamily = new TextBox
            {
                Location = new Point(160, y - 2),
                Size = new Size(200, 24),
                BackColor = Color.FromArgb(55, 55, 55),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            tab.Controls.Add(_txtFontFamily);

            y += 36;
            AddLabel(tab, "Font size (pt):", 16, y);
            _nudFontSize = CreateNumericUpDown(tab, 160, y - 2, 8, 24, 11);

            _tabs.TabPages.Add(tab);
        }

        private void BuildBehaviorTab()
        {
            var tab = CreateTab("Behavior");
            int y = 16;

            AddLabel(tab, "Paste mode:", 16, y);
            _cmbPasteMode = new ComboBox
            {
                Location = new Point(160, y - 2),
                Size = new Size(200, 24),
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(55, 55, 55),
                ForeColor = Color.White
            };
            _cmbPasteMode.Items.AddRange(new object[] { "Off", "Prompt", "Auto" });
            tab.Controls.Add(_cmbPasteMode);

            y += 40;
            _chkLiveMode = CreateCheckBox(tab, "Enable live mode", 16, y);

            y += 36;
            AddLabel(tab, "Live mode delay (ms):", 16, y);
            _nudLiveDelay = CreateNumericUpDown(tab, 200, y - 2, 500, 5000, 1500);
            _nudLiveDelay.Increment = 100;

            _tabs.TabPages.Add(tab);
        }

        private void BuildExportTab()
        {
            var tab = CreateTab("Export");
            int y = 16;

            AddLabel(tab, "Default export path:", 16, y);
            y += 24;
            _txtExportPath = new TextBox
            {
                Location = new Point(16, y),
                Size = new Size(300, 24),
                BackColor = Color.FromArgb(55, 55, 55),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            tab.Controls.Add(_txtExportPath);

            var btnBrowse = new Button
            {
                Text = "Browse...",
                Location = new Point(322, y - 1),
                Size = new Size(70, 26),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White
            };
            btnBrowse.FlatAppearance.BorderColor = Color.FromArgb(80, 80, 80);
            btnBrowse.Click += (s, e) =>
            {
                using (var dlg = new FolderBrowserDialog())
                {
                    dlg.Description = "Select default export folder";
                    if (!string.IsNullOrEmpty(_txtExportPath.Text))
                        dlg.SelectedPath = _txtExportPath.Text;
                    if (dlg.ShowDialog(this) == DialogResult.OK)
                        _txtExportPath.Text = dlg.SelectedPath;
                }
            };
            tab.Controls.Add(btnBrowse);

            y += 40;
            _chkIncludeImages = CreateCheckBox(tab, "Include images in export", 16, y);

            _tabs.TabPages.Add(tab);
        }

        private void BuildMermaidTab()
        {
            var tab = CreateTab("Mermaid");

            var label = new Label
            {
                Text = "Mermaid diagram settings \u2014 Coming Soon",
                ForeColor = Color.FromArgb(160, 160, 160),
                Font = new Font("Segoe UI", 11f, FontStyle.Italic),
                AutoSize = false,
                Size = new Size(360, 60),
                Location = new Point(20, 80),
                TextAlign = ContentAlignment.MiddleCenter
            };
            tab.Controls.Add(label);

            _tabs.TabPages.Add(tab);
        }

        private void BuildAboutTab()
        {
            var tab = CreateTab("About");

            var version = Assembly.GetExecutingAssembly().GetName().Version;
            var addinPath = Assembly.GetExecutingAssembly().Location;
            var clrVersion = Environment.Version;

            int y = 12;
            var lblTitle = new Label
            {
                Text = "MD Note",
                Font = new Font("Segoe UI", 16f, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(16, y)
            };
            tab.Controls.Add(lblTitle);

            y += 36;
            AddLabel(tab, $"Version {version.Major}.{version.Minor}.{version.Build}", 16, y,
                Color.FromArgb(180, 180, 180));

            y += 24;
            AddLabel(tab, "Markdown rendering for OneNote Desktop", 16, y,
                Color.FromArgb(180, 180, 180));

            y += 32;
            AddLabel(tab, "Credits:", 16, y);
            y += 20;
            AddLabel(tab, "  Markdig \u2014 Markdown parsing", 16, y, Color.FromArgb(160, 160, 160));
            y += 18;
            AddLabel(tab, "  ColorCode \u2014 Syntax highlighting", 16, y, Color.FromArgb(160, 160, 160));
            y += 18;
            AddLabel(tab, "  Mermaid.js \u2014 Diagram support", 16, y, Color.FromArgb(160, 160, 160));

            y += 32;
            AddLabel(tab, "System:", 16, y);
            y += 20;
            AddLabel(tab, $"  Add-in: {addinPath}", 16, y, Color.FromArgb(140, 140, 140));
            y += 18;
            AddLabel(tab, $"  .NET CLR: {clrVersion}", 16, y, Color.FromArgb(140, 140, 140));

            _tabs.TabPages.Add(tab);
        }

        private void BuildBottomButtons()
        {
            int y = 338;

            var btnOk = CreateDialogButton("OK", new Point(180, y), Color.FromArgb(46, 125, 50));
            btnOk.Click += (s, e) =>
            {
                SaveUIToSettings();
                DialogResult = DialogResult.OK;
                Close();
            };
            Controls.Add(btnOk);

            var btnCancel = CreateDialogButton("Cancel", new Point(256, y), Color.FromArgb(60, 60, 60));
            btnCancel.Click += (s, e) =>
            {
                DialogResult = DialogResult.Cancel;
                Close();
            };
            Controls.Add(btnCancel);

            var btnApply = CreateDialogButton("Apply", new Point(332, y), Color.FromArgb(21, 101, 192));
            btnApply.Click += (s, e) => SaveUIToSettings();
            Controls.Add(btnApply);

            var btnReset = CreateDialogButton("Reset", new Point(16, y), Color.FromArgb(120, 50, 50));
            btnReset.Click += (s, e) =>
            {
                SettingsManager.Instance.ResetToDefaults();
                LoadSettingsToUI();
            };
            Controls.Add(btnReset);
        }

        private void LoadSettingsToUI()
        {
            var s = SettingsManager.Current;
            _cmbTheme.SelectedItem = s.Theme == "light" ? "Light" : "Dark";
            _chkSyntaxHighlighting.Checked = s.EnableSyntaxHighlighting;
            _chkToc.Checked = s.EnableTableOfContents;
            _txtFontFamily.Text = s.FontFamily;
            _nudFontSize.Value = Math.Max(_nudFontSize.Minimum, Math.Min(_nudFontSize.Maximum, s.FontSize));
            _cmbPasteMode.SelectedIndex = (int)s.PasteMode;
            _chkLiveMode.Checked = s.LiveModeEnabled;
            _nudLiveDelay.Value = Math.Max(_nudLiveDelay.Minimum, Math.Min(_nudLiveDelay.Maximum, s.LiveModeDelayMs));
            _txtExportPath.Text = s.DefaultExportPath;
            _chkIncludeImages.Checked = s.IncludeImages;
        }

        private void SaveUIToSettings()
        {
            var s = SettingsManager.Current;
            s.Theme = _cmbTheme.SelectedItem?.ToString() == "Light" ? "light" : "dark";
            s.EnableSyntaxHighlighting = _chkSyntaxHighlighting.Checked;
            s.EnableTableOfContents = _chkToc.Checked;
            s.FontFamily = _txtFontFamily.Text;
            s.FontSize = (int)_nudFontSize.Value;
            s.PasteMode = (PasteMode)_cmbPasteMode.SelectedIndex;
            s.LiveModeEnabled = _chkLiveMode.Checked;
            s.LiveModeDelayMs = (int)_nudLiveDelay.Value;
            s.DefaultExportPath = _txtExportPath.Text;
            s.IncludeImages = _chkIncludeImages.Checked;
            SettingsManager.Instance.Save();
        }

        public static void ShowSettingsDialog()
        {
            var ownerHandle = GetForegroundWindow();
            var owner = new NativeWindow();
            try
            {
                owner.AssignHandle(ownerHandle);
                using (var form = new SettingsForm())
                {
                    form.ShowDialog(owner);
                }
            }
            finally
            {
                owner.ReleaseHandle();
            }
        }

        // --- UI helper methods ---

        private static TabPage CreateTab(string text)
        {
            return new TabPage(text)
            {
                BackColor = Color.FromArgb(40, 40, 40),
                ForeColor = Color.White,
                Padding = new Padding(4)
            };
        }

        private static Label AddLabel(Control parent, string text, int x, int y,
            Color? foreColor = null)
        {
            var lbl = new Label
            {
                Text = text,
                ForeColor = foreColor ?? Color.White,
                Font = new Font("Segoe UI", 9.5f),
                AutoSize = true,
                Location = new Point(x, y)
            };
            parent.Controls.Add(lbl);
            return lbl;
        }

        private static CheckBox CreateCheckBox(Control parent, string text, int x, int y)
        {
            var chk = new CheckBox
            {
                Text = text,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5f),
                AutoSize = true,
                Location = new Point(x, y)
            };
            parent.Controls.Add(chk);
            return chk;
        }

        private static NumericUpDown CreateNumericUpDown(Control parent, int x, int y,
            int min, int max, int defaultValue)
        {
            var nud = new NumericUpDown
            {
                Location = new Point(x, y),
                Size = new Size(80, 24),
                Minimum = min,
                Maximum = max,
                Value = defaultValue,
                BackColor = Color.FromArgb(55, 55, 55),
                ForeColor = Color.White
            };
            parent.Controls.Add(nud);
            return nud;
        }

        private static Button CreateDialogButton(string text, Point location, Color bgColor)
        {
            var btn = new Button
            {
                Text = text,
                Location = location,
                Size = new Size(70, 30),
                FlatStyle = FlatStyle.Flat,
                BackColor = bgColor,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderColor = Color.FromArgb(80, 80, 80);
            return btn;
        }
    }
}
