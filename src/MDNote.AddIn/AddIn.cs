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
        private HotkeyManager _hotkeyManager;

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

                _hotkeyManager = new HotkeyManager();
                _hotkeyManager.Register(
                    onRenderPage: () => RibbonHandler.OnRenderPage(_oneNoteApp),
                    onExport: () => RibbonHandler.OnExportClipboard(_oneNoteApp),
                    onToggleSource: () => RibbonHandler.OnToggleSource(_oneNoteApp));

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
            ToggleSourceCommand.Reset();
            _hotkeyManager?.Dispose();
            _hotkeyManager = null;

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
            try { RibbonHandler.OnRenderSelection(_oneNoteApp); }
            catch (Exception ex) { Log($"OnRenderSelection FAILED: {ex}"); }
        }

        public void OnToggleLiveMode(IRibbonControl control, bool pressed)
        {
            RibbonHandler.ShowStub("Live Mode", 3);
        }

        public void OnToggleSource(IRibbonControl control)
        {
            Log("OnToggleSource callback invoked");
            try { RibbonHandler.OnToggleSource(_oneNoteApp); }
            catch (Exception ex) { Log($"OnToggleSource FAILED: {ex}"); }
        }

        public void OnExportClipboard(IRibbonControl control)
        {
            Log("OnExportClipboard callback invoked");
            try { RibbonHandler.OnExportClipboard(_oneNoteApp); }
            catch (Exception ex) { Log($"OnExportClipboard FAILED: {ex}"); }
        }

        public void OnExportFile(IRibbonControl control)
        {
            Log("OnExportFile callback invoked");
            try { RibbonHandler.OnExportFile(_oneNoteApp); }
            catch (Exception ex) { Log($"OnExportFile FAILED: {ex}"); }
        }

        public void OnImportMarkdown(IRibbonControl control)
        {
            Log("OnImportMarkdown callback invoked");
            try { RibbonHandler.OnImportMarkdown(_oneNoteApp); }
            catch (Exception ex) { Log($"OnImportMarkdown FAILED: {ex}"); }
        }

        public void OnPasteRender(IRibbonControl control)
        {
            Log("OnPasteRender callback invoked");
            try { RibbonHandler.OnPasteRender(_oneNoteApp); }
            catch (Exception ex) { Log($"OnPasteRender FAILED: {ex}"); }
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
