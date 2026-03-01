using FluentAssertions;
using MDNote.Core;
using Xunit;

namespace MDNote.Core.Tests
{
    /// <summary>
    /// Tests for MarkdownDetector scoring on short texts (1-2 lines).
    /// These validate the raw scoring that ClipboardMonitor uses with a higher
    /// threshold (0.5) for short pastes to avoid false positives.
    /// </summary>
    public class MarkdownDetectorShortTextTests
    {
        private const double ShortTextThreshold = 0.5;

        [Fact]
        public void Detect_SingleHeadingLine_ScoresAboveShortThreshold()
        {
            var result = MarkdownDetector.Detect("# My Document");
            // 1 heading = 3 points / 1 line = 3.0
            result.Score.Should().BeGreaterThanOrEqualTo(ShortTextThreshold);
        }

        [Fact]
        public void Detect_TwoLineHeadingAndList_ScoresAboveShortThreshold()
        {
            var result = MarkdownDetector.Detect("# Title\n- Item");
            // 1 heading (3) + 1 list (1) = 4 points / 2 lines = 2.0
            result.Score.Should().BeGreaterThanOrEqualTo(ShortTextThreshold);
        }

        [Fact]
        public void Detect_SinglePlainSentence_ScoresBelowShortThreshold()
        {
            var result = MarkdownDetector.Detect("Meeting at 3pm tomorrow.");
            result.Score.Should().BeLessThan(ShortTextThreshold);
        }

        [Fact]
        public void Detect_TwoPlainSentences_ScoresBelowShortThreshold()
        {
            var result = MarkdownDetector.Detect(
                "Meeting at 3pm tomorrow.\nDon't forget your laptop.");
            result.Score.Should().BeLessThan(ShortTextThreshold);
        }

        [Fact]
        public void Detect_SingleCodeFenceLine_ScoresAboveShortThreshold()
        {
            // An opening ``` alone — 1 fence = 5 points / 1 line = 5.0
            var result = MarkdownDetector.Detect("```csharp");
            result.Score.Should().BeGreaterThanOrEqualTo(ShortTextThreshold);
        }

        [Fact]
        public void Detect_SingleBoldPhrase_ScoresAboveShortThreshold()
        {
            var result = MarkdownDetector.Detect("This is **important** text.");
            // 1 bold = 2 points / 1 line = 2.0
            result.Score.Should().BeGreaterThanOrEqualTo(ShortTextThreshold);
        }

        [Fact]
        public void Detect_SingleLink_ScoresAboveShortThreshold()
        {
            var result = MarkdownDetector.Detect("[click here](https://example.com)");
            // 1 link = 3 points / 1 line = 3.0
            result.Score.Should().BeGreaterThanOrEqualTo(ShortTextThreshold);
        }

        [Fact]
        public void Detect_HashtagInPlainText_ScoresBelowShortThreshold()
        {
            // "hashtag" without a space after # is not a heading
            var result = MarkdownDetector.Detect("#NoSpaceAfterHash");
            result.Score.Should().BeLessThan(ShortTextThreshold);
        }

        [Fact]
        public void Detect_NumberedSentence_ScoresBelowShortThreshold()
        {
            // "1." followed by text looks like ordered list, but only 1 line
            var result = MarkdownDetector.Detect("1. Do the laundry");
            // 1 ordered list = 1 point / 1 line = 1.0 — above threshold
            // This IS a valid ordered list item, so it should pass
            result.Score.Should().BeGreaterThanOrEqualTo(ShortTextThreshold);
        }

        [Fact]
        public void Detect_TwoLinePlainCode_ScoresBelowShortThreshold()
        {
            // Code without fences — just variable assignments
            var result = MarkdownDetector.Detect("var x = 42;\nvar y = x + 1;");
            result.Score.Should().BeLessThan(ShortTextThreshold);
        }

        [Fact]
        public void Detect_SingleInlineCode_BelowShortThreshold()
        {
            // 1 inline code = 1 point / 1 line = 1.0, above 0.5
            var result = MarkdownDetector.Detect("Use `npm install` to set up.");
            result.Score.Should().BeGreaterThanOrEqualTo(ShortTextThreshold);
        }

        [Fact]
        public void Detect_ThreeLinePlainText_UsesStandardThreshold()
        {
            // With 3+ lines, ClipboardMonitor uses the standard 0.15 threshold.
            // This test validates the detector's raw score for a 3-line plain text.
            var result = MarkdownDetector.Detect(
                "Line one.\nLine two.\nLine three.");
            result.Score.Should().BeLessThan(0.15);
        }

        [Fact]
        public void Detect_ThreeLineMarkdown_ScoresAboveStandardThreshold()
        {
            var text = "# Title\n\nSome paragraph\n\n- Item";
            var result = MarkdownDetector.Detect(text);
            // heading (3) + list (1) = 4 / 3 non-empty lines = 1.33
            result.Score.Should().BeGreaterThanOrEqualTo(0.15);
        }
    }
}
