namespace MDNote.Core.Models
{
    public class ConversionOptions
    {
        public bool EnableSyntaxHighlighting { get; set; } = true;
        public bool EnableTableOfContents { get; set; } = false;
        public string Theme { get; set; } = "dark";
        public bool InlineAllStyles { get; set; } = true;
    }
}
