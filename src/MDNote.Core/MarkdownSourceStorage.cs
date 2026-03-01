using System;
using System.Collections.Generic;
using System.Text;

namespace MDNote.Core
{
    /// <summary>
    /// Handles encoding/decoding of markdown source for round-trip storage
    /// in OneNote page Meta elements.
    /// </summary>
    public static class MarkdownSourceStorage
    {
        public const string CurrentVersion = "1.0";
        public const string MetaSource = "md-note-source";
        public const string MetaVersion = "md-note-version";
        public const string MetaRendered = "md-note-rendered";

        public static string EncodeSource(string markdown)
        {
            if (string.IsNullOrEmpty(markdown))
                return string.Empty;

            return Convert.ToBase64String(Encoding.UTF8.GetBytes(markdown));
        }

        public static string DecodeSource(string encoded)
        {
            if (string.IsNullOrEmpty(encoded))
                return string.Empty;

            return Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
        }

        public static Dictionary<string, string> CreateMetaEntries(string markdown)
        {
            return new Dictionary<string, string>
            {
                { MetaSource, EncodeSource(markdown) },
                { MetaVersion, CurrentVersion },
                { MetaRendered, DateTime.UtcNow.ToString("o") }
            };
        }
    }
}
