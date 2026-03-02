using FluentAssertions;
using MDNote.Core;
using MDNote.Core.Models;
using Xunit;

namespace MDNote.Core.Tests;

public class FullPipelineIntegrationTests
{
    private const string ComprehensiveDocument = @"---
title: Comprehensive Test
author: MD Note
date: 2026-03-01
---

# Main Heading

Paragraph with **bold**, *italic*, ~~strikethrough~~, and `inline code`.

## Second Level

### Third Level

#### Fourth Level

##### Fifth Level

###### Sixth Level

## Tables

| Left | Center | Right |
|:-----|:------:|------:|
| A1   | B1     | C1    |
| A2   | B2     | C2    |

## Code Examples

```python
def hello():
    print('Hello, World!')
    return 42
```

```javascript
const greet = (name) => {
    console.log(`Hello, ${name}!`);
};
```

```sql
SELECT * FROM users WHERE active = 1;
```

## Lists

- Unordered item 1
  - Nested item 1a
    - Deep nested item 1a-i
  - Nested item 1b
- Unordered item 2

1. First ordered
2. Second ordered
3. Third ordered

### Task List

- [x] Completed task
- [ ] Pending task
- [x] Another done task

## Blockquotes

> Simple quote

> Outer quote
> > Nested quote
> > > Triple nested

## Footnotes

Text with a footnote[^1] and another[^2].

[^1]: First footnote content.
[^2]: Second footnote content.

## Definition Lists

Markdown
:   A lightweight markup language

OneNote
:   A digital note-taking application

## Special Elements

---

==Highlighted text==

Text with a link [Example](https://example.com).

![Alt text](https://example.com/image.png)

## Math

Inline math: $E = mc^2$

$$
\int_0^\infty e^{-x} dx = 1
$$

## Mermaid

```mermaid
graph TD
    A --> B
    B --> C
```

That's all!
";

    [Fact]
    public void FullPipeline_ComprehensiveDocument_ProducesValidOutput()
    {
        var converter = new MarkdownConverter();
        var options = new ConversionOptions
        {
            EnableSyntaxHighlighting = true,
            Theme = "dark",
            InlineAllStyles = true
        };
        var result = converter.Convert(ComprehensiveDocument, options);

        var oneNoteConverter = new HtmlToOneNoteConverter();
        var oneNoteHtml = oneNoteConverter.ConvertForOneNote(result.Html);

        // Structural: no raw Markdig elements should survive
        oneNoteHtml.Should().NotBeNullOrEmpty();
        oneNoteHtml.Should().NotContain("<blockquote>");
        oneNoteHtml.Should().NotContain("<h1");
        oneNoteHtml.Should().NotContain("<h2");
        oneNoteHtml.Should().NotContain("<hr");
        oneNoteHtml.Should().NotContain("<thead>");
        oneNoteHtml.Should().NotContain("<tbody>");
        oneNoteHtml.Should().NotContain("<th");
        oneNoteHtml.Should().NotContain("<input");
        oneNoteHtml.Should().NotContain("<mark>");
        oneNoteHtml.Should().NotContain("<dl>");
        oneNoteHtml.Should().NotContain("<dt>");
        oneNoteHtml.Should().NotContain("<dd>");
        oneNoteHtml.Should().NotContain("<pre>");
        oneNoteHtml.Should().NotContain("<code>");

        // Headings converted to styled paragraphs with Calibri font
        oneNoteHtml.Should().Contain("font-family:Calibri");
        oneNoteHtml.Should().Contain("font-size:20pt");
        oneNoteHtml.Should().Contain("font-size:18pt");
        oneNoteHtml.Should().Contain("font-size:16pt");

        // Code blocks as tables
        oneNoteHtml.Should().Contain("Python");
        oneNoteHtml.Should().Contain("JavaScript");
        oneNoteHtml.Should().Contain("SQL");
        oneNoteHtml.Should().Contain("Consolas");

        // Tables with borders and header styling
        oneNoteHtml.Should().Contain("border-collapse:collapse");
        oneNoteHtml.Should().Contain("font-weight:bold");
        oneNoteHtml.Should().Contain("border-bottom:2px solid #999");

        // Checkboxes with spacing
        oneNoteHtml.Should().Contain("\u2611\u00A0");
        oneNoteHtml.Should().Contain("\u2610\u00A0");

        // Blockquotes with colored borders
        oneNoteHtml.Should().Contain("border-left:3px solid");

        // Definition lists converted
        oneNoteHtml.Should().Contain("Markdown");
        oneNoteHtml.Should().Contain("lightweight markup language");

        // Highlight
        oneNoteHtml.Should().Contain("background-color:#ffff00");

        // Lists converted to styled paragraphs with list markers for native OneNote lists
        oneNoteHtml.Should().Contain("list-bullet:");
        oneNoteHtml.Should().NotContain("<ul>");
        oneNoteHtml.Should().NotContain("<ol>");
        oneNoteHtml.Should().NotContain("<li>");

        // Table headers have background color
        oneNoteHtml.Should().Contain("background-color:#DEEBF6");

        // Base font applied to normal text
        oneNoteHtml.Should().Contain("font-family:Calibri;font-size:11pt");

        // Links and images preserved
        oneNoteHtml.Should().Contain("<a href=");
        oneNoteHtml.Should().Contain("<img");

        // Mermaid placeholder
        oneNoteHtml.Should().Contain("Mermaid diagram");
    }

    [Fact]
    public void FullPipeline_ComprehensiveDocument_ExtractsTitleFromFrontMatter()
    {
        var converter = new MarkdownConverter();
        var result = converter.Convert(ComprehensiveDocument);
        result.Title.Should().Be("Comprehensive Test");
    }

    [Fact]
    public void FullPipeline_ComprehensiveDocument_CollectsAllHeadings()
    {
        var converter = new MarkdownConverter();
        var result = converter.Convert(ComprehensiveDocument);
        result.Headings.Should().HaveCountGreaterThanOrEqualTo(10);
    }

    [Fact]
    public void FullPipeline_ComprehensiveDocument_ExtractsMermaid()
    {
        var converter = new MarkdownConverter();
        var result = converter.Convert(ComprehensiveDocument);
        result.MermaidBlocks.Should().HaveCount(1);
        result.MermaidBlocks[0].Definition.Should().Contain("graph TD");
    }

    [Fact]
    public void FullPipeline_ComprehensiveDocument_HasTimings()
    {
        var converter = new MarkdownConverter();
        var result = converter.Convert(ComprehensiveDocument);
        result.PipelineTimings.Should().NotBeEmpty();
        result.PipelineTimings.Should().ContainKey("MarkdigParse");
        result.PipelineTimings.Should().ContainKey("SyntaxHighlight");
    }

    [Fact]
    public void FullPipeline_LargeDocument_PerformanceAcceptable()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("# Large Document Test");
        for (int i = 0; i < 200; i++)
        {
            sb.AppendLine($"\n## Section {i}");
            sb.AppendLine($"Paragraph with **bold** and *italic* text.");
            sb.AppendLine($"\n```csharp\npublic void Method{i}() {{ }}\n```");
            sb.AppendLine($"\n| Col1 | Col2 |\n|------|------|\n| A{i} | B{i} |");
            sb.AppendLine($"\n> A quote in section {i}");
        }

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var converter = new MarkdownConverter();
        var result = converter.Convert(sb.ToString());
        var oneNoteConverter = new HtmlToOneNoteConverter();
        var oneNoteHtml = oneNoteConverter.ConvertForOneNote(result.Html);
        sw.Stop();

        oneNoteHtml.Should().NotBeNullOrEmpty();
        sw.ElapsedMilliseconds.Should().BeLessThan(10000);
    }

    [Fact]
    public void FullPipeline_UnicodeEmoji_EndToEnd()
    {
        var md = "# \u4f60\u597d\n\nHello :smile: world\n\nCaf\u00e9 \u00e8 buono";
        var converter = new MarkdownConverter();
        var result = converter.Convert(md);
        var oneNoteConverter = new HtmlToOneNoteConverter();
        var oneNoteHtml = oneNoteConverter.ConvertForOneNote(result.Html);

        oneNoteHtml.Should().Contain("\u4f60\u597d");
        oneNoteHtml.Should().Contain("Caf\u00e9");
    }

    [Fact]
    public void FullPipeline_Footnotes_EndToEnd()
    {
        var md = "Text with a footnote[^1].\n\n[^1]: Here is the footnote.";
        var converter = new MarkdownConverter();
        var result = converter.Convert(md);
        var oneNoteConverter = new HtmlToOneNoteConverter();
        var oneNoteHtml = oneNoteConverter.ConvertForOneNote(result.Html);

        oneNoteHtml.Should().Contain("[1]");
        oneNoteHtml.Should().Contain("footnote");
    }

    [Fact]
    public void FullPipeline_DefinitionList_EndToEnd()
    {
        var md = "Term\n:   Definition of the term";
        var converter = new MarkdownConverter();
        var result = converter.Convert(md);
        var oneNoteConverter = new HtmlToOneNoteConverter();
        var oneNoteHtml = oneNoteConverter.ConvertForOneNote(result.Html);

        oneNoteHtml.Should().Contain("Term");
        oneNoteHtml.Should().Contain("Definition of the term");
        oneNoteHtml.Should().NotContain("<dl>");
    }

    [Fact]
    public void FullPipeline_MermaidSkip_NoMermaidInput()
    {
        var md = "# Simple\n\nNo mermaid here.";
        var converter = new MarkdownConverter();
        var result = converter.Convert(md);
        result.MermaidBlocks.Should().BeEmpty();
        result.PipelineTimings.Should().ContainKey("ExtractMermaid");
    }

    [Fact]
    public void FullPipeline_RecognizedLanguageCodeBlocks_NoPreSurvives()
    {
        // Languages recognized by ColorCode produce <pre> without <code>
        var md = @"# Code Test

```csharp
public class Foo { }
```

```javascript
const x = 42;
```

```python
def hello(): pass
```

Some text after code.
";
        var converter = new MarkdownConverter();
        var options = new ConversionOptions
        {
            EnableSyntaxHighlighting = true,
            Theme = "dark",
            InlineAllStyles = true
        };
        var result = converter.Convert(md, options);

        var oneNoteConverter = new HtmlToOneNoteConverter();
        var oneNoteHtml = oneNoteConverter.ConvertForOneNote(result.Html);

        oneNoteHtml.Should().NotContain("<pre>");
        oneNoteHtml.Should().NotContain("</pre>");
        oneNoteHtml.Should().NotContain("<code>");
        oneNoteHtml.Should().Contain("<table");
        oneNoteHtml.Should().Contain("C#");
        oneNoteHtml.Should().Contain("JavaScript");
        oneNoteHtml.Should().Contain("Python");
    }

    [Fact]
    public void FullPipeline_NoUnsupportedTagsSurvive()
    {
        var converter = new MarkdownConverter();
        var options = new ConversionOptions
        {
            EnableSyntaxHighlighting = true,
            Theme = "dark",
            InlineAllStyles = true
        };
        var result = converter.Convert(ComprehensiveDocument, options);

        var oneNoteConverter = new HtmlToOneNoteConverter();
        var oneNoteHtml = oneNoteConverter.ConvertForOneNote(result.Html);

        // Verify no unsupported tags remain using a regex check
        var tagRegex = new System.Text.RegularExpressions.Regex(@"</?([a-zA-Z][a-zA-Z0-9]*)\b");
        var allowed = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase)
        {
            "p", "br", "span", "div", "a", "ul", "ol", "li",
            "table", "tr", "td",
            "h1", "h2", "h3", "h4", "h5", "h6",
            "b", "em", "strong", "i", "u", "del", "sup", "sub", "cite", "img"
        };

        foreach (System.Text.RegularExpressions.Match m in tagRegex.Matches(oneNoteHtml))
        {
            var tagName = m.Groups[1].Value;
            allowed.Should().Contain(tagName,
                $"tag <{tagName}> is not in OneNote's supported CDATA set");
        }
    }

    [Fact]
    public void FullPipeline_LargeDocumentWithCodeBlocks_PerformanceAcceptable()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("# Large Document with Code Blocks");
        for (int i = 0; i < 100; i++)
        {
            sb.AppendLine($"\n## Section {i}");
            sb.AppendLine($"Paragraph with **bold**, *italic*, and `inline code`.");
            sb.AppendLine($"\n```csharp\npublic class Section{i} {{ public int Value {{ get; set; }} }}\n```");
            sb.AppendLine($"\n```javascript\nconst section{i} = () => console.log({i});\n```");
            sb.AppendLine($"\n| Col1 | Col2 |\n|------|------|\n| A{i} | B{i} |");
            sb.AppendLine($"\n> A quote in section {i}");
        }

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var converter = new MarkdownConverter();
        var options = new ConversionOptions
        {
            EnableSyntaxHighlighting = true,
            Theme = "dark",
            InlineAllStyles = true
        };
        var result = converter.Convert(sb.ToString(), options);
        var oneNoteConverter = new HtmlToOneNoteConverter();
        var oneNoteHtml = oneNoteConverter.ConvertForOneNote(result.Html);
        sw.Stop();

        oneNoteHtml.Should().NotBeNullOrEmpty();
        oneNoteHtml.Should().NotContain("<pre>");
        oneNoteHtml.Should().NotContain("<code>");
        oneNoteHtml.Should().NotContain("<blockquote>");
        sw.ElapsedMilliseconds.Should().BeLessThan(15000);
    }
}
