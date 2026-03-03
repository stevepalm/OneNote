using System;

namespace MDNote.Core
{
    /// <summary>
    /// Extracts the HTML fragment and metadata from the Windows CF_HTML clipboard format.
    /// CF_HTML has a header section with byte offsets and version info, followed by HTML
    /// with &lt;!--StartFragment--&gt; and &lt;!--EndFragment--&gt; markers.
    /// </summary>
    public static class CfHtmlParser
    {
        private const string StartFragmentMarker = "<!--StartFragment-->";
        private const string EndFragmentMarker = "<!--EndFragment-->";

        /// <summary>
        /// Extracts the HTML fragment from a CF_HTML clipboard format string.
        /// Returns just the content between the StartFragment/EndFragment markers.
        /// </summary>
        public static string ExtractFragment(string cfHtml)
        {
            if (string.IsNullOrEmpty(cfHtml))
                return null;

            var startIdx = cfHtml.IndexOf(StartFragmentMarker, StringComparison.Ordinal);
            var endIdx = cfHtml.IndexOf(EndFragmentMarker, StringComparison.Ordinal);

            if (startIdx >= 0 && endIdx > startIdx)
            {
                startIdx += StartFragmentMarker.Length;
                return cfHtml.Substring(startIdx, endIdx - startIdx).Trim();
            }

            // Fallback: no markers found, try to find the <html> or <body> content
            var bodyStart = cfHtml.IndexOf("<body", StringComparison.OrdinalIgnoreCase);
            if (bodyStart >= 0)
            {
                var bodyTagEnd = cfHtml.IndexOf('>', bodyStart);
                if (bodyTagEnd >= 0)
                {
                    var bodyClose = cfHtml.IndexOf("</body>", bodyTagEnd, StringComparison.OrdinalIgnoreCase);
                    if (bodyClose > bodyTagEnd)
                        return cfHtml.Substring(bodyTagEnd + 1, bodyClose - bodyTagEnd - 1).Trim();
                }
            }

            // Last resort: strip header lines (lines with ":" before HTML content)
            var htmlStart = cfHtml.IndexOf('<');
            return htmlStart >= 0 ? cfHtml.Substring(htmlStart).Trim() : cfHtml;
        }

        /// <summary>
        /// Extracts the SourceURL from CF_HTML headers, if present.
        /// Returns null if not found.
        /// </summary>
        public static string ExtractSourceUrl(string cfHtml)
        {
            if (string.IsNullOrEmpty(cfHtml))
                return null;

            const string prefix = "SourceURL:";
            var idx = cfHtml.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
            if (idx < 0)
                return null;

            var start = idx + prefix.Length;
            var end = cfHtml.IndexOf('\n', start);
            if (end < 0) end = cfHtml.Length;

            var url = cfHtml.Substring(start, end - start).Trim();
            return string.IsNullOrEmpty(url) ? null : url;
        }
    }
}
