namespace MDNote
{
    using Extensibility;
    using Microsoft.Office.Core;
    using System;
    using System.IO;
    using System.Reflection;
    using System.Runtime.InteropServices;

    [ComVisible(true)]
    [Guid("A1B2C3D4-E5F6-4A7B-8C9D-0E1F2A3B4C5D")]
    [ProgId("MDNote.AddIn")]
    [ClassInterface(ClassInterfaceType.None)]
    public class AddIn : IDTExtensibility2, IRibbonExtensibility
    {
        private IRibbonUI _ribbon;
        private object _oneNoteApp;

        // --- IDTExtensibility2 ---

        public void OnConnection(
            object Application,
            ext_ConnectMode ConnectMode,
            object AddInInst,
            ref Array custom)
        {
            _oneNoteApp = Application;
        }

        public void OnDisconnection(
            ext_DisconnectMode RemoveMode,
            ref Array custom)
        {
            if (_oneNoteApp != null)
            {
                Marshal.ReleaseComObject(_oneNoteApp);
                _oneNoteApp = null;
            }
            _ribbon = null;
        }

        public void OnAddInsUpdate(ref Array custom) { }
        public void OnStartupComplete(ref Array custom) { }
        public void OnBeginShutdown(ref Array custom) { }

        // --- IRibbonExtensibility ---

        public string GetCustomUI(string RibbonID)
        {
            if (RibbonID != "Microsoft.OneNote")
                return null;

            var assembly = Assembly.GetExecutingAssembly();
            using (var stream = assembly.GetManifestResourceStream("MDNote.Ribbon.xml"))
            using (var reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }

        // --- Ribbon Callbacks ---

        public void RibbonLoaded(IRibbonUI ribbonUI)
        {
            _ribbon = ribbonUI;
        }

        public void OnRenderPage(IRibbonControl control)
        {
            RibbonHandler.OnRenderPage(_oneNoteApp);
        }

        public void OnRenderSelection(IRibbonControl control)
        {
            RibbonHandler.ShowStub("Render Selection", 2);
        }

        public void OnToggleLiveMode(IRibbonControl control, bool pressed)
        {
            RibbonHandler.ShowStub("Live Mode", 3);
        }

        public void OnToggleSource(IRibbonControl control)
        {
            RibbonHandler.ShowStub("Toggle Source", 2);
        }

        public void OnExportClipboard(IRibbonControl control)
        {
            RibbonHandler.ShowStub("Export Clipboard", 2);
        }

        public void OnExportFile(IRibbonControl control)
        {
            RibbonHandler.ShowStub("Export File", 2);
        }

        public void OnImportMarkdown(IRibbonControl control)
        {
            RibbonHandler.ShowStub("Import MD", 2);
        }

        public void OnPasteRender(IRibbonControl control)
        {
            RibbonHandler.ShowStub("Paste & Render", 2);
        }

        public void OnInsertToc(IRibbonControl control)
        {
            RibbonHandler.ShowStub("Insert TOC", 3);
        }

        public void OnOpenSettings(IRibbonControl control)
        {
            RibbonHandler.ShowStub("Settings", 4);
        }

        public void OnShowAbout(IRibbonControl control)
        {
            RibbonHandler.ShowStub("About", 1);
        }
    }
}
