using System;
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

        // Footnote reference: <a ... class="footnote-ref" ...><sup>N</sup></a>
        private static readonly Regex FootnoteRefRegex = new Regex(
            @"<a[^>]*class=""footnote-ref""[^>]*><sup>(\d+)</sup></a>",
            RegexOptions.Compiled);

        // Footnote section: <div class="footnotes">...</div>
        private static readonly Regex FootnoteSectionRegex = new Regex(
            @"<div\s+class=""footnotes"">([\s\S]*?)</div>\s*$",
            RegexOptions.Compiled);

        // Footnote back-reference links
        private static readonly Regex FootnoteBackRefRegex = new Regex(
            @"<a[^>]*class=""footnote-back-ref""[^>]*>[^<]*</a>",
            RegexOptions.Compiled);

        // Definition lists
        private static readonly Regex DlRegex = new Regex(
            @"<dl>([\s\S]*?)</dl>",
            RegexOptions.Compiled);

        private static readonly Regex DtRegex = new Regex(
            @"<dt>([\s\S]*?)</dt>",
            RegexOptions.Compiled);

        private static readonly Regex DdRegex = new Regex(
            @"<dd>([\s\S]*?)</dd>",
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

        // <th> with existing style attribute (e.g., text-align from Markdig)
        private static readonly Regex ThWithStyleRegex = new Regex(
            @"<th\s+style=""([^""]*)""([^>]*)>",
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

        // Blockquote border colors for nesting levels (innermost → outermost)
        private static readonly string[] BlockquoteBorderColors =
            { "#4a9eff", "#7b68ee", "#ff7043", "#66bb6a", "#ffa726", "#ccc" };

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

            // 4. Blockquotes — may be nested, process iteratively with increasing indent
            int blockquoteDepth = 0;
            while (BlockquoteRegex.IsMatch(html))
            {
                var depth = blockquoteDepth;
                html = BlockquoteRegex.Replace(html, match =>
                {
                    var inner = match.Groups[1].Value.Trim();
                    var indent = 28 + (depth * 20);
                    var colorIndex = Math.Min(depth, BlockquoteBorderColors.Length - 1);
                    var borderColor = BlockquoteBorderColors[colorIndex];
                    return $"<p style=\"margin-left:{indent}px;border-left:3px solid {borderColor};" +
                           $"padding-left:12px;color:#555\">{inner}</p>";
                });
                blockquoteDepth++;
            }

            // 5. Horizontal rules
            html = HrRegex.Replace(html, "<p style=\"border-bottom:1px solid #ccc\">&nbsp;</p>");

            // 6. Checkboxes (checked first to avoid double-matching) — with NBSP spacing
            html = CheckedBoxRegex.Replace(html, "\u2611\u00A0");
            html = UncheckedBoxRegex.Replace(html, "\u2610\u00A0");

            // 7. Mark/highlight
            html = MarkRegex.Replace(html, "<span style=\"background-color:#ffff00\">$1</span>");

            // 8. Footnote references — styled superscript
            html = FootnoteRefRegex.Replace(html, "<sup style=\"font-size:8pt;color:#4a9eff\">[$1]</sup>");

            // 9. Footnote section — styled container
            html = FootnoteSectionRegex.Replace(html, match =>
            {
                var inner = match.Groups[1].Value;
                inner = FootnoteBackRefRegex.Replace(inner, "");
                return "<p style=\"border-top:1px solid #ccc;margin-top:16px;padding-top:8px;" +
                       "font-size:9pt;color:#666\">" +
                       "<span style=\"font-weight:bold\">Footnotes</span></p>" + inner;
            });

            // 10. Definition lists — convert <dl>/<dt>/<dd> to styled paragraphs
            html = DlRegex.Replace(html, match =>
            {
                var inner = match.Groups[1].Value;
                inner = DtRegex.Replace(inner, "<p style=\"font-weight:bold;margin:8px 0 2px 0\">$1</p>");
                inner = DdRegex.Replace(inner, "<p style=\"margin:2px 0 8px 28px;color:#555\">$1</p>");
                return inner;
            });

            // 11. Headings — add font-size styling
            html = HeadingRegex.Replace(html, match =>
            {
                var level = int.Parse(match.Groups[1].Value);
                var attrs = match.Groups[2].Value;
                var content = match.Groups[3].Value;
                var style = GetHeadingStyle(level);
                return $"<p style=\"{style}\"{attrs}>{content}</p>";
            });

            // 12. Strip <thead>/<tbody> (OneNote doesn't support them)
            html = TheadTbodyRegex.Replace(html, "");

            // 13. Tables — add border styles if not already present
            html = TableOpenRegex.Replace(html, "<table style=\"border-collapse:collapse;margin:8px 0\"$1>");
            html = TdOpenRegex.Replace(html, "<td style=\"border:1px solid #ccc;padding:6px 10px\"$1>");

            // 14. <th> → <td> with bold + header bottom border
            //     First handle <th> with existing style (preserves text-align from Markdig)
            html = ThWithStyleRegex.Replace(html, match =>
            {
                var existingStyle = match.Groups[1].Value;
                var otherAttrs = match.Groups[2].Value;
                return $"<td style=\"border:1px solid #ccc;padding:6px 10px;" +
                       $"font-weight:bold;border-bottom:2px solid #999;{existingStyle}\"{otherAttrs}>";
            });
            //     Then handle <th> without style
            html = ThOpenRegex.Replace(html, match =>
            {
                var attrs = match.Groups[1].Value;
                return $"<td style=\"border:1px solid #ccc;padding:6px 10px;" +
                       $"font-weight:bold;border-bottom:2px solid #999\"{attrs}>";
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
