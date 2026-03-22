using System;
using System.IO;

namespace PlayoutServer.Core.Services
{
    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error,
        Critical
    }

    public static class FileLogger
    {
        private static readonly object _lock = new object();
        private static string _logFilePath = string.Empty;
        private static LogLevel _minLogLevel = LogLevel.Debug;

        public static void Initialize(string logDirectory, string prefix)
        {
            if (string.IsNullOrWhiteSpace(prefix)) prefix = "Log";
            if (string.IsNullOrWhiteSpace(logDirectory)) logDirectory = AppContext.BaseDirectory;

            var corePath = Path.Combine(AppContext.BaseDirectory, "Logs", "Log_core");
            Directory.CreateDirectory(corePath);

            var fileName = $"{prefix}_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt";
            _logFilePath = Path.Combine(corePath, fileName);
            Log($"=== Logging started for {prefix} ({DateTime.Now:yyyy-MM-dd HH:mm:ss}) ===", LogLevel.Info);
        }

        public static void SetMinimumLogLevel(LogLevel level)
        {
            lock (_lock)
            {
                _minLogLevel = level;
            }
        }

        public static void Log(string message, LogLevel level = LogLevel.Info)
        {
            try
            {
                lock (_lock)
                {
                    if (string.IsNullOrWhiteSpace(_logFilePath))
                    {
                        Initialize(null!, "Log");
                    }

                    if (level < _minLogLevel) return;

                    var levelStr = level.ToString().ToUpper();
                    var formatted = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{levelStr}] {message}";
                    File.AppendAllText(_logFilePath, formatted + Environment.NewLine);
                    Console.WriteLine(formatted);
                }
            }
            catch
            {
                // swallow, avoid throwing from logger
            }
        }

        public static void LogError(string message, Exception? ex = null)
        {
            var fullMsg = ex != null ? $"{message} | Exception: {ex.GetType().Name}: {ex.Message}" : message;
            Log(fullMsg, LogLevel.Error);
        }

        public static void LogWarning(string message)
        {
            Log(message, LogLevel.Warning);
        }

        public static void LogDebug(string message)
        {
            Log(message, LogLevel.Debug);
        }
    }
}
