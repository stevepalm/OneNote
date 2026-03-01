using System;
using System.Net;
using System.Text.RegularExpressions;

namespace MDNote.Core
{
    internal class MathRenderer
    {
        private static readonly Regex MathBlockRegex = new Regex(
            @"<div\s+class=""math"">([\s\S]*?)</div>",
            RegexOptions.Compiled);

        private static readonly Regex MathInlineRegex = new Regex(
            @"<span\s+class=""math"">([\s\S]*?)</span>",
            RegexOptions.Compiled);

        private const string InlineStyle =
            "font-family:'Cambria Math','Times New Roman',serif;font-style:italic;";

        private const string BlockStyle =
            "text-align:center;font-family:'Cambria Math','Times New Roman',serif;" +
            "font-size:1.2em;margin:0.5em 0;";

        public string ProcessMathBlocks(string html)
        {
            if (string.IsNullOrEmpty(html))
                return html;

            // Process block math first (more specific match)
            html = MathBlockRegex.Replace(html, match =>
            {
                try
                {
                    var content = StripDelimiters(match.Groups[1].Value);
                    return $"<div style=\"{BlockStyle}\">{content}</div>";
                }
                catch
                {
                    // Graceful degradation: return styled but unprocessed
                    return $"<div style=\"{BlockStyle}\">{match.Groups[1].Value}</div>";
                }
            });

            // Process inline math
            html = MathInlineRegex.Replace(html, match =>
            {
                try
                {
                    var content = StripDelimiters(match.Groups[1].Value);
                    return $"<span style=\"{InlineStyle}\">{content}</span>";
                }
                catch
                {
                    return $"<span style=\"{InlineStyle}\">{match.Groups[1].Value}</span>";
                }
            });

            return html;
        }

        private static string StripDelimiters(string content)
        {
            content = content.Trim();

            // Strip \[ ... \] (block delimiters)
            if (content.StartsWith("\\[") && content.EndsWith("\\]"))
                content = content.Substring(2, content.Length - 4).Trim();
            // Strip \( ... \) (inline delimiters)
            else if (content.StartsWith("\\(") && content.EndsWith("\\)"))
                content = content.Substring(2, content.Length - 4).Trim();

            return content;
        }
    }
}
