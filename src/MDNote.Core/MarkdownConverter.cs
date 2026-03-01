using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Markdig;
using Markdig.Extensions.Yaml;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using MDNote.Core.Models;

namespace MDNote.Core
{
    public class MarkdownConverter : IMarkdownConverter
    {
        private readonly MarkdownPipeline _pipeline;

        private static readonly Regex MermaidFenceRegex = new Regex(
            @"^(`{3,})\s*mermaid\s*\n([\s\S]*?)^\1\s*$",
            RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);

        private static readonly Regex TocMarkerRegex = new Regex(
            @"(\[TOC\]|<!--\s*toc\s*-->)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public MarkdownConverter()
        {
            _pipeline = new MarkdownPipelineBuilder()
                .UseAdvancedExtensions()
                .UseYamlFrontMatter()
                .UseEmojiAndSmiley()
                .UseSmartyPants()
                .Build();
        }

        public ConversionResult Convert(string markdown)
        {
            return Convert(markdown, new ConversionOptions());
        }

        public ConversionResult Convert(string markdown, ConversionOptions options)
        {
            if (options == null)
                options = new ConversionOptions();

            var result = new ConversionResult();

            if (string.IsNullOrEmpty(markdown))
            {
                result.Html = string.Empty;
                return result;
            }

            var sw = Stopwatch.StartNew();

            // 1. Extract mermaid blocks before Markdig processing (fast-path skip)
            string processed;
            if (markdown.IndexOf("mermaid", StringComparison.OrdinalIgnoreCase) >= 0)
                processed = ExtractMermaidBlocks(markdown, result.MermaidBlocks);
            else
                processed = markdown;
            result.PipelineTimings["ExtractMermaid"] = sw.ElapsedMilliseconds;
            sw.Restart();

            // 2. Detect and remove TOC markers
            bool hasTocMarker = TocMarkerRegex.IsMatch(processed);
            processed = TocMarkerRegex.Replace(processed, "");

            // 3. Parse with Markdig
            var document = Markdown.Parse(processed, _pipeline);
            result.PipelineTimings["MarkdigParse"] = sw.ElapsedMilliseconds;
            sw.Restart();

            // 4. Extract front-matter
            var yamlBlock = document.Descendants<YamlFrontMatterBlock>().FirstOrDefault();
            if (yamlBlock != null)
            {
                result.FrontMatter = ParseFrontMatter(yamlBlock);
            }

            // 5. Collect headings
            result.Headings = CollectHeadings(document);

            // 6. Extract title: front-matter "title" → first H1 → null
            if (result.FrontMatter.TryGetValue("title", out var fmTitle))
            {
                result.Title = fmTitle;
            }
            else
            {
                var firstH1 = result.Headings.FirstOrDefault(h => h.Level == 1);
                result.Title = firstH1?.Text;
            }

            // 7. Render AST to HTML
            var html = RenderToHtml(document);
            result.PipelineTimings["RenderHtml"] = sw.ElapsedMilliseconds;
            sw.Restart();

            // 8. Post-process: syntax highlighting
            if (options.EnableSyntaxHighlighting)
            {
                var highlighter = new SyntaxHighlighter(options.Theme);
                html = highlighter.HighlightCodeBlocks(html);
            }
            result.PipelineTimings["SyntaxHighlight"] = sw.ElapsedMilliseconds;
            sw.Restart();

            // 9. Post-process: math rendering
            var mathRenderer = new MathRenderer();
            html = mathRenderer.ProcessMathBlocks(html);

            // 10. Post-process: TOC insertion
            if (options.EnableTableOfContents || hasTocMarker)
            {
                var tocGenerator = new TableOfContentsGenerator();
                var toc = tocGenerator.GenerateToc(result.Headings);
                if (!string.IsNullOrEmpty(toc))
                {
                    // Insert at beginning of HTML
                    html = toc + html;
                }
            }
            result.PipelineTimings["MathAndToc"] = sw.ElapsedMilliseconds;
            sw.Restart();

            // 11. Replace mermaid placeholders with styled divs
            html = ReplaceMermaidPlaceholders(html, result.MermaidBlocks);

            // 12. Safety: strip any <style> tags if InlineAllStyles is true
            if (options.InlineAllStyles)
            {
                html = Regex.Replace(html, @"<style[\s\S]*?</style>", "", RegexOptions.IgnoreCase);
            }
            result.PipelineTimings["PostProcess"] = sw.ElapsedMilliseconds;

            result.Html = html;
            return result;
        }

        private string ExtractMermaidBlocks(string markdown, List<MermaidBlock> mermaidBlocks)
        {
            int index = 0;
            return MermaidFenceRegex.Replace(markdown, match =>
            {
                var block = new MermaidBlock
                {
                    Id = $"mermaid-{index}",
                    Definition = match.Groups[2].Value.Trim()
                };
                mermaidBlocks.Add(block);
                var placeholder = $"<!-- MDNOTE_MERMAID_{index} -->";
                index++;
                return placeholder;
            });
        }

        private static string ReplaceMermaidPlaceholders(string html, List<MermaidBlock> mermaidBlocks)
        {
            for (int i = 0; i < mermaidBlocks.Count; i++)
            {
                var placeholder = $"<!-- MDNOTE_MERMAID_{i} -->";
                var replacement =
                    $"<div style=\"border:1px dashed #666;padding:16px;text-align:center;" +
                    $"color:#888;margin:8px 0;border-radius:4px;\">" +
                    $"[Mermaid diagram: {mermaidBlocks[i].Id}]</div>";
                html = html.Replace(placeholder, replacement);
            }
            return html;
        }

        private static Dictionary<string, string> ParseFrontMatter(YamlFrontMatterBlock yamlBlock)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var lines = yamlBlock.Lines.ToString();

            foreach (var rawLine in lines.Split('\n'))
            {
                var line = rawLine.Trim();
                if (string.IsNullOrEmpty(line) || line == "---")
                    continue;

                var colonIndex = line.IndexOf(':');
                if (colonIndex <= 0)
                    continue;

                var key = line.Substring(0, colonIndex).Trim();
                var value = line.Substring(colonIndex + 1).Trim();
                result[key] = value;
            }

            return result;
        }

        private static List<HeadingInfo> CollectHeadings(MarkdownDocument document)
        {
            var headings = new List<HeadingInfo>();

            foreach (var block in document.Descendants<HeadingBlock>())
            {
                var text = ExtractInlineText(block.Inline);
                var id = block.GetAttributes()?.Id;

                headings.Add(new HeadingInfo
                {
                    Level = block.Level,
                    Text = text,
                    Id = id
                });
            }

            return headings;
        }

        private static string ExtractInlineText(ContainerInline container)
        {
            if (container == null)
                return string.Empty;

            var sb = new StringBuilder();
            foreach (var inline in container)
            {
                if (inline is LiteralInline literal)
                {
                    sb.Append(literal.Content);
                }
                else if (inline is ContainerInline nested)
                {
                    sb.Append(ExtractInlineText(nested));
                }
                else if (inline is CodeInline code)
                {
                    sb.Append(code.Content);
                }
            }
            return sb.ToString();
        }

        private string RenderToHtml(MarkdownDocument document)
        {
            using (var writer = new StringWriter())
            {
                var renderer = new HtmlRenderer(writer);
                _pipeline.Setup(renderer);
                renderer.Render(document);
                writer.Flush();
                return writer.ToString();
            }
        }
    }
}
