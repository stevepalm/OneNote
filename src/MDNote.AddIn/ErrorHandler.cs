namespace MDNote
{
    using System;
    using System.IO;

    /// <summary>
    /// Centralized logging and error handling. Logs to dated files in
    /// %LOCALAPPDATA%\MDNote\logs\. Never throws exceptions.
    /// </summary>
    internal static class ErrorHandler
    {
        private static readonly string LogDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MDNote", "logs");

        private static readonly object Lock = new object();

        public static void Log(string message)
        {
            WriteLog("INFO", message);
        }

        public static void LogWarning(string message)
        {
            WriteLog("WARN", message);
        }

        public static void LogError(string context, Exception ex)
        {
            WriteLog("ERROR", $"{context}: {ex}");
        }

        /// <summary>
        /// Logs the full exception and shows a user-friendly notification.
        /// </summary>
        public static void HandleError(string userMessage, Exception ex)
        {
            LogError(userMessage, ex);
            var detail = ex != null ? $"\n\n{ex.GetType().Name}: {ex.Message}" : "";
            NotificationHelper.ShowError(userMessage + detail);
        }

        private static void WriteLog(string level, string message)
        {
            try
            {
                lock (Lock)
                {
                    if (!Directory.Exists(LogDir))
                        Directory.CreateDirectory(LogDir);

                    var path = Path.Combine(LogDir,
                        $"mdnote-{DateTime.Now:yyyy-MM-dd}.log");

                    File.AppendAllText(path,
                        $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {message}{Environment.NewLine}");
                }
            }
            catch
            {
                // Never throw from the error handler
            }
        }
    }
}
