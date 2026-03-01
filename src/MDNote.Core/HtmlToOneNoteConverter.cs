using System.Net;
using System.Text.RegularExpressions;

namespace MDNote.Core
{
    /// <summary>
    /// Converts Markdig-generated HTML into the subset of HTML that OneNote supports in CDATA.
    /// Supported: p, br, span, div, a, ul, ol, li, table, tr, td, h1-h6, b, em, strong, i, u,
    ///            del, sup, sub, cite, img. Inline style attributes are preserved.
    /// </summary>
    public class HtmlToOneNoteConverter
    {
        // Code blocks: <pre><code class="language-xxx">...</code></pre> or <pre><code>...</code></pre>
        // Also matches the SyntaxHighlighter output: <div style="margin:8px 0;">...<div style="color:...;background-color:...;">...</div></div>
        private static readonly Regex HighlightedCodeBlockRegex = new Regex(
            @"<div style=""margin:8px 0;"">(.*?)<div style=""color:#[A-Fa-f0-9]+;background-color:#[A-Fa-f0-9]+;""><pre[^>]*><code[^>]*>([\s\S]*?)</code></pre></div></div>",
            RegexOptions.Compiled);

        private static readonly Regex LabelDivRegex = new Regex(
            @"<div style=""background:#2d2d2d;color:#858585;[^""]*"">([^<]*)</div>",
            RegexOptions.Compiled);

        private static readonly Regex PreCodeWithLangRegex = new Regex(
            @"<pre><code\s+class=""language-([^""]+)"">([\s\S]*?)</code></pre>",
            RegexOptions.Compiled);

        private static readonly Regex PreCodePlainRegex = new Regex(
            @"<pre><code>([\s\S]*?)</code></pre>",
            RegexOptions.Compiled);

        // Blockquotes
        private static readonly Regex BlockquoteRegex = new Regex(
            @"<blockquote>([\s\S]*?)</blockquote>",
            RegexOptions.Compiled);

        // Horizontal rules
        private static readonly Regex HrRegex = new Regex(
            @"<hr\s*/?>",
            RegexOptions.Compiled);

        // Checkboxes
        private static readonly Regex CheckedBoxRegex = new Regex(
            @"<input\s+[^>]*type=""checkbox""[^>]*checked[^>]*/?>",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex UncheckedBoxRegex = new Regex(
            @"<input\s+[^>]*type=""checkbox""[^>]*/?>",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Mark/highlight
        private static readonly Regex MarkRegex = new Regex(
            @"<mark>([\s\S]*?)</mark>",
            RegexOptions.Compiled);

        // Headings (capture level and inner content)
        private static readonly Regex HeadingRegex = new Regex(
            @"<h([1-6])([^>]*)>([\s\S]*?)</h\1>",
            RegexOptions.Compiled);

        // Tables without border styling
        private static readonly Regex TableOpenRegex = new Regex(
            @"<table(?![^>]*style)([^>]*)>",
            RegexOptions.Compiled);

        private static readonly Regex TdOpenRegex = new Regex(
            @"<td(?![^>]*style)([^>]*)>",
            RegexOptions.Compiled);

        private static readonly Regex ThOpenRegex = new Regex(
            @"<th([^>]*)>",
            RegexOptions.Compiled);

        private static readonly Regex ThCloseRegex = new Regex(
            @"</th>",
            RegexOptions.Compiled);

        // <thead> and <tbody> — not supported by OneNote, strip them
        private static readonly Regex TheadTbodyRegex = new Regex(
            @"</?t(head|body)>",
            RegexOptions.Compiled);

        private readonly CodeBlockRenderer _codeBlockRenderer;

        public HtmlToOneNoteConverter(string theme = "dark")
        {
            _codeBlockRenderer = new CodeBlockRenderer(theme);
        }

        /// <summary>
        /// Converts Markdig HTML to OneNote-compatible HTML.
        /// </summary>
        public string ConvertForOneNote(string markdigHtml)
        {
            if (string.IsNullOrEmpty(markdigHtml))
                return string.Empty;

            var html = markdigHtml;

            // 1. Code blocks first (outermost) — syntax-highlighted blocks from SyntaxHighlighter
            html = HighlightedCodeBlockRegex.Replace(html, match =>
            {
                var labelDiv = match.Groups[1].Value;
                var codeContent = match.Groups[2].Value;

                // Extract language from label
                string language = null;
                var labelMatch = LabelDivRegex.Match(labelDiv);
                if (labelMatch.Success)
                {
                    language = labelMatch.Groups[1].Value.Trim();
                }

                return _codeBlockRenderer.RenderCodeBlockAsTable(codeContent, language);
            });

            // 2. Code blocks — <pre><code class="language-xxx">
            html = PreCodeWithLangRegex.Replace(html, match =>
            {
                var language = match.Groups[1].Value.Trim();
                var code = match.Groups[2].Value;
                return _codeBlockRenderer.RenderCodeBlockAsTable(code, language);
            });

            // 3. Code blocks — plain <pre><code>
            html = PreCodePlainRegex.Replace(html, match =>
            {
                var code = match.Groups[1].Value;
                return _codeBlockRenderer.RenderCodeBlockAsTable(code, null);
            });

            // 4. Blockquotes — may be nested, process iteratively
            while (BlockquoteRegex.IsMatch(html))
            {
                html = BlockquoteRegex.Replace(html, match =>
                {
                    var inner = match.Groups[1].Value.Trim();
                    return $"<p style=\"margin-left:28px;border-left:3px solid #ccc;padding-left:12px;color:#555\">{inner}</p>";
                });
            }

            // 5. Horizontal rules
            html = HrRegex.Replace(html, "<p style=\"border-bottom:1px solid #ccc\">&nbsp;</p>");

            // 6. Checkboxes (checked first to avoid double-matching)
            html = CheckedBoxRegex.Replace(html, "\u2611");
            html = UncheckedBoxRegex.Replace(html, "\u2610");

            // 7. Mark/highlight
            html = MarkRegex.Replace(html, "<span style=\"background-color:#ffff00\">$1</span>");

            // 8. Headings — add font-size styling
            html = HeadingRegex.Replace(html, match =>
            {
                var level = int.Parse(match.Groups[1].Value);
                var attrs = match.Groups[2].Value;
                var content = match.Groups[3].Value;
                var style = GetHeadingStyle(level);
                return $"<p style=\"{style}\"{attrs}>{content}</p>";
            });

            // 9. Strip <thead>/<tbody> (OneNote doesn't support them)
            html = TheadTbodyRegex.Replace(html, "");

            // 10. Tables — add border styles if not already present
            html = TableOpenRegex.Replace(html, "<table style=\"border-collapse:collapse;margin:8px 0\"$1>");
            html = TdOpenRegex.Replace(html, "<td style=\"border:1px solid #ccc;padding:6px 10px\"$1>");

            // 11. <th> → <td> with bold (OneNote doesn't support <th>)
            html = ThOpenRegex.Replace(html, match =>
            {
                var attrs = match.Groups[1].Value;
                return $"<td style=\"border:1px solid #ccc;padding:6px 10px;font-weight:bold\"{attrs}>";
            });
            html = ThCloseRegex.Replace(html, "</td>");

            return html;
        }

        private static string GetHeadingStyle(int level)
        {
            switch (level)
            {
                case 1: return "font-size:20pt;font-weight:bold";
                case 2: return "font-size:16pt;font-weight:bold";
                case 3: return "font-size:13pt;font-weight:bold";
                case 4: return "font-size:11pt;font-weight:bold";
                case 5: return "font-size:10pt;font-weight:bold";
                case 6: return "font-size:9pt;font-weight:bold";
                default: return "font-weight:bold";
            }
        }
    }
}
