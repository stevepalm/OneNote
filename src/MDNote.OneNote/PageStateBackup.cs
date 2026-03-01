using System.Collections.Generic;

namespace MDNote.OneNote
{
    /// <summary>
    /// Stores recent page XML states for undo on render failure.
    /// Thread-safe, in-memory ring buffer (no disk I/O).
    /// </summary>
    public static class PageStateBackup
    {
        private const int MaxSnapshots = 3;
        private static readonly object Lock = new object();

        // Key: pageId, Value: list of XML snapshots (most recent last)
        private static readonly Dictionary<string, List<string>> Snapshots
            = new Dictionary<string, List<string>>();

        /// <summary>
        /// Saves a snapshot of the page XML before modification.
        /// </summary>
        public static void Save(string pageId, string pageXml)
        {
            if (string.IsNullOrEmpty(pageId) || string.IsNullOrEmpty(pageXml))
                return;

            lock (Lock)
            {
                if (!Snapshots.TryGetValue(pageId, out var list))
                {
                    list = new List<string>();
                    Snapshots[pageId] = list;
                }

                list.Add(pageXml);

                while (list.Count > MaxSnapshots)
                    list.RemoveAt(0);
            }
        }

        /// <summary>
        /// Gets the most recent snapshot for undo. Removes it from the buffer.
        /// Returns null if no snapshots available.
        /// </summary>
        public static string PopLatest(string pageId)
        {
            if (string.IsNullOrEmpty(pageId))
                return null;

            lock (Lock)
            {
                if (!Snapshots.TryGetValue(pageId, out var list) || list.Count == 0)
                    return null;

                var latest = list[list.Count - 1];
                list.RemoveAt(list.Count - 1);
                return latest;
            }
        }

        /// <summary>
        /// Gets the number of stored snapshots for a page.
        /// </summary>
        public static int GetSnapshotCount(string pageId)
        {
            lock (Lock)
            {
                if (Snapshots.TryGetValue(pageId, out var list))
                    return list.Count;
                return 0;
            }
        }

        /// <summary>
        /// Clears all snapshots. Called on add-in disconnect.
        /// </summary>
        public static void Reset()
        {
            lock (Lock)
            {
                Snapshots.Clear();
            }
        }
    }
}
