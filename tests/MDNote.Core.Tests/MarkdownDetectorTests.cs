using FluentAssertions;
using MDNote.Core;
using Xunit;

namespace MDNote.Core.Tests
{
    public class MarkdownDetectorTests
    {
        [Fact]
        public void Detect_ObviousMarkdown_IsMarkdownTrue()
        {
            var text = @"# My Document

This is a paragraph with **bold** text.

## Section One

- Item one
- Item two
- Item three

```csharp
var x = 42;
```

[Click here](https://example.com) for more info.

> This is a blockquote.
";

            var result = MarkdownDetector.Detect(text);
            result.IsMarkdown.Should().BeTrue();
            result.Score.Should().BeGreaterThanOrEqualTo(0.15);
            result.Indicators.Should().NotBeEmpty();
        }

        [Fact]
        public void Detect_PlainText_IsMarkdownFalse()
        {
            var text = @"Hello world. This is a paragraph.
Another line of regular text here.
Nothing special about this content at all.
Just plain sentences without any formatting.";

            var result = MarkdownDetector.Detect(text);
            result.IsMarkdown.Should().BeFalse();
        }

        [Fact]
        public void Detect_EmptyString_IsMarkdownFalse()
        {
            var result = MarkdownDetector.Detect("");
            result.IsMarkdown.Should().BeFalse();
            result.Score.Should().Be(0);
        }

        [Fact]
        public void Detect_NullString_IsMarkdownFalse()
        {
            var result = MarkdownDetector.Detect(null);
            result.IsMarkdown.Should().BeFalse();
            result.Score.Should().Be(0);
        }

        [Fact]
        public void Detect_WhitespaceOnly_IsMarkdownFalse()
        {
            var result = MarkdownDetector.Detect("   \n  \n   ");
            result.IsMarkdown.Should().BeFalse();
        }

        [Fact]
        public void Detect_SingleHeading_ScoresCorrectly()
        {
            var text = "# Heading\nSome text here.";
            var result = MarkdownDetector.Detect(text);
            // 1 heading = 3 points / 2 lines = 1.5
            result.Score.Should().BeGreaterThanOrEqualTo(1.0);
            result.IsMarkdown.Should().BeTrue();
        }

        [Fact]
        public void Detect_CodeFences_WeighHeavily()
        {
            var text = @"Some intro text.

```
console.log('hello');
```";

            var result = MarkdownDetector.Detect(text);
            // 2 code fences = 10 points / 4 non-empty lines = 2.5
            result.IsMarkdown.Should().BeTrue();
            result.Indicators.Should().Contain(i => i.Contains("code fences"));
        }

        [Fact]
        public void Detect_TableRows_ScoreCorrectly()
        {
            var text = @"| Name | Age |
| ---- | --- |
| Alice | 30 |
| Bob | 25 |";

            var result = MarkdownDetector.Detect(text);
            result.IsMarkdown.Should().BeTrue();
            result.Indicators.Should().Contain(i => i.Contains("table rows"));
        }

        [Fact]
        public void Detect_BoldAndLinks_Score()
        {
            var text = @"This has **bold text** and a [link](https://example.com).
And __another bold__ with [another link](https://test.com).";

            var result = MarkdownDetector.Detect(text);
            result.IsMarkdown.Should().BeTrue();
            result.Indicators.Should().Contain(i => i.Contains("bold"));
            result.Indicators.Should().Contain(i => i.Contains("links"));
        }

        [Fact]
        public void Detect_AllPlainParagraphs_BelowThreshold()
        {
            var text = @"The quick brown fox jumps over the lazy dog.
Pack my box with five dozen liquor jugs.
How vexingly quick daft zebras jump.
The five boxing wizards jump quickly.
Sphinx of black quartz judge my vow.
Two driven jocks help fax my big quiz.
The jay pig fox zebra and my wolves quack.
Crazy Frederick bought many very exquisite opal jewels.";

            var result = MarkdownDetector.Detect(text);
            result.IsMarkdown.Should().BeFalse();
        }

        [Fact]
        public void Detect_ShortMarkdownFile_StillDetected()
        {
            var text = @"# Title
- Item one";

            var result = MarkdownDetector.Detect(text);
            result.IsMarkdown.Should().BeTrue();
        }

        [Fact]
        public void Detect_Indicators_ListsMatchedPatterns()
        {
            var text = @"# Heading

> A quote

- A list item";

            var result = MarkdownDetector.Detect(text);
            result.Indicators.Should().HaveCountGreaterThanOrEqualTo(3);
            result.Indicators.Should().Contain(i => i.Contains("headings"));
            result.Indicators.Should().Contain(i => i.Contains("blockquotes"));
            result.Indicators.Should().Contain(i => i.Contains("unordered list"));
        }

        [Fact]
        public void Detect_OrderedList_Scores()
        {
            var text = @"1. First item
2. Second item
3. Third item";

            var result = MarkdownDetector.Detect(text);
            result.IsMarkdown.Should().BeTrue();
            result.Indicators.Should().Contain(i => i.Contains("ordered list"));
        }
    }
}
