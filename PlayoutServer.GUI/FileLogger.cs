using System;
using System.IO;

namespace PlayoutServer.GUI
{
    public static class FileLogger
    {
        private static readonly object _lock = new object();
        private static string _logFilePath = string.Empty;

        public static void Initialize(string logDirectory, string prefix)
        {
            if (string.IsNullOrWhiteSpace(prefix)) prefix = "Log";
            if (string.IsNullOrWhiteSpace(logDirectory)) logDirectory = AppContext.BaseDirectory;

            var guiPath = Path.Combine(AppContext.BaseDirectory, "Logs", "Log_gui");
            Directory.CreateDirectory(guiPath);

            var fileName = $"{prefix}_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt";
            _logFilePath = Path.Combine(guiPath, fileName);
            Log($"=== GUI Logging started ({DateTime.Now:yyyy-MM-dd HH:mm:ss}) ===");
        }

        public static void Log(string message)
        {
            try
            {
                lock (_lock)
                {
                    if (string.IsNullOrWhiteSpace(_logFilePath))
                    {
                        Initialize(null!, "gui");
                    }

                    var formatted = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}";
                    File.AppendAllText(_logFilePath, formatted + Environment.NewLine);
                    Console.WriteLine(formatted);
                }
            }
            catch
            {
                // ignore exceptions during logging
            }
        }
    }
}
