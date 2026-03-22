using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using WatsonWebsocket;

namespace PlayoutServer.Core.Services
{
    /// <summary>
    /// Streamer.bot WebSocket Connector
    /// (DE) Verbindung mit Streamer.bot über WebSocket zur Steuerung von Aktionen
    /// (EN) Connection to Streamer.bot via WebSocket to trigger actions
    /// </summary>
    public class StreamerbotConnector : IDisposable
    {
        private WatsonWsClient? _client;
        private string _password = string.Empty;
        private bool _authenticated = false;
        private readonly string _host;
        private readonly int _port;

        public event EventHandler<string>? OnActionTriggered;
        public event EventHandler? OnConnected;
        public event EventHandler? OnDisconnected;

        public bool IsConnected => _client?.Connected ?? false;
        public bool IsAuthenticated => _authenticated;

        public StreamerbotConnector(string host, int port, string password = "")
        {
            _host = host;
            _port = port;
            _password = password;
        }

        public async Task ConnectAsync()
        {
            try
            {
                var uri = new Uri($"ws://{_host}:{_port}");
                _client = new WatsonWsClient(uri);
                _client.MessageReceived += OnMessageReceived;
                _client.ServerConnected += OnServerConnected;
                _client.ServerDisconnected += OnServerDisconnected;

                _client.Start();
                FileLogger.Log($"[Streamer.bot] Connecting to {uri}...");

                // Wait for connection
                await Task.Delay(1000).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                FileLogger.Log($"[Streamer.bot] Connection failed: {ex.Message}");
                throw;
            }
        }

        public void Disconnect()
        {
            if (_client != null)
            {
                _client.Stop();
                _authenticated = false;
            }
        }

        public async Task TriggerActionAsync(string actionName, Dictionary<string, object>? args = null)
        {
            if (!IsConnected)
            {
                FileLogger.Log($"[Streamer.bot] Not connected, cannot trigger action: {actionName}");
                return;
            }

            var requestId = Guid.NewGuid().ToString();
            var request = new
            {
                request = "DoAction",
                action = new { name = actionName },
                args = args ?? new Dictionary<string, object>(),
                id = requestId
            };

            var json = JsonSerializer.Serialize(request);
            var buffer = Encoding.UTF8.GetBytes(json);

            try
            {
                await _client!.SendAsync(buffer).ConfigureAwait(false);
                FileLogger.Log($"[Streamer.bot] Action triggered: {actionName}");
            }
            catch (Exception ex)
            {
                FileLogger.Log($"[Streamer.bot] Failed to trigger action {actionName}: {ex.Message}");
            }
        }

        public async Task SubscribeToEventsAsync(Dictionary<string, List<string>> events)
        {
            if (!IsConnected)
            {
                FileLogger.Log("[Streamer.bot] Not connected, cannot subscribe to events");
                return;
            }

            var requestId = Guid.NewGuid().ToString();
            var request = new
            {
                request = "Subscribe",
                id = requestId,
                events = events
            };

            var json = JsonSerializer.Serialize(request);
            var buffer = Encoding.UTF8.GetBytes(json);

            try
            {
                await _client!.SendAsync(buffer).ConfigureAwait(false);
                FileLogger.Log($"[Streamer.bot] Subscribed to events");
            }
            catch (Exception ex)
            {
                FileLogger.Log($"[Streamer.bot] Failed to subscribe to events: {ex.Message}");
            }
        }

        private void OnServerConnected(object? sender, EventArgs e)
        {
            FileLogger.Log("[Streamer.bot] Server connected");
            OnConnected?.Invoke(this, EventArgs.Empty);
        }

        private void OnServerDisconnected(object? sender, EventArgs e)
        {
            FileLogger.Log("[Streamer.bot] Server disconnected");
            _authenticated = false;
            OnDisconnected?.Invoke(this, EventArgs.Empty);
        }

        private void OnMessageReceived(object? sender, WatsonWebsocket.MessageReceivedEventArgs e)
        {
            try
            {
                var json = Encoding.UTF8.GetString(e.Data);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                // Check for Hello message (first connection)
                if (root.GetProperty("request").GetString() == "Hello")
                {
                    HandleHello(json);
                    return;
                }

                // Check for event
                if (root.TryGetProperty("event", out var eventObj))
                {
                    var eventType = eventObj.GetProperty("type").GetString();
                    OnActionTriggered?.Invoke(this, eventType ?? "Unknown");
                    FileLogger.Log($"[Streamer.bot] Event received: {eventType}");
                    return;
                }

                FileLogger.Log($"[Streamer.bot] Message: {json}");
            }
            catch (Exception ex)
            {
                FileLogger.Log($"[Streamer.bot] Error parsing message: {ex.Message}");
            }
        }

        private void HandleHello(string helloJson)
        {
            FileLogger.Log("[Streamer.bot] Hello message received");

            using var doc = JsonDocument.Parse(helloJson);
            var root = doc.RootElement;

            // Check if authentication is required
            if (string.IsNullOrEmpty(_password))
            {
                _authenticated = true;
                FileLogger.Log("[Streamer.bot] No password set, authentication skipped");
                return;
            }

            if (!root.TryGetProperty("authentication", out var authObj))
            {
                _authenticated = true;
                FileLogger.Log("[Streamer.bot] No authentication required by server");
                return;
            }

            try
            {
                var saltStr = authObj.GetProperty("salt").GetString() ?? string.Empty;
                var challengeStr = authObj.GetProperty("challenge").GetString() ?? string.Empty;

                if (string.IsNullOrEmpty(saltStr) || string.IsNullOrEmpty(challengeStr))
                {
                    _authenticated = true;
                    FileLogger.Log("[Streamer.bot] Invalid salt or challenge");
                    return;
                }

                var authString = GenerateAuthenticationString(_password, saltStr, challengeStr);
                SendAuthenticationRequest(authString);
            }
            catch (Exception ex)
            {
                FileLogger.Log($"[Streamer.bot] Authentication error: {ex.Message}");
            }
        }

        private string GenerateAuthenticationString(string password, string salt, string challenge)
        {
            // 1. Concatenate password + salt and generate SHA256 hash, then base64 encode
            var secretInput = password + salt;
            using (var sha256 = SHA256.Create())
            {
                var secretHash = sha256.ComputeHash(Encoding.UTF8.GetBytes(secretInput));
                var secretBase64 = Convert.ToBase64String(secretHash);

                // 2. Concatenate base64 secret + challenge and generate SHA256 hash, then base64 encode
                var authInput = secretBase64 + challenge;
                var authHash = sha256.ComputeHash(Encoding.UTF8.GetBytes(authInput));
                var authBase64 = Convert.ToBase64String(authHash);

                return authBase64;
            }
        }

        private void SendAuthenticationRequest(string authString)
        {
            try
            {
                var requestId = Guid.NewGuid().ToString();
                var request = new
                {
                    request = "Authenticate",
                    id = requestId,
                    authentication = authString
                };

                var json = JsonSerializer.Serialize(request);
                var buffer = Encoding.UTF8.GetBytes(json);

                _client?.SendAsync(buffer).ConfigureAwait(false);
                _authenticated = true;
                FileLogger.Log("[Streamer.bot] Authentication request sent");
            }
            catch (Exception ex)
            {
                FileLogger.Log($"[Streamer.bot] Failed to send authentication: {ex.Message}");
            }
        }

        public void Dispose()
        {
            Disconnect();
            _client?.Dispose();
        }
    }
}
