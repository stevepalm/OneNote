using MDNote.Core.Models;

namespace MDNote.Core
{
    /// <summary>
    /// Converts Markdown text to styled HTML suitable for OneNote rendering.
    /// </summary>
    public interface IMarkdownConverter
    {
        /// <summary>
        /// Converts Markdown to HTML with default options.
        /// </summary>
        ConversionResult Convert(string markdown);

        /// <summary>
        /// Converts Markdown to HTML with specified options.
        /// </summary>
        ConversionResult Convert(string markdown, ConversionOptions options);
    }
}
