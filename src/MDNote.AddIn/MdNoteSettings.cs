namespace MDNote
{
    using System;
    using System.IO;
    using System.Text.RegularExpressions;

    internal enum PasteMode
    {
        Off = 0,
        Prompt = 1,
        Auto = 2
    }

    /// <summary>
    /// Global settings with JSON persistence at %LOCALAPPDATA%\MDNote\settings.json.
    /// Thread-safe singleton; loads on first access, saves on demand.
    /// </summary>
    internal class MdNoteSettings
    {
        private static readonly string SettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MDNote", "settings.json");

        private static MdNoteSettings _current;
        private static readonly object Lock = new object();

        public PasteMode PasteMode { get; set; } = PasteMode.Prompt;

        /// <summary>
        /// Singleton accessor. Loads from disk on first access.
        /// </summary>
        public static MdNoteSettings Current
        {
            get
            {
                if (_current == null)
                {
                    lock (Lock)
                    {
                        if (_current == null)
                            _current = Load();
                    }
                }
                return _current;
            }
        }

        public void Save()
        {
            try
            {
                var dir = Path.GetDirectoryName(SettingsPath);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                File.WriteAllText(SettingsPath,
                    "{\"PasteMode\":" + (int)PasteMode + "}");

                ErrorHandler.Log($"Settings saved: PasteMode={PasteMode}");
            }
            catch (Exception ex)
            {
                ErrorHandler.LogError("Save settings", ex);
            }
        }

        private static MdNoteSettings Load()
        {
            try
            {
                if (!File.Exists(SettingsPath))
                    return new MdNoteSettings();

                var json = File.ReadAllText(SettingsPath);
                var match = Regex.Match(json, @"""PasteMode""\s*:\s*(\d+)");

                if (match.Success
                    && int.TryParse(match.Groups[1].Value, out var mode)
                    && Enum.IsDefined(typeof(PasteMode), mode))
                {
                    return new MdNoteSettings { PasteMode = (PasteMode)mode };
                }

                return new MdNoteSettings();
            }
            catch
            {
                return new MdNoteSettings();
            }
        }

        /// <summary>
        /// Clears the singleton so next access re-reads from disk.
        /// Called from AddIn.OnDisconnection.
        /// </summary>
        internal static void Reset()
        {
            lock (Lock)
                _current = null;
        }
    }
}
