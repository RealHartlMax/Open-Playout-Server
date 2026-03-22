using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace PlayoutServer.Core.Services
{
    /// <summary>
    /// Diagnostics-Service für detailliertes Tracking von Verbindungen und Service-Status
    /// </summary>
    public static class DiagnosticsService
    {
        private static readonly Dictionary<string, ServiceInfo> _serviceStatus = new();
        private static readonly object _lock = new object();

        public class ServiceInfo
        {
            public string ServiceName { get; set; } = string.Empty;
            public string Status { get; set; } = "Unknown";
            public DateTime LastStatusUpdate { get; set; }
            public string? LastError { get; set; }
            public int ConnectionAttempts { get; set; }
            public long ResponseTimeMs { get; set; }
            public Dictionary<string, string> Details { get; set; } = new();
        }

        public static void RegisterService(string serviceName)
        {
            lock (_lock)
            {
                if (!_serviceStatus.ContainsKey(serviceName))
                {
                    _serviceStatus[serviceName] = new ServiceInfo
                    {
                        ServiceName = serviceName,
                        Status = "Registered",
                        LastStatusUpdate = DateTime.Now
                    };
                    FileLogger.LogDebug($"[DIAGNOSTICS] Service registered: {serviceName}");
                }
            }
        }

        public static void LogConnectionAttempt(string serviceName, string host, int port)
        {
            lock (_lock)
            {
                if (!_serviceStatus.ContainsKey(serviceName))
                    RegisterService(serviceName);

                _serviceStatus[serviceName].ConnectionAttempts++;
                _serviceStatus[serviceName].Status = "Connecting";
                _serviceStatus[serviceName].Details["Target"] = $"{host}:{port}";
                _serviceStatus[serviceName].LastStatusUpdate = DateTime.Now;

                FileLogger.LogDebug($"[DIAGNOSTICS] {serviceName} connection attempt #{_serviceStatus[serviceName].ConnectionAttempts} to {host}:{port}");
            }
        }

        public static void LogConnectionSuccess(string serviceName, string serviceVersion = "", string? instanceId = null)
        {
            lock (_lock)
            {
                if (!_serviceStatus.ContainsKey(serviceName))
                    RegisterService(serviceName);

                _serviceStatus[serviceName].Status = "Connected";
                _serviceStatus[serviceName].LastStatusUpdate = DateTime.Now;
                _serviceStatus[serviceName].LastError = null;
                if (!string.IsNullOrEmpty(serviceVersion))
                    _serviceStatus[serviceName].Details["Version"] = serviceVersion;
                if (!string.IsNullOrEmpty(instanceId))
                    _serviceStatus[serviceName].Details["InstanceId"] = instanceId;

                FileLogger.Log($"✓ [DIAGNOSTICS] {serviceName} connected successfully (v{serviceVersion})", LogLevel.Info);
            }
        }

        public static void LogConnectionFailure(string serviceName, string reason, Exception? ex = null)
        {
            lock (_lock)
            {
                if (!_serviceStatus.ContainsKey(serviceName))
                    RegisterService(serviceName);

                _serviceStatus[serviceName].Status = "Failed";
                _serviceStatus[serviceName].LastStatusUpdate = DateTime.Now;
                _serviceStatus[serviceName].LastError = reason;

                var errorMsg = ex != null ? $"{reason} ({ex.GetType().Name}: {ex.Message})" : reason;
                FileLogger.LogWarning($"✗ [DIAGNOSTICS] {serviceName} connection failed: {errorMsg}");
            }
        }

        public static void LogServiceMethod(string serviceName, string methodName, long elapsedMs, bool success, string? details = null)
        {
            lock (_lock)
            {
                if (!_serviceStatus.ContainsKey(serviceName))
                    RegisterService(serviceName);

                _serviceStatus[serviceName].ResponseTimeMs = elapsedMs;

                var status = success ? "✓" : "✗";
                var msg = $"[DIAGNOSTICS] {status} {serviceName}.{methodName} [{elapsedMs}ms]";
                if (!string.IsNullOrEmpty(details))
                    msg += $" | {details}";

                FileLogger.LogDebug(msg);
            }
        }

        public static void LogPortConflict(int port, string blockingService)
        {
            FileLogger.LogWarning($"[DIAGNOSTICS] Port conflict detected: Port {port} is in use by {blockingService}");
        }

        public static string GetDiagnosticsReport()
        {
            lock (_lock)
            {
                var report = "\n========== DIAGNOSTICS REPORT ==========\n";
                report += $"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n\n";

                if (_serviceStatus.Count == 0)
                {
                    report += "No services registered.\n";
                }
                else
                {
                    foreach (var svc in _serviceStatus.Values)
                    {
                        report += $"SERVICE: {svc.ServiceName}\n";
                        report += $"  Status: {svc.Status}\n";
                        report += $"  Last Update: {svc.LastStatusUpdate:yyyy-MM-dd HH:mm:ss}\n";
                        report += $"  Connection Attempts: {svc.ConnectionAttempts}\n";
                        report += $"  Last Response Time: {svc.ResponseTimeMs}ms\n";

                        if (svc.LastError != null)
                            report += $"  Last Error: {svc.LastError}\n";

                        if (svc.Details.Count > 0)
                        {
                            report += "  Details:\n";
                            foreach (var detail in svc.Details)
                                report += $"    - {detail.Key}: {detail.Value}\n";
                        }

                        report += "\n";
                    }
                }

                report += "=========================================\n";
                return report;
            }
        }

        public static bool IsServiceHealthy(string serviceName)
        {
            lock (_lock)
            {
                if (_serviceStatus.ContainsKey(serviceName))
                    return _serviceStatus[serviceName].Status == "Connected";
                return false;
            }
        }
    }
}