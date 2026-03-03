using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace MDNote.Core
{
    /// <summary>
    /// Converts standard web HTML to Markdown.
    /// Handles output from Claude.ai, ChatGPT, web browsers, Stack Overflow, and other sources.
    /// Different from <see cref="OneNoteHtmlToMarkdown"/> which reverses OneNote-specific HTML patterns.
    /// </summary>
    public class WebHtmlToMarkdown
    {
        // Blocks to strip entirely (content and all)
        private static readonly Regex ScriptStyleRegex = new Regex(
            @"<(script|style|noscript|head)[^>]*>[\s\S]*?</\1>",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Code blocks: <pre><code class="language-xxx"> or <pre><code class="hljs language-xxx">
        // Also handles bare <pre><code> and <pre> without <code>
        private static readonly Regex PreCodeWithLangRegex = new Regex(
            @"<pre[^>]*>\s*<code[^>]*(?:class=""[^""]*?(?:language-|hljs\s+language-)(\w+)[^""]*"")[^>]*>([\s\S]*?)</code>\s*</pre>",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex PreCodeRegex = new Regex(
            @"<pre[^>]*>\s*<code[^>]*>([\s\S]*?)</code>\s*</pre>",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex PreBareRegex = new Regex(
            @"<pre[^>]*>([\s\S]*?)</pre>",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Headings
        private static readonly Regex HeadingRegex = new Regex(
            @"<h([1-6])[^>]*>([\s\S]*?)</h\1>",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Tables
        private static readonly Regex TableRegex = new Regex(
            @"<table[^>]*>([\s\S]*?)</table>",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex TableRowRegex = new Regex(
            @"<tr[^>]*>([\s\S]*?)</tr>",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex TableHeaderCellRegex = new Regex(
            @"<th[^>]*>([\s\S]*?)</th>",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex TableDataCellRegex = new Regex(
            @"<t[dh][^>]*>([\s\S]*?)</t[dh]>",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Blockquotes (matches innermost first — no nested <blockquote> inside content)
        private static readonly Regex BlockquoteRegex = new Regex(
            @"<blockquote[^>]*>((?:(?!<blockquote)[\s\S])*?)</blockquote>",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Lists
        private static readonly Regex UlRegex = new Regex(
            @"<ul[^>]*>([\s\S]*?)</ul>",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex OlRegex = new Regex(
            @"<ol[^>]*>([\s\S]*?)</ol>",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex LiRegex = new Regex(
            @"<li[^>]*>([\s\S]*?)</li>",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Horizontal rule
        private static readonly Regex HrRegex = new Regex(
            @"<hr\s*/?>", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Inline formatting
        private static readonly Regex StrongRegex = new Regex(
            @"<(?:strong|b)(?:\s[^>]*)?>(?!$)([\s\S]*?)</(?:strong|b)>",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex EmRegex = new Regex(
            @"<(?:em|i)(?:\s[^>]*)?>(?!$)([\s\S]*?)</(?:em|i)>",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex DelRegex = new Regex(
            @"<(?:del|s|strike)(?:\s[^>]*)?>(?!$)([\s\S]*?)</(?:del|s|strike)>",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex InlineCodeRegex = new Regex(
            @"<code(?:\s[^>]*)?>([^<]*?)</code>",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex MarkRegex = new Regex(
            @"<mark(?:\s[^>]*)?>(?!$)([\s\S]*?)</mark>",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Links and images
        private static readonly Regex LinkRegex = new Regex(
            @"<a\s[^>]*href=""([^""]*?)""[^>]*>([\s\S]*?)</a>",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex ImgWithAltRegex = new Regex(
            @"<img\s[^>]*src=""([^""]*?)""[^>]*alt=""([^""]*?)""[^>]*/?>",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex ImgAltFirstRegex = new Regex(
            @"<img\s[^>]*alt=""([^""]*?)""[^>]*src=""([^""]*?)""[^>]*/?>",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex ImgNoAltRegex = new Regex(
            @"<img\s[^>]*src=""([^""]*?)""[^>]*/?>",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Paragraphs and line breaks
        private static readonly Regex ParagraphRegex = new Regex(
            @"<p[^>]*>([\s\S]*?)</p>",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex BrRegex = new Regex(
            @"<br\s*/?>", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Container tags to strip (keep content)
        private static readonly Regex ContainerTagRegex = new Regex(
            @"</?(?:div|span|section|article|main|nav|header|footer|figure|figcaption|aside|details|summary|dd|dt|dl)(?:\s[^>]*)?>",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Generic HTML tag stripping
        private static readonly Regex HtmlTagRegex = new Regex(
            @"<[^>]+>", RegexOptions.Compiled);

        // Syntax highlighting spans inside code (Highlight.js, Prism, etc.)
        private static readonly Regex SyntaxSpanRegex = new Regex(
            @"<span[^>]*>([\s\S]*?)</span>", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Multiple blank lines cleanup
        private static readonly Regex MultipleBlankLinesRegex = new Regex(
            @"\n{3,}", RegexOptions.Compiled);

        // HTML comments
        private static readonly Regex HtmlCommentRegex = new Regex(
            @"<!--[\s\S]*?-->", RegexOptions.Compiled);

        /// <summary>
        /// Converts web HTML to Markdown.
        /// </summary>
        public string Convert(string html)
        {
            if (string.IsNullOrEmpty(html))
                return string.Empty;

            var result = html;

            // 1. Strip script, style, noscript, head blocks
            result = ScriptStyleRegex.Replace(result, "");

            // 2. Strip HTML comments
            result = HtmlCommentRegex.Replace(result, "");

            // 3. Code blocks → placeholders (protect from further processing)
            var codeBlocks = new List<string>();
            result = PreCodeWithLangRegex.Replace(result, match =>
            {
                var lang = match.Groups[1].Value.ToLowerInvariant();
                var code = StripCodeSpans(match.Groups[2].Value);
                code = WebUtility.HtmlDecode(code);
                var index = codeBlocks.Count;
                codeBlocks.Add($"\n```{lang}\n{code.TrimEnd()}\n```\n");
                return $"\x00CODEBLOCK_{index}\x00";
            });

            result = PreCodeRegex.Replace(result, match =>
            {
                var code = StripCodeSpans(match.Groups[1].Value);
                code = WebUtility.HtmlDecode(code);
                var index = codeBlocks.Count;
                codeBlocks.Add($"\n```\n{code.TrimEnd()}\n```\n");
                return $"\x00CODEBLOCK_{index}\x00";
            });

            result = PreBareRegex.Replace(result, match =>
            {
                var code = StripCodeSpans(match.Groups[1].Value);
                code = WebUtility.HtmlDecode(code);
                var index = codeBlocks.Count;
                codeBlocks.Add($"\n```\n{code.TrimEnd()}\n```\n");
                return $"\x00CODEBLOCK_{index}\x00";
            });

            // 4. Headings
            result = HeadingRegex.Replace(result, match =>
            {
                var level = int.Parse(match.Groups[1].Value);
                var content = match.Groups[2].Value;
                content = ProcessInlineFormatting(content);
                content = HtmlTagRegex.Replace(content, "");
                content = WebUtility.HtmlDecode(content).Trim();
                var prefix = new string('#', level);
                return $"\n{prefix} {content}\n";
            });

            // 5. Tables
            result = TableRegex.Replace(result, ConvertTable);

            // 6. Blockquotes (iterative for nesting)
            int maxIter = 10;
            while (maxIter-- > 0 && BlockquoteRegex.IsMatch(result))
            {
                result = BlockquoteRegex.Replace(result, match =>
                {
                    var inner = match.Groups[1].Value.Trim();
                    // Process paragraphs inside blockquote
                    inner = ParagraphRegex.Replace(inner, m => m.Groups[1].Value.Trim() + "\n\n");
                    inner = BrRegex.Replace(inner, "\n");
                    inner = inner.Trim();
                    var lines = inner.Split(new[] { '\n' }, StringSplitOptions.None);
                    var sb = new StringBuilder();
                    foreach (var line in lines)
                    {
                        sb.Append("> ");
                        sb.AppendLine(line.TrimEnd());
                    }
                    return "\n" + sb.ToString().TrimEnd() + "\n";
                });
            }

            // 7. Lists (iterative for nesting)
            result = ConvertLists(result);

            // 8. Horizontal rules
            result = HrRegex.Replace(result, "\n---\n");

            // 9. Inline code → placeholders (decoded entities may produce HTML-like text)
            var inlineCodes = new List<string>();
            result = InlineCodeRegex.Replace(result, match =>
            {
                var code = WebUtility.HtmlDecode(match.Groups[1].Value);
                var index = inlineCodes.Count;
                inlineCodes.Add($"`{code}`");
                return $"\x00INLINECODE_{index}\x00";
            });

            // 10. Inline formatting
            result = ProcessInlineFormatting(result);

            // 11. Paragraphs → newlines
            result = ParagraphRegex.Replace(result, match =>
            {
                var content = match.Groups[1].Value.Trim();
                return content + "\n\n";
            });

            // 12. Line breaks
            result = BrRegex.Replace(result, "\n");

            // 13. Strip container tags (keep content)
            result = ContainerTagRegex.Replace(result, "");

            // 14. Strip any remaining HTML tags
            result = HtmlTagRegex.Replace(result, "");

            // 15. HTML entity decode
            result = WebUtility.HtmlDecode(result);

            // 16. Restore code blocks and inline code from placeholders
            for (int i = 0; i < codeBlocks.Count; i++)
                result = result.Replace($"\x00CODEBLOCK_{i}\x00", codeBlocks[i]);
            for (int i = 0; i < inlineCodes.Count; i++)
                result = result.Replace($"\x00INLINECODE_{i}\x00", inlineCodes[i]);

            // 17. Cleanup: collapse blank lines, trim
            result = MultipleBlankLinesRegex.Replace(result, "\n\n");
            return result.Trim();
        }

        private static string StripCodeSpans(string codeHtml)
        {
            // Remove syntax highlighting spans but keep their text
            var result = SyntaxSpanRegex.Replace(codeHtml, "$1");
            // Remove any other HTML tags inside code
            result = HtmlTagRegex.Replace(result, "");
            return result;
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

            // Images (handle both src-before-alt and alt-before-src attribute orderings)
            html = ImgWithAltRegex.Replace(html, "![$2]($1)");
            html = ImgAltFirstRegex.Replace(html, "![$1]($2)");
            html = ImgNoAltRegex.Replace(html, "![]($1)");

            // Mark/highlight
            html = MarkRegex.Replace(html, "==$1==");

            // Bold, italic, strikethrough
            html = StrongRegex.Replace(html, "**$1**");
            html = EmRegex.Replace(html, "*$1*");
            html = DelRegex.Replace(html, "~~$1~~");

            return html;
        }

        private string ConvertTable(Match match)
        {
            var tableContent = match.Groups[1].Value;
            var rows = TableRowRegex.Matches(tableContent);
            if (rows.Count == 0)
                return "";

            var parsedRows = new List<List<string>>();

            for (int r = 0; r < rows.Count; r++)
            {
                var rowHtml = rows[r].Groups[1].Value;
                var cells = TableDataCellRegex.Matches(rowHtml);
                var row = new List<string>();

                foreach (Match cell in cells)
                {
                    var cellContent = cell.Groups[1].Value;
                    cellContent = ProcessInlineFormatting(cellContent);
                    cellContent = HtmlTagRegex.Replace(cellContent, "");
                    cellContent = WebUtility.HtmlDecode(cellContent).Trim();
                    row.Add(cellContent);
                }

                if (row.Count > 0)
                    parsedRows.Add(row);
            }

            if (parsedRows.Count == 0)
                return "";

            int colCount = 0;
            foreach (var row in parsedRows)
                if (row.Count > colCount)
                    colCount = row.Count;

            if (colCount == 0)
                return "";

            var sb = new StringBuilder("\n");

            // Header row
            var headerRow = parsedRows[0];
            sb.Append("| ");
            for (int c = 0; c < colCount; c++)
            {
                sb.Append(c < headerRow.Count ? headerRow[c] : "");
                sb.Append(" | ");
            }
            sb.AppendLine();

            // Separator
            sb.Append("| ");
            for (int c = 0; c < colCount; c++)
                sb.Append("--- | ");
            sb.AppendLine();

            // Data rows
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

        private static string ConvertLists(string html)
        {
            // Process lists iteratively (handles nesting by processing innermost first)
            int maxIter = 10;
            while (maxIter-- > 0 && (UlRegex.IsMatch(html) || OlRegex.IsMatch(html)))
            {
                // Process unordered lists
                html = UlRegex.Replace(html, match =>
                {
                    return ConvertListItems(match.Groups[1].Value, false);
                });

                // Process ordered lists
                html = OlRegex.Replace(html, match =>
                {
                    return ConvertListItems(match.Groups[1].Value, true);
                });
            }

            return html;
        }

        private static string ConvertListItems(string listContent, bool ordered)
        {
            var items = LiRegex.Matches(listContent);
            var sb = new StringBuilder("\n");
            int num = 1;

            foreach (Match item in items)
            {
                var text = item.Groups[1].Value;

                // Check for task list checkbox
                var checkboxMatch = Regex.Match(text,
                    @"^\s*<input[^>]*type=""checkbox""[^>]*/?>",
                    RegexOptions.IgnoreCase);
                if (checkboxMatch.Success)
                {
                    text = text.Substring(checkboxMatch.Index + checkboxMatch.Length);
                    var isChecked = Regex.IsMatch(checkboxMatch.Value,
                        @"\bchecked\b", RegexOptions.IgnoreCase);
                    var itemText = HtmlTagRegex.Replace(text, "");
                    itemText = WebUtility.HtmlDecode(itemText).Trim();
                    sb.AppendLine(isChecked ? $"- [x] {itemText}" : $"- [ ] {itemText}");
                }
                else
                {
                    // Process inline formatting (bold, links, etc.) before stripping tags
                    text = ProcessInlineFormatting(text);
                    text = ParagraphRegex.Replace(text, m => m.Groups[1].Value.Trim());
                    text = HtmlTagRegex.Replace(text, "");
                    text = WebUtility.HtmlDecode(text).Trim();

                    var prefix = ordered ? $"{num}. " : "- ";
                    sb.AppendLine($"{prefix}{text}");
                    num++;
                }
            }

            return sb.ToString();
        }
    }
}
