namespace MDNote
{
    using MDNote.Core;
    using MDNote.OneNote;
    using System;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using System.Threading;

    /// <summary>
    /// Polls the active OneNote page on a timer and auto-renders when markdown
    /// content changes. Pauses in source view mode or when OneNote is not foreground.
    /// Uses System.Threading.Timer so the dllhost STA thread is never blocked.
    /// </summary>
    internal class LiveModeManager : IDisposable
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern int GetWindowThreadProcessId(IntPtr hWnd, out int processId);

        private Timer _timer;
        private object _oneNoteApp;
        private string _lastSourceHash;
        private string _lastTextHash;
        private bool _disposed;

        public bool IsActive { get; private set; }

        /// <summary>
        /// Initializes the manager. If live mode is enabled in settings, starts the timer.
        /// Subscribes to SettingsChanged to sync state in real time.
        /// </summary>
        public void Start(object oneNoteApp)
        {
            _oneNoteApp = oneNoteApp;
            SettingsManager.Instance.SettingsChanged += OnSettingsChanged;
            SyncState();
        }

        /// <summary>
        /// Toggles live mode on/off, persists the change, and syncs the timer.
        /// </summary>
        public void Toggle(object oneNoteApp)
        {
            _oneNoteApp = oneNoteApp;
            var settings = SettingsManager.Current;
            settings.LiveModeEnabled = !settings.LiveModeEnabled;
            SettingsManager.Instance.Save();
        }

        private void OnSettingsChanged(object sender, EventArgs e)
        {
            SyncState();
        }

        private void SyncState()
        {
            var settings = SettingsManager.Current;
            var interval = Math.Max(500, settings.LiveModeDelayMs);

            if (settings.LiveModeEnabled)
            {
                if (_timer == null)
                {
                    _timer = new Timer(OnTimerTick, null,
                        Timeout.Infinite, Timeout.Infinite);
                }

                if (!IsActive)
                {
                    // First activation — seed hashes from current page to avoid
                    // an immediate re-render of already-rendered content.
                    SeedHashes();
                    _timer.Change(interval, interval);
                    IsActive = true;
                    ErrorHandler.Log("Live mode started.");
                }
                else
                {
                    // Update interval if it changed
                    _timer.Change(interval, interval);
                }
            }
            else
            {
                if (IsActive)
                {
                    _timer?.Change(Timeout.Infinite, Timeout.Infinite);
                    IsActive = false;
                    _lastSourceHash = null;
                    _lastTextHash = null;
                    ErrorHandler.Log("Live mode stopped.");
                }
            }
        }

        private void SeedHashes()
        {
            try
            {
                var interop = new OneNoteInterop(_oneNoteApp);
                var pageId = interop.GetActivePageId();
                if (string.IsNullOrEmpty(pageId)) return;

                var pageXml = interop.GetPageContent(pageId);
                if (string.IsNullOrEmpty(pageXml)) return;

                var parser = new PageXmlParser(pageXml);
                var storedMarkdown = parser.GetStoredMarkdownSource();

                if (!string.IsNullOrEmpty(storedMarkdown))
                    _lastSourceHash = storedMarkdown.GetHashCode().ToString();
                else
                    _lastTextHash = parser.GetOutlinePlainText().GetHashCode().ToString();
            }
            catch
            {
                // Seeding is best-effort; timer will work correctly even without it
            }
        }

        private void OnTimerTick(object state)
        {
            // Skip if another command is already running
            if (CommandRunner.IsBusy)
                return;

            if (!IsOneNoteForeground())
                return;

            // Dispatch the render work to an STA thread via CommandRunner.
            // TryRunCommand silently skips if a command started between our check and now.
            CommandRunner.TryRunCommand(() => DoLiveRender(), "Live Mode");
        }

        private void DoLiveRender()
        {
            try
            {
                var interop = new OneNoteInterop(_oneNoteApp);
                var pageId = interop.GetActivePageId();
                if (string.IsNullOrEmpty(pageId))
                    return;

                var pageXml = interop.GetPageContent(pageId);
                if (string.IsNullOrEmpty(pageXml))
                    return;

                var parser = new PageXmlParser(pageXml);

                // Skip pages in source view mode
                var viewMode = parser.GetMetaValue("md-note-view-mode");
                if (viewMode == "source")
                    return;

                var storedMarkdown = parser.GetStoredMarkdownSource();

                if (!string.IsNullOrEmpty(storedMarkdown))
                {
                    // Page has stored markdown source — check if it changed
                    var hash = storedMarkdown.GetHashCode().ToString();
                    if (hash == _lastSourceHash)
                        return;

                    _lastSourceHash = hash;

                    var markdown = storedMarkdown;
                    var options = SettingsManager.Current.ToConversionOptions();
                    var converter = new MarkdownConverter();
                    var result = converter.Convert(markdown, options);

                    var writer = new PageWriter(interop);
                    writer.RenderMarkdownToPage(pageId, result, markdown);
                    ErrorHandler.Log("Live mode: re-rendered from stored source.");
                }
                else
                {
                    // No stored source — check if plain text looks like new markdown
                    var plainText = parser.GetOutlinePlainText();
                    if (string.IsNullOrWhiteSpace(plainText))
                        return;

                    var hash = plainText.GetHashCode().ToString();
                    if (hash == _lastTextHash)
                        return;

                    _lastTextHash = hash;

                    var detection = MarkdownDetector.Detect(plainText);
                    if (!detection.IsMarkdown)
                        return;

                    var options = SettingsManager.Current.ToConversionOptions();
                    var converter = new MarkdownConverter();
                    var result = converter.Convert(plainText, options);

                    var writer = new PageWriter(interop);
                    writer.RenderMarkdownToPage(pageId, result, plainText);

                    // After first render the page now has stored source;
                    // seed the source hash to prevent re-triggering
                    var newXml = interop.GetPageContent(pageId);
                    if (!string.IsNullOrEmpty(newXml))
                    {
                        var newParser = new PageXmlParser(newXml);
                        var newMarkdown = newParser.GetStoredMarkdownSource();
                        if (!string.IsNullOrEmpty(newMarkdown))
                            _lastSourceHash = newMarkdown.GetHashCode().ToString();
                    }

                    ErrorHandler.Log("Live mode: auto-rendered new markdown.");
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.LogError("Live mode render", ex);
            }
        }

        private static bool IsOneNoteForeground()
        {
            try
            {
                var foreground = GetForegroundWindow();
                if (foreground == IntPtr.Zero)
                    return false;

                GetWindowThreadProcessId(foreground, out int pid);
                var process = Process.GetProcessById(pid);
                return process.ProcessName.IndexOf(
                    "ONENOTE", StringComparison.OrdinalIgnoreCase) >= 0;
            }
            catch
            {
                return false;
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            SettingsManager.Instance.SettingsChanged -= OnSettingsChanged;
            _timer?.Change(Timeout.Infinite, Timeout.Infinite);
            _timer?.Dispose();
            _timer = null;
            IsActive = false;
            ErrorHandler.Log("Live mode manager disposed.");
        }
    }
}
