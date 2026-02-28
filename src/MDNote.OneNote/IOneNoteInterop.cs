namespace MDNote.OneNote
{
    /// <summary>
    /// Abstraction over the OneNote COM API for testability.
    /// </summary>
    public interface IOneNoteInterop
    {
        string GetActivePageId();
        string GetPageContent(string pageId);
        string GetPageTitle(string pageId);
        string GetPagePlainText(string pageId);
        void UpdatePageContent(string xml);
        void NavigateToPage(string pageId);
    }
}
