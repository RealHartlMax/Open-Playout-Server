using System;
using System.IO;

namespace PlayoutServer.Core.Services
{
    public static class FileLogger
    {
        private static readonly object _lock = new object();
        private static string _logFilePath = string.Empty;

        public static void Initialize(string logDirectory, string prefix)
        {
            if (string.IsNullOrWhiteSpace(prefix)) prefix = "Log";
            if (string.IsNullOrWhiteSpace(logDirectory)) logDirectory = AppContext.BaseDirectory;

            var corePath = Path.Combine(AppContext.BaseDirectory, "Logs", "Log_core");
            Directory.CreateDirectory(corePath);

            var fileName = $"{prefix}_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt";
            _logFilePath = Path.Combine(corePath, fileName);
            Log($"=== Logging started for {prefix} ({DateTime.Now:yyyy-MM-dd HH:mm:ss}) ===");
        }

        public static void Log(string message)
        {
            try
            {
                lock (_lock)
                {
                    if (string.IsNullOrWhiteSpace(_logFilePath))
                    {
                        Initialize(null!, "Log");
                    }

                    var formatted = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}";
                    File.AppendAllText(_logFilePath, formatted + Environment.NewLine);
                    Console.WriteLine(formatted);
                }
            }
            catch
            {
                // swallow, avoid throwing from logger
            }
        }
    }
}
