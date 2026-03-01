using System.Collections.Generic;

namespace MDNote.Core.Models
{
    public class ConversionResult
    {
        public string Html { get; set; }
        public string Title { get; set; }
        public List<MermaidBlock> MermaidBlocks { get; set; } = new List<MermaidBlock>();
        public List<HeadingInfo> Headings { get; set; } = new List<HeadingInfo>();
        public Dictionary<string, string> FrontMatter { get; set; } = new Dictionary<string, string>();
    }
}
