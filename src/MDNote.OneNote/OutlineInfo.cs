using System.Collections.Generic;

namespace MDNote.OneNote
{
    /// <summary>
    /// Represents a single outline element parsed from OneNote page XML.
    /// </summary>
    public class OutlineInfo
    {
        public string Id { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public List<string> HtmlContent { get; set; } = new List<string>();
    }
}
