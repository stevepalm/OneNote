using MDNote.Core.Models;

namespace MDNote.Core
{
    public enum PasteMode
    {
        Off = 0,
        Prompt = 1,
        Auto = 2
    }

    /// <summary>
    /// POCO model for all MD Note settings. Grouped into Rendering, Behavior, and Export sections.
    /// Default values match the initial out-of-box experience.
    /// </summary>
    public class Settings
    {
        // --- Rendering ---

        public string Theme { get; set; } = "dark";
        public bool EnableSyntaxHighlighting { get; set; } = true;
        public bool EnableTableOfContents { get; set; } = false;
        public string FontFamily { get; set; } = "Calibri";
        public int FontSize { get; set; } = 11;

        // --- Behavior ---

        public PasteMode PasteMode { get; set; } = PasteMode.Prompt;
        public bool LiveModeEnabled { get; set; } = false;
        public int LiveModeDelayMs { get; set; } = 1500;

        // --- Export ---

        public string DefaultExportPath { get; set; } = "";
        public bool IncludeImages { get; set; } = true;

        /// <summary>
        /// Creates a ConversionOptions instance reflecting current rendering settings.
        /// </summary>
        public ConversionOptions ToConversionOptions()
        {
            return new ConversionOptions
            {
                EnableSyntaxHighlighting = EnableSyntaxHighlighting,
                EnableTableOfContents = EnableTableOfContents,
                Theme = Theme
            };
        }
    }
}
