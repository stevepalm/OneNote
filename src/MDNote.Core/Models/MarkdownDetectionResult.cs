using System.Collections.Generic;

namespace MDNote.Core.Models
{
    /// <summary>
    /// Result of heuristic markdown detection.
    /// </summary>
    public class MarkdownDetectionResult
    {
        /// <summary>
        /// Normalized score (total points / line count). Higher = more likely markdown.
        /// </summary>
        public double Score { get; set; }

        /// <summary>
        /// True if the score meets or exceeds the detection threshold (0.15).
        /// </summary>
        public bool IsMarkdown { get; set; }

        /// <summary>
        /// Human-readable descriptions of matched patterns (e.g., "3 headings (+9)").
        /// </summary>
        public List<string> Indicators { get; set; } = new List<string>();
    }
}
