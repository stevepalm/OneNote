namespace MDNote.OneNote
{
    using Microsoft.Office.Interop.OneNote;
    using System;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Xml.Linq;

    public class OneNoteInterop : IOneNoteInterop
    {
        private static readonly XNamespace OneNs =
            "http://schemas.microsoft.com/office/onenote/2013/onenote";

        private readonly IApplication _app;

        /// <summary>
        /// Wraps an existing OneNote Application COM object
        /// (received from IDTExtensibility2.OnConnection).
        /// </summary>
        public OneNoteInterop(object applicationObject)
        {
            _app = (IApplication)applicationObject;
        }

        /// <summary>
        /// Creates a new OneNote Application COM object.
        /// Used when not running as an add-in (e.g., standalone testing).
        /// </summary>
        public OneNoteInterop()
        {
            _app = new Application();
        }

        public string GetActivePageId()
        {
            return _app.Windows.CurrentWindow?.CurrentPageId;
        }

        public string GetPageContent(string pageId)
        {
            if (string.IsNullOrEmpty(pageId))
                return null;

            return RetryOnBusy(() =>
            {
                _app.GetPageContent(pageId, out var xml, PageInfo.piAll, XMLSchema.xs2013);
                return xml;
            });
        }

        public string GetPageTitle(string pageId)
        {
            var xml = GetPageContent(pageId);
            if (string.IsNullOrEmpty(xml))
                return "(no content)";

            var page = XElement.Parse(xml);
            var title = page
                .Elements(OneNs + "Title")
                .Elements(OneNs + "OE")
                .Elements(OneNs + "T")
                .Select(t => t.Value)
                .FirstOrDefault();

            return title ?? "(untitled)";
        }

        public string GetPagePlainText(string pageId)
        {
            var xml = GetPageContent(pageId);
            if (string.IsNullOrEmpty(xml))
                return string.Empty;

            var page = XElement.Parse(xml);
            var texts = page
                .Descendants(OneNs + "T")
                .Select(t => t.Value);

            return string.Join("\n", texts);
        }

        public void UpdatePageContent(string xml)
        {
            RetryOnBusy(() =>
            {
                _app.UpdatePageContent(xml, DateTime.MinValue, XMLSchema.xs2013, true);
                return true;
            });
        }

        public void NavigateToPage(string pageId)
        {
            _app.NavigateTo(pageId);
        }

        /// <summary>
        /// Retries a COM operation up to 3 times when OneNote reports busy.
        /// </summary>
        private T RetryOnBusy<T>(Func<T> action, int maxRetries = 3)
        {
            const uint HR_COM_BUSY = 0x8001010A;
            int retries = 0;

            while (true)
            {
                try
                {
                    return action();
                }
                catch (COMException ex) when ((uint)ex.ErrorCode == HR_COM_BUSY
                                               && retries < maxRetries)
                {
                    retries++;
                    System.Threading.Thread.Sleep(250 * retries);
                }
            }
        }
    }
}
