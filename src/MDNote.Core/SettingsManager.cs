using System;
using System.IO;
using System.Text.RegularExpressions;

namespace MDNote.Core
{
    /// <summary>
    /// Thread-safe singleton for MD Note settings with JSON persistence at %APPDATA%\MDNote\settings.json.
    /// Loads on first access, saves on demand, fires SettingsChanged after each save.
    /// </summary>
    public class SettingsManager
    {
        private static readonly string SettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MDNote", "settings.json");

        private static SettingsManager _instance;
        private static readonly object Lock = new object();

        private Settings _settings;

        public event EventHandler SettingsChanged;

        private SettingsManager(Settings settings)
        {
            _settings = settings;
        }

        /// <summary>
        /// Current settings. Loads from disk on first access.
        /// </summary>
        public static Settings Current
        {
            get => Instance._settings;
        }

        /// <summary>
        /// Singleton accessor. Creates instance and loads from disk on first access.
        /// </summary>
        public static SettingsManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (Lock)
                    {
                        if (_instance == null)
                        {
                            var settings = LoadFromDisk();
                            _instance = new SettingsManager(settings);
                        }
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// Persists current settings to disk and fires SettingsChanged.
        /// </summary>
        public void Save()
        {
            try
            {
                var dir = Path.GetDirectoryName(SettingsPath);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var json = ToJson(_settings);
                File.WriteAllText(SettingsPath, json);
            }
            catch
            {
                // Never crash the host over a settings write failure
            }

            SettingsChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Resets all settings to their default values and saves.
        /// </summary>
        public void ResetToDefaults()
        {
            _settings = new Settings();
            Save();
        }

        /// <summary>
        /// Clears the singleton so next access re-reads from disk.
        /// Called from AddIn.OnDisconnection.
        /// </summary>
        public static void Reset()
        {
            lock (Lock)
                _instance = null;
        }

        // ---- JSON serialization (manual, no NuGet dependency) ----

        /// <summary>
        /// Serializes a Settings object to a JSON string.
        /// </summary>
        public static string ToJson(Settings s)
        {
            if (s == null)
                s = new Settings();

            return "{"
                + $"\"PasteMode\":{(int)s.PasteMode}"
                + $",\"Theme\":\"{Escape(s.Theme)}\""
                + $",\"EnableSyntaxHighlighting\":{Bool(s.EnableSyntaxHighlighting)}"
                + $",\"EnableTableOfContents\":{Bool(s.EnableTableOfContents)}"
                + $",\"FontFamily\":\"{Escape(s.FontFamily)}\""
                + $",\"FontSize\":{s.FontSize}"
                + $",\"LiveModeEnabled\":{Bool(s.LiveModeEnabled)}"
                + $",\"LiveModeDelayMs\":{s.LiveModeDelayMs}"
                + $",\"DefaultExportPath\":\"{Escape(s.DefaultExportPath)}\""
                + $",\"IncludeImages\":{Bool(s.IncludeImages)}"
                + "}";
        }

        /// <summary>
        /// Deserializes a JSON string to a Settings object.
        /// Missing or invalid fields get default values.
        /// </summary>
        public static Settings FromJson(string json)
        {
            var s = new Settings();

            if (string.IsNullOrWhiteSpace(json))
                return s;

            s.PasteMode = ReadEnum<PasteMode>(json, "PasteMode", s.PasteMode);
            s.Theme = ReadString(json, "Theme", s.Theme);
            s.EnableSyntaxHighlighting = ReadBool(json, "EnableSyntaxHighlighting", s.EnableSyntaxHighlighting);
            s.EnableTableOfContents = ReadBool(json, "EnableTableOfContents", s.EnableTableOfContents);
            s.FontFamily = ReadString(json, "FontFamily", s.FontFamily);
            s.FontSize = ReadInt(json, "FontSize", s.FontSize);
            s.LiveModeEnabled = ReadBool(json, "LiveModeEnabled", s.LiveModeEnabled);
            s.LiveModeDelayMs = ReadInt(json, "LiveModeDelayMs", s.LiveModeDelayMs);
            s.DefaultExportPath = ReadString(json, "DefaultExportPath", s.DefaultExportPath);
            s.IncludeImages = ReadBool(json, "IncludeImages", s.IncludeImages);

            return s;
        }

        // ---- Private helpers ----

        private static Settings LoadFromDisk()
        {
            try
            {
                if (!File.Exists(SettingsPath))
                    return new Settings();

                var json = File.ReadAllText(SettingsPath);
                return FromJson(json);
            }
            catch
            {
                return new Settings();
            }
        }

        private static string Bool(bool value) => value ? "true" : "false";

        private static string Escape(string s)
        {
            return (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        private static string Unescape(string s)
        {
            return (s ?? "").Replace("\\\"", "\"").Replace("\\\\", "\\");
        }

        private static T ReadEnum<T>(string json, string key, T defaultValue) where T : struct
        {
            var match = Regex.Match(json, $"\"{key}\"\\s*:\\s*(\\d+)");
            if (match.Success
                && int.TryParse(match.Groups[1].Value, out var intVal)
                && Enum.IsDefined(typeof(T), intVal))
            {
                return (T)Enum.ToObject(typeof(T), intVal);
            }
            return defaultValue;
        }

        private static string ReadString(string json, string key, string defaultValue)
        {
            var match = Regex.Match(json, $"\"{key}\"\\s*:\\s*\"((?:[^\"\\\\]|\\\\.)*)\"");
            if (match.Success)
                return Unescape(match.Groups[1].Value);
            return defaultValue;
        }

        private static bool ReadBool(string json, string key, bool defaultValue)
        {
            var match = Regex.Match(json, $"\"{key}\"\\s*:\\s*(true|false)");
            if (match.Success)
                return match.Groups[1].Value == "true";
            return defaultValue;
        }

        private static int ReadInt(string json, string key, int defaultValue)
        {
            var match = Regex.Match(json, $"\"{key}\"\\s*:\\s*(\\d+)");
            if (match.Success && int.TryParse(match.Groups[1].Value, out var val))
                return val;
            return defaultValue;
        }
    }
}
