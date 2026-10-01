namespace FBMMultiMessenger.Buisness.Helpers
{
    /// <summary>
    /// Thread-safe, append-only logger to files under the app's Logs/ directory. Used for lightweight
    /// diagnostics (e.g. extension disconnect reasons) that are later viewed via the diagnostics endpoint.
    /// Never throws — logging must never break the caller.
    /// </summary>
    public static class DiagnosticFileLogger
    {
        private static readonly object _lock = new();

        /// <summary>The shared Logs directory (same location the conflict-data writers use).</summary>
        public static string LogsDirectory => Path.Combine(Directory.GetCurrentDirectory(), "Logs");

        /// <summary>Appends a single timestamped line to the given log file (created if missing).</summary>
        public static void Append(string fileName, string line)
        {
            try
            {
                var dir = LogsDirectory;
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                var path = Path.Combine(dir, fileName);
                var entry = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}Z {line}{Environment.NewLine}";

                lock (_lock)
                {
                    File.AppendAllText(path, entry);
                }
            }
            catch
            {
                // Diagnostics must never throw.
            }
        }
    }
}
