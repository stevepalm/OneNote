using FluentAssertions;
using MDNote.Core;
using MDNote.Core.Models;
using System.Text.RegularExpressions;
using Xunit;

namespace MDNote.Core.Tests;

public class ComplexDocumentTests
{
    private readonly IMarkdownConverter _converter = new MarkdownConverter();

    private const string FullDocument = @"---
title: Complete Test Document
author: Test Author
tags: markdown, test, comprehensive
---

# Main Title

This is an introductory paragraph with **bold**, *italic*, and ~~strikethrough~~ text.
Here's some `inline code` too.

## Table of Data

| Name | Role | Score |
|:-----|:----:|------:|
| Alice | Dev | 95 |
| Bob | QA | 88 |

## Code Examples

```csharp
public class Hello
{
    public void World() => Console.WriteLine(""Hello!"");
}
```

```python
def hello():
    print('Hello, World!')
```

```bash
echo ""Hello World""
```

## Lists

### Unordered
- Item A
  - Sub-item A1
    - Sub-sub-item A1a
  - Sub-item A2
- Item B

### Ordered
1. First step
2. Second step
3. Third step

### Task List
- [x] Completed task
- [ ] Pending task

## Blockquotes

> This is a quote.
>
> > This is a nested quote.

## Links and Images

Visit [Example](https://example.com) for more info.

![Sample Image](https://example.com/img.png ""Sample"")

---

## Footnotes

This text has a footnote[^1].

[^1]: Here is the footnote content.

*[HTML]: Hyper Text Markup Language

The HTML spec is interesting.

Term One
:   Definition of term one.

That's all folks!
";

    [Fact]
    public void Convert_ComplexDocument_ProducesValidHtml()
    {
        var result = _converter.Convert(FullDocument);

        result.Html.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Convert_ComplexDocument_ExtractsTitle()
    {
        var result = _converter.Convert(FullDocument);

        // Front-matter title takes priority
        result.Title.Should().Be("Complete Test Document");
    }

    [Fact]
    public void Convert_ComplexDocument_ExtractsFrontMatter()
    {
        var result = _converter.Convert(FullDocument);

        result.FrontMatter.Should().ContainKey("title");
        result.FrontMatter.Should().ContainKey("author");
        result.FrontMatter.Should().ContainKey("tags");
    }

    [Fact]
    public void Convert_ComplexDocument_CollectsHeadings()
    {
        var result = _converter.Convert(FullDocument);

        result.Headings.Should().NotBeEmpty();
        result.Headings[0].Text.Should().Be("Main Title");
        result.Headings[0].Level.Should().Be(1);
    }

    [Fact]
    public void Convert_ComplexDocument_ContainsNoStyleTags()
    {
        var result = _converter.Convert(FullDocument);

        result.Html.Should().NotContain("<style");
        result.Html.Should().NotContain("</style>");
    }

    [Fact]
    public void Convert_ComplexDocument_ContainsAllElements()
    {
        var result = _converter.Convert(FullDocument);

        // Headings
        result.Html.Should().Contain("<h1");
        result.Html.Should().Contain("<h2");
        result.Html.Should().Contain("<h3");

        // Inline formatting
        result.Html.Should().Contain("<strong>");
        result.Html.Should().Contain("<em>");
        result.Html.Should().Contain("<del>");

        // Table
        result.Html.Should().Contain("<table");
        result.Html.Should().Contain("<th");
        result.Html.Should().Contain("<td");

        // Lists
        result.Html.Should().Contain("<ul>");
        result.Html.Should().Contain("<ol>");

        // Blockquote
        result.Html.Should().Contain("<blockquote>");

        // Links and images
        result.Html.Should().Contain("<a href=");
        result.Html.Should().Contain("<img");

        // Horizontal rule
        result.Html.Should().Contain("<hr");

        // Code blocks with syntax highlighting
        result.Html.Should().Contain("C#");
        result.Html.Should().Contain("Python");
    }

    [Fact]
    public void Convert_ComplexDocument_CodeBlocksHaveInlineStyles()
    {
        var result = _converter.Convert(FullDocument);

        // Code blocks should have inline style attributes (not CSS classes)
        result.Html.Should().Contain("style=\"");
        result.Html.Should().Contain("#1E1E1E");
    }

    [Fact]
    public void Convert_ComplexDocument_HasCheckboxes()
    {
        var result = _converter.Convert(FullDocument);

        result.Html.Should().Contain("type=\"checkbox\"");
    }

    [Fact]
    public void Convert_ComplexDocument_HasFootnotes()
    {
        var result = _converter.Convert(FullDocument);

        result.Html.Should().Contain("footnote");
    }

    [Fact]
    public void Convert_ComplexDocument_HasDefinitionList()
    {
        var result = _converter.Convert(FullDocument);

        result.Html.Should().Contain("<dl>");
        result.Html.Should().Contain("<dt>");
    }

    [Fact]
    public void Convert_ComplexDocument_HasAbbreviation()
    {
        var result = _converter.Convert(FullDocument);

        result.Html.Should().Contain("<abbr");
    }
}
