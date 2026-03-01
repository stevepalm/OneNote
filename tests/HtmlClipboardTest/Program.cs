using System;
using System.Text;
using System.Windows.Forms;
using MDNote.Core;
using MDNote.Core.Models;

namespace HtmlClipboardTest
{

class Program
{
    [STAThread]
    static void Main()
    {
        var converter = new MarkdownConverter();

        var markdown = @"---
title: OneNote HTML Rendering Test
author: MD Note
---

# Heading 1

## Heading 2

### Heading 3

#### Heading 4

This is a paragraph with **bold**, *italic*, ~~strikethrough~~, and `inline code`.

---

## Links and Images

[Example Link](https://example.com)

![Alt text](https://via.placeholder.com/150 ""Placeholder"")

## Unordered List

- Item A
  - Sub A1
    - Sub A1a
  - Sub A2
- Item B
- Item C

## Ordered List

1. First
2. Second
3. Third

## Task List

- [x] Completed task
- [ ] Pending task

## Table

| Name | Role | Score |
|:-----|:----:|------:|
| Alice | Developer | 95 |
| Bob | Designer | 88 |
| Carol | Manager | 92 |

## Blockquote

> This is a blockquote.
>
> > Nested blockquote inside.

## Code Blocks

### With Syntax Highlighting (C#)

```csharp
public class Calculator
{
    public int Add(int a, int b) => a + b;
    public string Greet(string name) => $""Hello, {name}!"";
}
```

### Unsupported Language (Bash)

```bash
echo ""Hello World""
ls -la /tmp
```

### No Language Specified

```
Just plain monospace text
No syntax highlighting here
```

## Math

Inline math: $E = mc^2$

Block math:

$$
x = \frac{-b \pm \sqrt{b^2 - 4ac}}{2a}
$$

## Footnotes

This has a footnote[^1].

[^1]: Footnote content here.

## Definition List

Markdown
:   A lightweight markup language

OneNote
:   A digital note-taking application

## Abbreviation

*[HTML]: Hyper Text Markup Language
*[CSS]: Cascading Style Sheets

HTML and CSS are web technologies.

## Emoji

Hello :wave: and :smile:!

---

*End of test document.*
";

        var result = converter.Convert(markdown);

        Console.WriteLine("=== CONVERSION RESULT ===");
        Console.WriteLine($"Title: {result.Title}");
        Console.WriteLine($"Headings: {result.Headings.Count}");
        Console.WriteLine($"Mermaid blocks: {result.MermaidBlocks.Count}");
        Console.WriteLine($"Front matter keys: {string.Join(", ", result.FrontMatter.Keys)}");
        Console.WriteLine($"HTML length: {result.Html.Length} chars");
        Console.WriteLine();

        // Wrap in a basic HTML document for clipboard
        var fullHtml = $@"<html>
<head><meta charset=""utf-8""></head>
<body>
{result.Html}
</body>
</html>";

        // Copy to clipboard in HTML format
        var dataObject = new DataObject();
        dataObject.SetData(DataFormats.Html, BuildCfHtml(fullHtml));
        dataObject.SetData(DataFormats.Text, result.Html);
        Clipboard.SetDataObject(dataObject, true);

        Console.WriteLine("HTML copied to clipboard! Paste into OneNote to test rendering.");
        Console.WriteLine();
        Console.WriteLine("Press any key to also save raw HTML to file...");
        Console.ReadKey();

        var outputPath = System.IO.Path.Combine(
            System.IO.Path.GetDirectoryName(typeof(Program).Assembly.Location)!,
            "test-output.html");
        System.IO.File.WriteAllText(outputPath, fullHtml);
        Console.WriteLine($"Saved to: {outputPath}");
    }

    /// <summary>
    /// Builds the CF_HTML clipboard format with proper headers.
    /// </summary>
    static string BuildCfHtml(string html)
    {
        // CF_HTML format requires byte-offset headers
        const string header = "Version:0.9\r\n" +
                              "StartHTML:{0:D10}\r\n" +
                              "EndHTML:{1:D10}\r\n" +
                              "StartFragment:{2:D10}\r\n" +
                              "EndFragment:{3:D10}\r\n";

        var startFragMarker = "<!--StartFragment-->";
        var endFragMarker = "<!--EndFragment-->";

        var htmlWithMarkers = html.Replace("<body>", $"<body>\r\n{startFragMarker}")
                                  .Replace("</body>", $"{endFragMarker}\r\n</body>");

        // Calculate placeholder header length (with 10-digit numbers)
        var headerLen = string.Format(header, 0, 0, 0, 0).Length;
        var startHtml = headerLen;
        var bytes = Encoding.UTF8.GetBytes(htmlWithMarkers);
        var endHtml = headerLen + bytes.Length;

        var fragStartStr = startFragMarker;
        var fragStartIndex = htmlWithMarkers.IndexOf(fragStartStr);
        var startFragment = headerLen + Encoding.UTF8.GetByteCount(
            htmlWithMarkers.Substring(0, fragStartIndex)) +
            Encoding.UTF8.GetByteCount(fragStartStr);

        var fragEndIndex = htmlWithMarkers.IndexOf(endFragMarker);
        var endFragment = headerLen + Encoding.UTF8.GetByteCount(
            htmlWithMarkers.Substring(0, fragEndIndex));

        return string.Format(header, startHtml, endHtml, startFragment, endFragment) + htmlWithMarkers;
    }
}
}
