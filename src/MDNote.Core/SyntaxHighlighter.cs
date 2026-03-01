using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;
using ColorCode;
using ColorCode.Styling;

namespace MDNote.Core
{
    internal class SyntaxHighlighter
    {
        private static readonly Regex CodeBlockRegex = new Regex(
            @"<pre><code\s+class=""language-([^""]+)"">([\s\S]*?)</code></pre>",
            RegexOptions.Compiled);

        private static readonly Regex PlainCodeBlockRegex = new Regex(
            @"<pre><code>([\s\S]*?)</code></pre>",
            RegexOptions.Compiled);

        private static readonly Dictionary<string, ILanguage> LanguageMap = BuildLanguageMap();

        private readonly StyleDictionary _styles;

        public SyntaxHighlighter(string theme = "dark")
        {
            _styles = theme == "dark" ? StyleDictionary.DefaultDark : StyleDictionary.DefaultLight;
        }

        public string HighlightCodeBlocks(string html)
        {
            if (string.IsNullOrEmpty(html))
                return html;

            // Process blocks with a language specified
            html = CodeBlockRegex.Replace(html, match =>
            {
                var language = match.Groups[1].Value.Trim().ToLowerInvariant();
                var encodedCode = match.Groups[2].Value;
                var rawCode = WebUtility.HtmlDecode(encodedCode);

                return FormatCodeBlock(rawCode, language);
            });

            // Process blocks without a language specified
            html = PlainCodeBlockRegex.Replace(html, match =>
            {
                var encodedCode = match.Groups[1].Value;
                var rawCode = WebUtility.HtmlDecode(encodedCode);

                return FormatFallbackCodeBlock(rawCode, null);
            });

            return html;
        }

        private string FormatCodeBlock(string code, string language)
        {
            if (LanguageMap.TryGetValue(language, out var lang))
            {
                return FormatHighlightedCodeBlock(code, lang, language);
            }

            return FormatFallbackCodeBlock(code, language);
        }

        private string FormatHighlightedCodeBlock(string code, ILanguage language, string languageAlias)
        {
            var formatter = new HtmlFormatter(_styles);
            var highlighted = formatter.GetHtmlString(code, language);

            // ColorCode wraps in <div style="..."><pre>...</pre></div>
            // We prepend a language label header and wrap everything together
            var label = GetDisplayName(languageAlias);

            return
                $"<div style=\"margin:8px 0;\">" +
                $"<div style=\"background:#2d2d2d;color:#858585;padding:4px 12px;font-size:11px;" +
                $"font-family:Consolas,'Courier New',monospace;border-radius:4px 4px 0 0;\">{WebUtility.HtmlEncode(label)}</div>" +
                highlighted +
                "</div>";
        }

        private string FormatFallbackCodeBlock(string code, string language)
        {
            var encodedCode = WebUtility.HtmlEncode(code);
            var labelHtml = "";

            if (!string.IsNullOrEmpty(language))
            {
                var label = GetDisplayName(language);
                labelHtml =
                    $"<div style=\"background:#2d2d2d;color:#858585;padding:4px 12px;font-size:11px;" +
                    $"font-family:Consolas,'Courier New',monospace;border-radius:4px 4px 0 0;\">{WebUtility.HtmlEncode(label)}</div>";
            }

            return
                $"<div style=\"margin:8px 0;\">" +
                labelHtml +
                $"<div style=\"color:#DADADA;background-color:#1E1E1E;\">" +
                $"<pre style=\"margin:0;padding:12px;overflow-x:auto;\">" +
                $"<code style=\"font-family:Consolas,'Courier New',monospace;font-size:13px;\">{encodedCode}</code>" +
                "</pre></div></div>";
        }

        internal static string GetDisplayName(string alias)
        {
            switch (alias)
            {
                case "cs": case "csharp": case "c#": return "C#";
                case "js": case "javascript": return "JavaScript";
                case "ts": case "typescript": return "TypeScript";
                case "py": case "python": return "Python";
                case "java": return "Java";
                case "sql": return "SQL";
                case "json": return "JSON";
                case "xml": return "XML";
                case "html": return "HTML";
                case "css": return "CSS";
                case "bash": case "sh": case "shell": return "Bash";
                case "powershell": case "ps1": return "PowerShell";
                case "rust": case "rs": return "Rust";
                case "go": case "golang": return "Go";
                case "yaml": case "yml": return "YAML";
                case "ruby": case "rb": return "Ruby";
                case "php": return "PHP";
                case "swift": return "Swift";
                case "kotlin": case "kt": return "Kotlin";
                case "cpp": case "c++": return "C++";
                case "c": return "C";
                case "dockerfile": return "Dockerfile";
                case "graphql": return "GraphQL";
                case "toml": return "TOML";
                case "diff": return "Diff";
                case "fsharp": case "f#": return "F#";
                case "haskell": return "Haskell";
                case "markdown": case "md": return "Markdown";
                case "plaintext": case "text": return "Plain Text";
                default: return alias;
            }
        }

        private static Dictionary<string, ILanguage> BuildLanguageMap()
        {
            return new Dictionary<string, ILanguage>
            {
                // C#
                { "csharp", Languages.CSharp },
                { "cs", Languages.CSharp },
                { "c#", Languages.CSharp },

                // JavaScript
                { "javascript", Languages.JavaScript },
                { "js", Languages.JavaScript },

                // TypeScript
                { "typescript", Languages.Typescript },
                { "ts", Languages.Typescript },

                // Python
                { "python", Languages.Python },
                { "py", Languages.Python },

                // Java
                { "java", Languages.Java },

                // SQL
                { "sql", Languages.Sql },

                // XML
                { "xml", Languages.Xml },

                // HTML
                { "html", Languages.Html },

                // CSS
                { "css", Languages.Css },

                // PowerShell
                { "powershell", Languages.PowerShell },
                { "ps1", Languages.PowerShell },

                // C++
                { "cpp", Languages.Cpp },
                { "c++", Languages.Cpp },

                // C (mapped to C++)
                { "c", Languages.Cpp },

                // PHP
                { "php", Languages.Php },

                // F#
                { "fsharp", Languages.FSharp },
                { "f#", Languages.FSharp },

                // Haskell
                { "haskell", Languages.Haskell },

                // Markdown
                { "markdown", Languages.Markdown },
                { "md", Languages.Markdown },
            };
        }
    }
}
