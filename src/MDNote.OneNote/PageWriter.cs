namespace MDNote.OneNote
{
    /// <summary>
    /// Writes structured content back to OneNote pages.
    /// Stub for Session 1 — implementation in Session 3+.
    /// </summary>
    public class PageWriter
    {
        private readonly IOneNoteInterop _interop;

        public PageWriter(IOneNoteInterop interop)
        {
            _interop = interop;
        }
    }
}
