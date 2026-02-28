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
    [ClassInterface(ClassInterfaceType.AutoDispatch)]
    public class AddIn : IDTExtensibility2, IRibbonExtensibility
    {
        private static readonly string LogPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MDNote", "addin.log");

        private IRibbonUI _ribbon;
        private object _oneNoteApp;

        private static void Log(string message)
        {
            try
            {
                var dir = Path.GetDirectoryName(LogPath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.AppendAllText(LogPath,
                    $"[{DateTime.Now:HH:mm:ss.fff}] {message}{Environment.NewLine}");
            }
            catch { }
        }

        // --- IDTExtensibility2 ---

        public void OnConnection(
            object Application,
            ext_ConnectMode ConnectMode,
            object AddInInst,
            ref Array custom)
        {
            try
            {
                Log($"OnConnection called. ConnectMode={ConnectMode}");
                _oneNoteApp = Application;
                Log("OnConnection OK");
            }
            catch (Exception ex)
            {
                Log($"OnConnection FAILED: {ex}");
            }
        }

        public void OnDisconnection(
            ext_DisconnectMode RemoveMode,
            ref Array custom)
        {
            Log("OnDisconnection called");
            if (_oneNoteApp != null)
            {
                Marshal.ReleaseComObject(_oneNoteApp);
                _oneNoteApp = null;
            }
            _ribbon = null;
        }

        public void OnAddInsUpdate(ref Array custom) { }
        public void OnStartupComplete(ref Array custom) { Log("OnStartupComplete"); }
        public void OnBeginShutdown(ref Array custom) { Log("OnBeginShutdown"); }

        // --- IRibbonExtensibility ---

        public string GetCustomUI(string RibbonID)
        {
            try
            {
                Log($"GetCustomUI called. RibbonID='{RibbonID}'");

                var assembly = Assembly.GetExecutingAssembly();
                var names = assembly.GetManifestResourceNames();
                Log($"Embedded resources: [{string.Join(", ", names)}]");

                using (var stream = assembly.GetManifestResourceStream("MDNote.Ribbon.xml"))
                {
                    if (stream == null)
                    {
                        Log("ERROR: Ribbon.xml resource stream is null");
                        return null;
                    }
                    using (var reader = new StreamReader(stream))
                    {
                        var xml = reader.ReadToEnd();
                        Log($"Returning ribbon XML ({xml.Length} chars)");
                        return xml;
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"GetCustomUI FAILED: {ex}");
                return null;
            }
        }

        // --- Ribbon Callbacks ---

        public void RibbonLoaded(IRibbonUI ribbonUI)
        {
            _ribbon = ribbonUI;
        }

        public void OnRenderPage(IRibbonControl control)
        {
            Log("OnRenderPage callback invoked");
            try
            {
                RibbonHandler.OnRenderPage(_oneNoteApp);
                Log("OnRenderPage completed");
            }
            catch (Exception ex) { Log($"OnRenderPage FAILED: {ex}"); }
        }

        public void OnRenderSelection(IRibbonControl control)
        {
            Log("OnRenderSelection callback invoked");
            try { RibbonHandler.ShowStub("Render Selection", 2); }
            catch (Exception ex) { Log($"OnRenderSelection FAILED: {ex}"); }
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
