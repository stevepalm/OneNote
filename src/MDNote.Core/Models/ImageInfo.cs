namespace MDNote.Core.Models
{
    /// <summary>
    /// Represents an embedded image extracted from OneNote page XML.
    /// </summary>
    public class ImageInfo
    {
        public string Id { get; set; }
        public string FileName { get; set; }
        public byte[] Data { get; set; }

        /// <summary>
        /// The original src attribute or CallbackID reference from the OneNote XML.
        /// Used for rewriting image paths during export.
        /// </summary>
        public string OriginalReference { get; set; }
    }
}
