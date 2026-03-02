using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace MDNote.Core
{
    /// <summary>
    /// Best-effort reverse conversion from OneNote-rendered HTML back to Markdown.
    /// Reverses the transformations applied by <see cref="HtmlToOneNoteConverter"/>
    /// and <see cref="CodeBlockRenderer"/>. Used as a fallback when no stored
    /// markdown source is available.
    /// </summary>
    public class OneNoteHtmlToMarkdown
    {
        // Code block table: single-cell table with Consolas font and white-space:pre
        // Optionally preceded by a label row with background:#2d2d2d
        private static readonly Regex CodeBlockTableRegex = new Regex(
            @"<table[^>]*style=""[^""]*border-collapse:collapse;width:100%[^""]*""[^>]*>" +
            @"(?:<tr><td[^>]*background:#2d2d2d[^>]*>([^<]*)</td></tr>)?" +
            @"<tr><td[^>]*white-space:pre[^>]*>([\s\S]*?)</td></tr></table>",
            RegexOptions.Compiled);

        // Heading: <p style="[font-family:Calibri;]font-size:XXpt;font-weight:bold">
        private static readonly Regex HeadingRegex = new Regex(
            @"<p\s+style=""(?:font-family:[^;]+;)?font-size:(\d+)pt;font-weight:bold""[^>]*>([\s\S]*?)</p>",
            RegexOptions.Compiled);

        // Blockquote: <p style="margin-left:NNpx;border-left:3px solid #COLOR;...">
        private static readonly Regex BlockquoteRegex = new Regex(
            @"<p\s+style=""[^""]*border-left:3px solid #[A-Fa-f0-9]+[^""]*"">([\s\S]*?)</p>",
            RegexOptions.Compiled);

        // Definition list term: <p style="font-weight:bold;margin:8px 0 2px 0">
        private static readonly Regex DefTermRegex = new Regex(
            @"<p\s+style=""font-weight:bold;margin:8px 0 2px 0"">([\s\S]*?)</p>",
            RegexOptions.Compiled);

        // Definition list description: <p style="margin:2px 0 8px 28px;color:#555">
        private static readonly Regex DefDescRegex = new Regex(
            @"<p\s+style=""margin:2px 0 8px 28px;color:#555"">([\s\S]*?)</p>",
            RegexOptions.Compiled);

        // Horizontal rule
        private static readonly Regex HrRegex = new Regex(
            @"<p\s+style=""border-bottom:1px solid #ccc"">&nbsp;</p>",
            RegexOptions.Compiled);

        // Highlight span
        private static readonly Regex HighlightRegex = new Regex(
            @"<span\s+style=""background-color:#ffff00"">([\s\S]*?)</span>",
            RegexOptions.Compiled);

        // Regular (non-code) table
        private static readonly Regex DataTableRegex = new Regex(
            @"<table[^>]*style=""[^""]*border-collapse:collapse;margin:8px 0[^""]*""[^>]*>([\s\S]*?)</table>",
            RegexOptions.Compiled);

        // Table row
        private static readonly Regex TableRowRegex = new Regex(
            @"<tr>([\s\S]*?)</tr>",
            RegexOptions.Compiled);

        // Table cell (td with optional styles)
        private static readonly Regex TableCellRegex = new Regex(
            @"<td[^>]*>([\s\S]*?)</td>",
            RegexOptions.Compiled);

        // Bold header cell (font-weight:bold in td style)
        private static readonly Regex BoldCellRegex = new Regex(
            @"<td[^>]*font-weight:bold[^>]*>([\s\S]*?)</td>",
            RegexOptions.Compiled);

        // Inline formatting
        private static readonly Regex StrongRegex = new Regex(
            @"<(?:strong|b)>([\s\S]*?)</(?:strong|b)>", RegexOptions.Compiled);
        private static readonly Regex EmRegex = new Regex(
            @"<(?:em|i)>([\s\S]*?)</(?:em|i)>", RegexOptions.Compiled);
        private static readonly Regex DelRegex = new Regex(
            @"<del>([\s\S]*?)</del>", RegexOptions.Compiled);

        // Links and images
        private static readonly Regex LinkRegex = new Regex(
            @"<a\s+href=""([^""]*?)""[^>]*>([\s\S]*?)</a>", RegexOptions.Compiled);
        private static readonly Regex ImgRegex = new Regex(
            @"<img\s+[^>]*src=""([^""]*?)""[^>]*alt=""([^""]*?)""[^>]*/?>", RegexOptions.Compiled);
        private static readonly Regex ImgNoAltRegex = new Regex(
            @"<img\s+[^>]*src=""([^""]*?)""[^>]*/?>", RegexOptions.Compiled);

        // Lists
        private static readonly Regex UlRegex = new Regex(
            @"<ul>([\s\S]*?)</ul>", RegexOptions.Compiled);
        private static readonly Regex OlRegex = new Regex(
            @"<ol[^>]*>([\s\S]*?)</ol>", RegexOptions.Compiled);
        private static readonly Regex LiRegex = new Regex(
            @"<li>([\s\S]*?)</li>", RegexOptions.Compiled);

        // Color spans from syntax highlighting (inside code blocks)
        private static readonly Regex ColorSpanRegex = new Regex(
            @"<span\s+style=""color:#[A-Fa-f0-9]+"">([\s\S]*?)</span>", RegexOptions.Compiled);

        // Generic HTML tag stripping
        private static readonly Regex HtmlTagRegex = new Regex(
            @"<[^>]+>", RegexOptions.Compiled);

        // Paragraph tags
        private static readonly Regex ParagraphRegex = new Regex(
            @"<p[^>]*>([\s\S]*?)</p>", RegexOptions.Compiled);

        // Line break
        private static readonly Regex BrRegex = new Regex(
            @"<br\s*/?>", RegexOptions.Compiled);

        // Multiple blank lines cleanup
        private static readonly Regex MultipleBlankLinesRegex = new Regex(
            @"\n{3,}", RegexOptions.Compiled);

        // Heading font-size to level mapping
        private static readonly Dictionary<int, int> FontSizeToLevel = new Dictionary<int, int>
        {
            { 20, 1 }, { 18, 2 }, { 16, 3 }, { 14, 4 }, { 13, 3 }, { 12, 5 },
            { 11, 4 }, { 10, 5 }, { 9, 6 }
        };

        // Display name to language alias (reverse of SyntaxHighlighter.GetDisplayName)
        private static readonly Dictionary<string, string> DisplayNameToAlias =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "C#", "csharp" }, { "JavaScript", "javascript" }, { "TypeScript", "typescript" },
            { "Python", "python" }, { "Java", "java" }, { "SQL", "sql" }, { "JSON", "json" },
            { "XML", "xml" }, { "HTML", "html" }, { "CSS", "css" }, { "Bash", "bash" },
            { "PowerShell", "powershell" }, { "Rust", "rust" }, { "Go", "go" },
            { "YAML", "yaml" }, { "Ruby", "ruby" }, { "PHP", "php" }, { "Swift", "swift" },
            { "Kotlin", "kotlin" }, { "C++", "cpp" }, { "C", "c" },
            { "Dockerfile", "dockerfile" }, { "GraphQL", "graphql" }, { "TOML", "toml" },
            { "Diff", "diff" }, { "F#", "fsharp" }, { "Haskell", "haskell" },
            { "Markdown", "markdown" }, { "Plain Text", "text" }
        };

        /// <summary>
        /// Converts a list of HTML fragments (from OneNote OE/T elements) to Markdown.
        /// </summary>
        public string Convert(List<string> htmlFragments)
        {
            if (htmlFragments == null || htmlFragments.Count == 0)
                return string.Empty;

            var joined = string.Join("\n", htmlFragments);
            return Convert(joined);
        }

        /// <summary>
        /// Converts a single OneNote HTML string to Markdown.
        /// </summary>
        public string Convert(string html)
        {
            if (string.IsNullOrEmpty(html))
                return string.Empty;

            var result = html;

            // 1. Code block tables → placeholders (protects decoded entities from tag stripping)
            var codeBlocks = new List<string>();
            result = CodeBlockTableRegex.Replace(result, match =>
            {
                var md = ConvertCodeBlock(match);
                var index = codeBlocks.Count;
                codeBlocks.Add(md);
                return $"\x00CODEBLOCK_{index}\x00";
            });

            // 2. Regular data tables (non-code)
            result = DataTableRegex.Replace(result, ConvertDataTable);

            // 3. Horizontal rules (before paragraph processing)
            result = HrRegex.Replace(result, "\n---\n");

            // 4. Headings
            result = HeadingRegex.Replace(result, ConvertHeading);

            // 5. Definition lists (before blockquotes — both use styled <p>)
            result = DefTermRegex.Replace(result, match =>
            {
                var term = HtmlTagRegex.Replace(match.Groups[1].Value, "");
                term = WebUtility.HtmlDecode(term).Trim();
                return $"\n{term}";
            });
            result = DefDescRegex.Replace(result, match =>
            {
                var desc = HtmlTagRegex.Replace(match.Groups[1].Value, "");
                desc = WebUtility.HtmlDecode(desc).Trim();
                return $"\n:   {desc}\n";
            });

            // 6. Blockquotes
            result = BlockquoteRegex.Replace(result, match =>
            {
                var inner = match.Groups[1].Value;
                inner = ProcessInlineFormatting(inner);
                inner = StripRemainingTags(inner);
                var lines = inner.Split(new[] { '\n' }, StringSplitOptions.None);
                var sb = new StringBuilder();
                foreach (var line in lines)
                    sb.AppendLine("> " + line.Trim());
                return sb.ToString();
            });

            // 6. Lists (before paragraph stripping)
            result = UlRegex.Replace(result, ConvertUnorderedList);
            result = OlRegex.Replace(result, ConvertOrderedList);

            // 7. Checkbox unicode characters
            result = result.Replace("\u2611\u00A0", "- [x] ");
            result = result.Replace("\u2611", "- [x]");
            result = result.Replace("\u2610\u00A0", "- [ ] ");
            result = result.Replace("\u2610", "- [ ]");

            // 8. Highlight
            result = HighlightRegex.Replace(result, "==$1==");

            // 9. Inline formatting
            result = ProcessInlineFormatting(result);

            // 10. Paragraphs → newlines
            result = ParagraphRegex.Replace(result, match =>
            {
                var content = match.Groups[1].Value.Trim();
                return content + "\n\n";
            });

            // 11. Line breaks
            result = BrRegex.Replace(result, "\n");

            // 12. Strip remaining HTML tags
            result = StripRemainingTags(result);

            // 13. Restore code blocks from placeholders
            for (int i = 0; i < codeBlocks.Count; i++)
                result = result.Replace($"\x00CODEBLOCK_{i}\x00", codeBlocks[i]);

            // 14. Clean up
            result = MultipleBlankLinesRegex.Replace(result, "\n\n");
            return result.Trim();
        }

        private string ConvertCodeBlock(Match match)
        {
            var label = match.Groups[1].Value.Trim();
            var codeHtml = match.Groups[2].Value;

            // Strip syntax highlighting color spans
            var code = ColorSpanRegex.Replace(codeHtml, "$1");
            // Strip any remaining HTML tags inside code
            code = HtmlTagRegex.Replace(code, "");
            // Decode HTML entities
            code = WebUtility.HtmlDecode(code);

            // Resolve language alias from display name
            var lang = "";
            if (!string.IsNullOrEmpty(label))
            {
                lang = DisplayNameToAlias.TryGetValue(label, out var alias) ? alias : label.ToLowerInvariant();
            }

            return $"\n```{lang}\n{code}\n```\n";
        }

        private static string ConvertHeading(Match match)
        {
            var fontSize = int.Parse(match.Groups[1].Value);
            var content = match.Groups[2].Value;

            // Strip any inline HTML from heading content
            content = HtmlTagRegex.Replace(content, "");
            content = WebUtility.HtmlDecode(content).Trim();

            if (!FontSizeToLevel.TryGetValue(fontSize, out var level))
                level = 1;

            var prefix = new string('#', level);
            return $"\n{prefix} {content}\n";
        }

        private string ConvertDataTable(Match match)
        {
            var tableContent = match.Groups[1].Value;
            var rows = TableRowRegex.Matches(tableContent);
            if (rows.Count == 0)
                return match.Value;

            var parsedRows = new List<List<string>>();
            bool firstRowIsBold = false;

            for (int r = 0; r < rows.Count; r++)
            {
                var rowHtml = rows[r].Groups[1].Value;
                var cells = TableCellRegex.Matches(rowHtml);
                var row = new List<string>();

                // Check if first row has bold styling (header row)
                if (r == 0 && BoldCellRegex.IsMatch(rowHtml))
                    firstRowIsBold = true;

                foreach (Match cell in cells)
                {
                    var cellContent = cell.Groups[1].Value;
                    cellContent = ProcessInlineFormatting(cellContent);
                    cellContent = HtmlTagRegex.Replace(cellContent, "");
                    cellContent = WebUtility.HtmlDecode(cellContent).Trim();
                    row.Add(cellContent);
                }

                parsedRows.Add(row);
            }

            if (parsedRows.Count == 0)
                return "";

            // Determine column count
            int colCount = 0;
            foreach (var row in parsedRows)
                if (row.Count > colCount)
                    colCount = row.Count;

            if (colCount == 0)
                return "";

            var sb = new StringBuilder("\n");

            // First row (header)
            var headerRow = parsedRows[0];
            sb.Append("| ");
            for (int c = 0; c < colCount; c++)
            {
                sb.Append(c < headerRow.Count ? headerRow[c] : "");
                sb.Append(" | ");
            }
            sb.AppendLine();

            // Separator row
            sb.Append("| ");
            for (int c = 0; c < colCount; c++)
                sb.Append("--- | ");
            sb.AppendLine();

            // Data rows (skip first if it was header)
            int startRow = firstRowIsBold ? 1 : 0;

            // If first row is NOT bold, we already printed it as header,
            // but for a proper table we need all rows. Re-print from index 1.
            for (int r = 1; r < parsedRows.Count; r++)
            {
                var row = parsedRows[r];
                sb.Append("| ");
                for (int c = 0; c < colCount; c++)
                {
                    sb.Append(c < row.Count ? row[c] : "");
                    sb.Append(" | ");
                }
                sb.AppendLine();
            }

            return sb.ToString();
        }

        private string ConvertUnorderedList(Match match)
        {
            var listContent = match.Groups[1].Value;
            var items = LiRegex.Matches(listContent);
            var sb = new StringBuilder("\n");
            foreach (Match item in items)
            {
                var text = item.Groups[1].Value;
                text = ProcessInlineFormatting(text);
                text = HtmlTagRegex.Replace(text, "");
                text = WebUtility.HtmlDecode(text).Trim();
                sb.AppendLine($"- {text}");
            }
            return sb.ToString();
        }

        private string ConvertOrderedList(Match match)
        {
            var listContent = match.Groups[1].Value;
            var items = LiRegex.Matches(listContent);
            var sb = new StringBuilder("\n");
            int num = 1;
            foreach (Match item in items)
            {
                var text = item.Groups[1].Value;
                text = ProcessInlineFormatting(text);
                text = HtmlTagRegex.Replace(text, "");
                text = WebUtility.HtmlDecode(text).Trim();
                sb.AppendLine($"{num}. {text}");
                num++;
            }
            return sb.ToString();
        }

        private static string ProcessInlineFormatting(string html)
        {
            // Links (before stripping tags)
            html = LinkRegex.Replace(html, match =>
            {
                var href = match.Groups[1].Value;
                var text = HtmlTagRegex.Replace(match.Groups[2].Value, "");
                text = WebUtility.HtmlDecode(text);
                return $"[{text}]({href})";
            });

            // Images (with alt first, then without)
            html = ImgRegex.Replace(html, "![$2]($1)");
            html = ImgNoAltRegex.Replace(html, "![]($1)");

            // Bold, italic, strikethrough
            html = StrongRegex.Replace(html, "**$1**");
            html = EmRegex.Replace(html, "*$1*");
            html = DelRegex.Replace(html, "~~$1~~");

            return html;
        }

        private static string StripRemainingTags(string html)
        {
            html = HtmlTagRegex.Replace(html, "");
            return WebUtility.HtmlDecode(html);
        }
    }
}
