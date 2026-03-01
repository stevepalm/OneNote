namespace MDNote
{
    using System;
    using System.Threading;

    /// <summary>
    /// Dispatches ribbon command work to separate STA threads so the COM
    /// thread (dllhost.exe) returns immediately and never shows
    /// "COM Surrogate is not responding."
    /// </summary>
    internal static class CommandRunner
    {
        private static int _busy;
        private static string _currentOperation;

        /// <summary>
        /// True when a page-mutating command is running on a worker thread.
        /// </summary>
        public static bool IsBusy => _busy != 0;

        /// <summary>
        /// Runs a page-mutating command on a new STA thread.
        /// If a command is already running, shows a "busy" notification and returns.
        /// </summary>
        public static void RunCommand(Action action, string operationName)
        {
            if (Interlocked.CompareExchange(ref _busy, 1, 0) != 0)
            {
                NotificationHelper.ShowWarning(
                    $"Please wait \u2014 {_currentOperation ?? "operation"} in progress.");
                return;
            }

            _currentOperation = operationName;
            StartWorkerThread(action, operationName, releaseBusy: true);
        }

        /// <summary>
        /// Like RunCommand but silently returns false if already busy.
        /// Used for automatic triggers (live mode, paste detection) that
        /// should not nag the user.
        /// </summary>
        public static bool TryRunCommand(Action action, string operationName)
        {
            if (Interlocked.CompareExchange(ref _busy, 1, 0) != 0)
                return false;

            _currentOperation = operationName;
            StartWorkerThread(action, operationName, releaseBusy: true);
            return true;
        }

        /// <summary>
        /// Runs a non-mutating dialog (Settings, About) on a new STA thread.
        /// Does not use the busy guard — dialogs can open while a render runs.
        /// </summary>
        public static void RunDialog(Action action)
        {
            StartWorkerThread(action, "Dialog", releaseBusy: false);
        }

        private static void StartWorkerThread(
            Action action, string operationName, bool releaseBusy)
        {
            var thread = new Thread(() =>
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    ErrorHandler.HandleError($"{operationName} failed.", ex);
                }
                finally
                {
                    if (releaseBusy)
                    {
                        _currentOperation = null;
                        Interlocked.Exchange(ref _busy, 0);
                    }
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.IsBackground = true;
            thread.Name = $"MDNote-{operationName}";
            thread.Start();
        }
    }
}
