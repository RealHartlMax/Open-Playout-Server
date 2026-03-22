using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using PlayoutServer.Core.Services;
using WatsonWebsocket;

namespace PlayoutServer.Core.Services
{
    /// <summary>
    /// WebSocket-Server verteilt Status-Events an alle verbundenen Clients.
    /// (DE) Nachrichtenempfang und Weiterleitung an Status-Handler.
    /// (EN) Receives commands and broadcasts events to connected clients.
    /// </summary>
    public class WebSocketServer : IDisposable
    {
        private WatsonWsServer? _server;
        private readonly ConcurrentDictionary<string, bool> _clients = new();

        public bool IsRunning => _server != null;

        // (DE) Event, wenn eine beliebige Rohnachricht empfangen wurde.
        // (EN) Event when any raw message is received.
        public event EventHandler<string>? OnMessageReceived;

        // (DE) Kommandos aus der Client-Nachricht.
        // (EN) Commands from the client message.
        public event Func<string, object?, Task>? OnCommandReceived;

        public void Start(int port)
        {
            _server = new WatsonWsServer("0.0.0.0", port, false);
            _server.ClientConnected += (s, e) => _clients[e.Client.IpPort] = true;
            _server.ClientDisconnected += (s, e) => _clients.TryRemove(e.Client.IpPort, out _);
            _server.MessageReceived += async (s, e) =>
            {
                var message = Encoding.UTF8.GetString(e.Data);
                OnMessageReceived?.Invoke(this, message);

                if (OnCommandReceived == null) return;

                string command = message.Trim();

                try
                {
                    if (message.StartsWith("{", StringComparison.Ordinal))
                    {
                        var cmdObj = JsonConvert.DeserializeObject<dynamic>(message);
                        if (cmdObj?.command != null)
                        {
                            command = (string)cmdObj.command;
                        }
                    }
                }
                catch
                {
                    // ignore invalid JSON and treat as plain command text
                }

                if (string.IsNullOrWhiteSpace(command)) return;

                command = command.ToLowerInvariant();

                if (command == "skip" || command == "next")
                {
                    await OnCommandReceived.Invoke("skip", null).ConfigureAwait(false);
                }
                else if (command == "pause" || command == "stop")
                {
                    await OnCommandReceived.Invoke("stop", null).ConfigureAwait(false);
                }
                else if (command == "play" || command == "start")
                {
                    await OnCommandReceived.Invoke("play", null).ConfigureAwait(false);
                }
                else if (command == "replay")
                {
                    await OnCommandReceived.Invoke("replay", null).ConfigureAwait(false);
                }
                else if (command == "loopon")
                {
                    await OnCommandReceived.Invoke("loopon", null).ConfigureAwait(false);
                }
                else if (command == "loopoff")
                {
                    await OnCommandReceived.Invoke("loopoff", null).ConfigureAwait(false);
                }
                else if (command == "toggleloop")
                {
                    await OnCommandReceived.Invoke("toggleloop", null).ConfigureAwait(false);
                }
                else if (command == "playlist")
                {
                    await OnCommandReceived.Invoke("playlist", null).ConfigureAwait(false);
                }
                else if (command == "reload")
                {
                    await OnCommandReceived.Invoke("reload", null).ConfigureAwait(false);
                }
                else if (command == "scanmedia")
                {
                    await OnCommandReceived.Invoke("scanmedia", null).ConfigureAwait(false);
                }
                else if (command == "updateplaylist")
                {
                    var payload = JsonConvert.DeserializeObject<dynamic>(message);
                    await OnCommandReceived.Invoke("updateplaylist", payload).ConfigureAwait(false);
                }
            };
            try
            {
                _server.Start();
                var msg = $"WebSocket server started on ws://0.0.0.0:{port}";
                Console.WriteLine(msg);
                FileLogger.Log(msg);
            }
            catch (Exception ex)
            {
                var msg = $"WebSocket server failed to start on port {port}: {ex.Message}";
                Console.WriteLine(msg);
                FileLogger.Log(msg);
                _server = null;
            }
        }

        public void Stop()
        {
            if (_server is null) return;
            try
            {
                _server.Stop();
            }
            catch (InvalidOperationException)
            {
                // Server war nicht gestartet oder bereits gestoppt
            }
            _server.Dispose();
            _server = null;
            _clients.Clear();
        }

        public async Task BroadcastAsync(object payload)
        {
            if (_server == null) return;
            var serialized = JsonConvert.SerializeObject(payload);

            foreach (var client in _server.ListClients())
            {
                await _server.SendAsync(client.Guid, serialized).ConfigureAwait(false);
            }
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
