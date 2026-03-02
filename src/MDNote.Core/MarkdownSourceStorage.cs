using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;

namespace MDNote.Core
{
    /// <summary>
    /// Handles encoding/decoding of markdown source for round-trip storage.
    /// Source is stored as a hidden HTML span inside the rendered Outline,
    /// because UpdatePageContent does not accept Meta elements.
    /// Falls back to reading Meta for backwards compatibility.
    /// </summary>
    public static class MarkdownSourceStorage
    {
        public const string CurrentVersion = "1.0";
        public const string MetaSource = "md-note-source";
        public const string MetaVersion = "md-note-version";
        public const string MetaRendered = "md-note-rendered";

        // Hidden span tag used to embed source in Outline CDATA.
        // Uses title attribute (OneNote preserves it) instead of data-* which
        // may not be accepted by OneNote's CDATA parser.
        private const string SourceTagPrefix = "<span title=\"mdsrc:";
        private const string CompressedSourceTagPrefix = "<span title=\"mdsrc-gz:";
        private const string SourceTagSuffix = "\" style=\"display:none\"></span>";

        private static readonly Regex SourceTagRegex = new Regex(
            @"<span\s+title=""mdsrc:([^""]+)""\s+style=""display:none""></span>",
            RegexOptions.Compiled);

        private static readonly Regex CompressedSourceTagRegex = new Regex(
            @"<span\s+title=""mdsrc-gz:([^""]+)""\s+style=""display:none""></span>",
            RegexOptions.Compiled);

        // Legacy format for backward compatibility with pages saved before this change
        private static readonly Regex LegacySourceTagRegex = new Regex(
            @"<span\s+data-md-source=""([^""]+)""\s+style=""display:none""></span>",
            RegexOptions.Compiled);

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

        /// <summary>
        /// Compresses markdown source with GZip then Base64-encodes the result.
        /// Typically achieves 60-80% size reduction on markdown text.
        /// </summary>
        public static string CompressSource(string markdown)
        {
            if (string.IsNullOrEmpty(markdown))
                return string.Empty;

            var bytes = Encoding.UTF8.GetBytes(markdown);
            using (var output = new MemoryStream())
            {
                using (var gz = new GZipStream(output, CompressionLevel.Optimal))
                {
                    gz.Write(bytes, 0, bytes.Length);
                }
                return Convert.ToBase64String(output.ToArray());
            }
        }

        /// <summary>
        /// Decompresses a GZip+Base64 encoded source back to markdown text.
        /// </summary>
        public static string DecompressSource(string compressed)
        {
            if (string.IsNullOrEmpty(compressed))
                return string.Empty;

            var bytes = Convert.FromBase64String(compressed);
            using (var input = new MemoryStream(bytes))
            using (var gz = new GZipStream(input, CompressionMode.Decompress))
            using (var reader = new StreamReader(gz, Encoding.UTF8))
            {
                return reader.ReadToEnd();
            }
        }

        /// <summary>
        /// Builds the hidden HTML span that embeds the compressed source
        /// inside the Outline's CDATA content.
        /// Accepts raw markdown — compresses automatically.
        /// </summary>
        public static string BuildHiddenSourceHtml(string markdown)
        {
            if (string.IsNullOrEmpty(markdown))
                return string.Empty;

            var compressed = CompressSource(markdown);
            return CompressedSourceTagPrefix + compressed + SourceTagSuffix;
        }

        /// <summary>
        /// Extracts and decodes markdown source from HTML containing the hidden span.
        /// Handles compressed (mdsrc-gz:), uncompressed (mdsrc:), and legacy (data-md-source) formats.
        /// Returns null if not found.
        /// </summary>
        public static string ExtractMarkdownSource(string html)
        {
            if (string.IsNullOrEmpty(html))
                return null;

            // Compressed format (newest)
            var match = CompressedSourceTagRegex.Match(html);
            if (match.Success)
                return DecompressSource(match.Groups[1].Value);

            // Uncompressed format
            match = SourceTagRegex.Match(html);
            if (match.Success)
                return DecodeSource(match.Groups[1].Value);

            // Legacy data-md-source format
            match = LegacySourceTagRegex.Match(html);
            return match.Success ? DecodeSource(match.Groups[1].Value) : null;
        }

        /// <summary>
        /// Extracts encoded source from HTML content containing the hidden span.
        /// Checks current format first, then falls back to legacy data-md-source.
        /// Returns null if not found.
        /// </summary>
        [Obsolete("Use ExtractMarkdownSource which handles all formats and returns decoded markdown.")]
        public static string ExtractHiddenSource(string html)
        {
            if (string.IsNullOrEmpty(html))
                return null;

            var match = SourceTagRegex.Match(html);
            if (match.Success)
                return match.Groups[1].Value;

            // Fallback to legacy format
            match = LegacySourceTagRegex.Match(html);
            return match.Success ? match.Groups[1].Value : null;
        }

        /// <summary>
        /// Strips the hidden source span from HTML content.
        /// Handles compressed, current, and legacy formats.
        /// </summary>
        public static string StripHiddenSource(string html)
        {
            if (string.IsNullOrEmpty(html))
                return html;

            html = CompressedSourceTagRegex.Replace(html, "");
            html = SourceTagRegex.Replace(html, "");
            html = LegacySourceTagRegex.Replace(html, "");
            return html;
        }
    }
}
