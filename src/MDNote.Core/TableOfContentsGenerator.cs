using System.Collections.Generic;
using System.Net;
using System.Text;
using MDNote.Core.Models;

namespace MDNote.Core
{
    internal class TableOfContentsGenerator
    {
        public string GenerateToc(List<HeadingInfo> headings)
        {
            if (headings == null || headings.Count == 0)
                return string.Empty;

            var sb = new StringBuilder();
            sb.Append("<div style=\"margin:16px 0;padding:12px 16px;border:1px solid #ddd;border-radius:4px;\">");
            sb.Append("<p style=\"font-weight:bold;margin:0 0 8px 0;\">Table of Contents</p>");

            int minLevel = int.MaxValue;
            foreach (var h in headings)
            {
                if (h.Level < minLevel)
                    minLevel = h.Level;
            }

            BuildNestedList(sb, headings, 0, minLevel, out _);

            sb.Append("</div>");
            return sb.ToString();
        }

        private void BuildNestedList(StringBuilder sb, List<HeadingInfo> headings,
            int startIndex, int currentLevel, out int nextIndex)
        {
            sb.Append("<ul style=\"margin:0;padding-left:20px;list-style-type:disc;\">");

            int i = startIndex;
            while (i < headings.Count)
            {
                var heading = headings[i];

                if (heading.Level < currentLevel)
                {
                    break;
                }

                if (heading.Level == currentLevel)
                {
                    var text = WebUtility.HtmlEncode(heading.Text ?? "");
                    var href = !string.IsNullOrEmpty(heading.Id) ? $" href=\"#{heading.Id}\"" : "";
                    sb.Append($"<li style=\"margin:2px 0;\"><a{href}>{text}</a>");

                    // Check if next item is a deeper level
                    if (i + 1 < headings.Count && headings[i + 1].Level > currentLevel)
                    {
                        BuildNestedList(sb, headings, i + 1, headings[i + 1].Level, out i);
                    }
                    else
                    {
                        i++;
                    }

                    sb.Append("</li>");
                }
                else
                {
                    // Deeper level without a parent at current level — create nested list
                    BuildNestedList(sb, headings, i, heading.Level, out i);
                }
            }

            sb.Append("</ul>");
            nextIndex = i;
        }
    }
}
