using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using MDNote.Core.Models;

namespace MDNote.Core
{
    /// <summary>
    /// Heuristic detection of whether text content is Markdown.
    /// </summary>
    public static class MarkdownDetector
    {
        private const double Threshold = 0.15;

        // Patterns matched per-line (multiline mode)
        private static readonly Regex HeadingRegex = new Regex(
            @"^#{1,6}\s", RegexOptions.Compiled | RegexOptions.Multiline);

        private static readonly Regex UnorderedListRegex = new Regex(
            @"^[\t ]*[-*+]\s", RegexOptions.Compiled | RegexOptions.Multiline);

        private static readonly Regex OrderedListRegex = new Regex(
            @"^[\t ]*\d+\.\s", RegexOptions.Compiled | RegexOptions.Multiline);

        private static readonly Regex FencedCodeRegex = new Regex(
            @"^`{3,}", RegexOptions.Compiled | RegexOptions.Multiline);

        private static readonly Regex TableRegex = new Regex(
            @"^\|.+\|", RegexOptions.Compiled | RegexOptions.Multiline);

        private static readonly Regex BlockquoteRegex = new Regex(
            @"^>\s", RegexOptions.Compiled | RegexOptions.Multiline);

        // Patterns matched anywhere in text
        private static readonly Regex BoldRegex = new Regex(
            @"\*\*[^*]+\*\*|__[^_]+__", RegexOptions.Compiled);

        private static readonly Regex LinkRegex = new Regex(
            @"\[[^\]]+\]\([^)]+\)", RegexOptions.Compiled);

        private static readonly Regex ImageRegex = new Regex(
            @"!\[[^\]]*\]\([^)]+\)", RegexOptions.Compiled);

        private static readonly Regex InlineCodeRegex = new Regex(
            @"`[^`]+`", RegexOptions.Compiled);

        /// <summary>
        /// Analyzes text for markdown patterns and returns a detection result.
        /// </summary>
        public static MarkdownDetectionResult Detect(string text)
        {
            var result = new MarkdownDetectionResult();

            if (string.IsNullOrWhiteSpace(text))
                return result;

            var lines = text.Split(new[] { '\r', '\n' },
                StringSplitOptions.RemoveEmptyEntries);
            int lineCount = Math.Max(lines.Length, 1);
            int totalPoints = 0;

            totalPoints += ScorePattern(HeadingRegex, text, 3, "headings", result.Indicators);
            totalPoints += ScorePattern(UnorderedListRegex, text, 1, "unordered list items", result.Indicators);
            totalPoints += ScorePattern(OrderedListRegex, text, 1, "ordered list items", result.Indicators);
            totalPoints += ScorePattern(FencedCodeRegex, text, 5, "code fences", result.Indicators);
            totalPoints += ScorePattern(TableRegex, text, 2, "table rows", result.Indicators);
            totalPoints += ScorePattern(BlockquoteRegex, text, 1, "blockquotes", result.Indicators);
            totalPoints += ScorePattern(BoldRegex, text, 2, "bold text", result.Indicators);
            totalPoints += ScorePattern(LinkRegex, text, 3, "links", result.Indicators);
            totalPoints += ScorePattern(ImageRegex, text, 3, "images", result.Indicators);
            totalPoints += ScorePattern(InlineCodeRegex, text, 1, "inline code", result.Indicators);

            result.Score = (double)totalPoints / lineCount;
            result.IsMarkdown = result.Score >= Threshold;

            return result;
        }

        private static int ScorePattern(Regex pattern, string text,
            int pointsPerMatch, string label, List<string> indicators)
        {
            var matches = pattern.Matches(text);
            if (matches.Count == 0)
                return 0;

            int points = matches.Count * pointsPerMatch;
            indicators.Add($"{matches.Count} {label} (+{points})");
            return points;
        }
    }
}
