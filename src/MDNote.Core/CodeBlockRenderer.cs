using System.Net;

namespace MDNote.Core
{
    /// <summary>
    /// Renders code blocks as single-cell OneNote tables with monospace font and background color.
    /// Preserves inline syntax highlighting color spans from ColorCode.
    /// </summary>
    public class CodeBlockRenderer
    {
        private const string DarkBackground = "#1e1e1e";
        private const string LightBackground = "#f6f8fa";
        private const string DarkText = "#dadada";
        private const string LightText = "#24292e";
        private const string LabelBackground = "#2d2d2d";
        private const string LabelColor = "#858585";
        private const string FontFamily = "Consolas,'Courier New',monospace";
        private const string FontSize = "10pt";
        private const string LabelFontSize = "9pt";
        private const string BorderColor = "#444";

        private readonly string _background;
        private readonly string _textColor;

        public CodeBlockRenderer(string theme = "dark")
        {
            _background = theme == "light" ? LightBackground : DarkBackground;
            _textColor = theme == "light" ? LightText : DarkText;
        }

        /// <summary>
        /// Renders code content as a single-cell table suitable for OneNote.
        /// The codeHtml may contain inline &lt;span style="color:..."&gt; elements from ColorCode.
        /// </summary>
        /// <param name="codeHtml">Pre-formatted code HTML (may include ColorCode spans).</param>
        /// <param name="language">Optional language identifier for the label row.</param>
        public string RenderCodeBlockAsTable(string codeHtml, string language)
        {
            if (string.IsNullOrEmpty(codeHtml))
                return string.Empty;

            var labelRow = "";
            if (!string.IsNullOrEmpty(language))
            {
                var displayName = SyntaxHighlighter.GetDisplayName(language);
                labelRow =
                    $"<tr><td style=\"background:{LabelBackground};color:{LabelColor};" +
                    $"padding:4px 12px;font-family:{FontFamily};font-size:{LabelFontSize};" +
                    $"border:1px solid {BorderColor};\">" +
                    $"{WebUtility.HtmlEncode(displayName)}</td></tr>";
            }

            var codeRow =
                $"<tr><td style=\"background:{_background};color:{_textColor};" +
                $"padding:12px;font-family:{FontFamily};font-size:{FontSize};" +
                $"border:1px solid {BorderColor};white-space:pre;\">" +
                $"{codeHtml}</td></tr>";

            return
                $"<table style=\"border-collapse:collapse;width:100%;margin:8px 0;\">" +
                labelRow + codeRow + "</table>";
        }
    }
}
