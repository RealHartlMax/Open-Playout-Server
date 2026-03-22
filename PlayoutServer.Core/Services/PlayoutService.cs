using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using PlayoutServer.Core.Models;
using PlayoutServer.Core.Providers;

namespace PlayoutServer.Core.Services
{
    /// <summary>
    /// Hintergrunddienst für den Playout-Server.
    /// (DE) Lädt Konfiguration, startet Provider und WebSocket Server.
    /// (EN) Loads config, starts provider and websocket server.
    /// </summary>
    public class PlayoutService : BackgroundService
    {
        private readonly WebSocketServer _wsServer;
        private readonly IHostApplicationLifetime _lifetime;
        private StreamerbotConnector? _streamerbot;

        public PlayoutService(WebSocketServer wsServer, IHostApplicationLifetime lifetime)
        {
            _wsServer = wsServer;
            _lifetime = lifetime;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                var configPath = "appsettings.json";
                if (!File.Exists(configPath))
                {
                    var basePath = AppContext.BaseDirectory;
                    var altPath = Path.Combine(basePath, "appsettings.json");
                    if (File.Exists(altPath))
                    {
                        configPath = altPath;
                    }
                    else
                    {
                        Console.WriteLine($"Konfiguration {configPath} nicht gefunden.");
                        _lifetime.StopApplication();
                        return;
                    }
                }

                var cfgJson = await File.ReadAllTextAsync(configPath).ConfigureAwait(false);
                var config = JsonConvert.DeserializeObject<AppConfig>(cfgJson) ?? new AppConfig();

                FileLogger.Log($"Lade Playlist von {config.PlaylistPath}");
                var playlist = await LoadPlaylistAsync(config.PlaylistPath).ConfigureAwait(false);
                FileLogger.Log($"Gefundene Anzahl Playlisteinträge: {playlist.Count}");
                if (playlist.Count == 0)
                {
                    FileLogger.Log("Playlist ist leer. Bitte Playlist-Datei prüfen.");
                    _lifetime.StopApplication();
                    return;
                }

                IPlayoutProvider provider = config.PlayoutMode.Equals("OBS", StringComparison.OrdinalIgnoreCase)
                    ? new ObsProvider()
                    : new CasparCGProvider();

                await provider.ConnectAsync(config.PlayoutMode.Equals("OBS", StringComparison.OrdinalIgnoreCase) ? config.OBS.IP : config.CasparCG.IP,
                    config.PlayoutMode.Equals("OBS", StringComparison.OrdinalIgnoreCase) ? config.OBS.Port : config.CasparCG.Port).ConfigureAwait(false);

                // WebSocketServer wird NICHT gestartet - nur noch Streamer.bot als Client
                // _wsServer.Start(config.WebSocketPort);
                FileLogger.Log("[PlayoutService] WebSocket Server nicht gestartet - nur Streamer.bot als Client Mode");

                // Initialize Streamer.bot connector if enabled
                if (config.StreamerbotEnabled)
                {
                    try
                    {
                        _streamerbot = new StreamerbotConnector(config.Streamerbot.IP, config.Streamerbot.Port, config.Streamerbot.Password);
                        await _streamerbot.ConnectAsync().ConfigureAwait(false);
                        FileLogger.Log("[PlayoutService] Streamer.bot connector initialized");
                    }
                    catch (Exception ex)
                    {
                        FileLogger.LogError("[PlayoutService] Streamer.bot connection failed", ex);
                    }
                }

                using var playlistManager = new PlaylistManager(provider, _wsServer, playlist);
                playlistManager.SetLoop(config.LoopEnabled);

                // Register PlayoutController mit PlaylistManager für REST API
                Controllers.PlayoutController.SetPlayoutManager(playlistManager, provider, _streamerbot);

                // Initiale UI-Füllung: Playlist und Media laden (noch nicht starten)
                // WS Broadcasts sind optional - wenn kein Server läuft, werden sie ignoriert
                try
                {
                    await _wsServer.BroadcastAsync(new { eventType = "PLAYLIST_UPDATE", playlist = playlistManager.GetPlaylist() }).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    FileLogger.LogDebug($"WS Broadcast ignoriert (Server nicht gestartet): {ex.Message}");
                }

                try
                {
                    var casparMedia = await provider.GetMediaLibraryAsync().ConfigureAwait(false);
                    if (casparMedia.Count > 0)
                    {
                        await _wsServer.BroadcastAsync(new { eventType = "MEDIA_LIBRARY_UPDATE", media = casparMedia }).ConfigureAwait(false);
                    }
                    else if (config.UseLocalMediaScanner)
                    {
                        var mediaScanner = new MediaScanner(config.MediaFolderPath);
                        var initialMedia = mediaScanner.ScanMedia();
                        await _wsServer.BroadcastAsync(new { eventType = "MEDIA_LIBRARY_UPDATE", media = initialMedia }).ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    FileLogger.Log($"MediaLibrary initial load failed from provider: {ex.Message}");
                    if (config.UseLocalMediaScanner)
                    {
                        var mediaScanner = new MediaScanner(config.MediaFolderPath);
                        var initialMedia = mediaScanner.ScanMedia();
                        await _wsServer.BroadcastAsync(new { eventType = "MEDIA_LIBRARY_UPDATE", media = initialMedia }).ConfigureAwait(false);
                    }
                }

                _wsServer.OnCommandReceived += async (cmd, data) =>
                {
                    if (string.IsNullOrWhiteSpace(cmd)) return;

                    switch (cmd.ToLowerInvariant())
                    {
                        case "skip":
                        case "next":
                            await playlistManager.SkipAsync().ConfigureAwait(false);
                            break;
                        case "stop":
                        case "pause":
                            await playlistManager.StopAsync().ConfigureAwait(false);
                            break;
                        case "play":
                        case "start":
                            await playlistManager.StartAsync(stoppingToken).ConfigureAwait(false);
                            break;
                        case "replay":
                            await playlistManager.ReplayAsync().ConfigureAwait(false);
                            break;
                        case "loopon":
                            playlistManager.SetLoop(true);
                            break;
                        case "loopoff":
                            playlistManager.SetLoop(false);
                            break;
                        case "toggleloop":
                            playlistManager.SetLoop(!playlistManager.IsLoopEnabled());
                            break;
                        case "playlist":
                            await _wsServer.BroadcastAsync(new { eventType = "PLAYLIST_UPDATE", playlist = playlistManager.GetPlaylist() }).ConfigureAwait(false);
                            break;
                        case "reload":
                            var newPlaylist = await LoadPlaylistAsync(config.PlaylistPath).ConfigureAwait(false);
                            playlistManager.UpdatePlaylist(newPlaylist);
                            await playlistManager.StartAsync(stoppingToken).ConfigureAwait(false);
                            break;
                        case "updateplaylist":
                            if (data != null)
                            {
                                try
                                {
                                    var payload = data as Newtonsoft.Json.Linq.JObject;
                                    var itemsJson = payload?.GetValue("items")?.ToString() ?? data.ToString();
                                    var items = JsonConvert.DeserializeObject<List<PlaylistItem>>(itemsJson!) ?? new List<PlaylistItem>();
                                    playlistManager.UpdatePlaylist(items);
                                    await playlistManager.StartAsync(stoppingToken).ConfigureAwait(false);
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"[PlaylistService] updateplaylist failed: {ex.Message}");
                                }
                            }
                            break;
                        case "scanmedia":
                            try
                            {
                                var media = await provider.GetMediaLibraryAsync().ConfigureAwait(false);
                                if (media.Count == 0 && config.UseLocalMediaScanner)
                                {
                                    var mediaScanner = new MediaScanner(config.MediaFolderPath);
                                    media = mediaScanner.ScanMedia();
                                }
                                await _wsServer.BroadcastAsync(new { eventType = "MEDIA_LIBRARY_UPDATE", media }).ConfigureAwait(false);
                            }
                            catch (Exception ex)
                            {
                                FileLogger.Log($"scanmedia failed: {ex.Message}");
                                if (config.UseLocalMediaScanner)
                                {
                                    var mediaScanner = new MediaScanner(config.MediaFolderPath);
                                    var media = mediaScanner.ScanMedia();
                                    await _wsServer.BroadcastAsync(new { eventType = "MEDIA_LIBRARY_UPDATE", media }).ConfigureAwait(false);
                                }
                            }
                            break;
                    }
                };

                // Nicht automatisch starten; Benutzer kann per Play-Taste starten
                await Task.Delay(Timeout.Infinite, stoppingToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                FileLogger.Log($"Fehler: {ex}");
                _streamerbot?.Dispose();
                _lifetime.StopApplication();
            }
        }

        private static async Task<List<PlaylistItem>> LoadPlaylistAsync(string path)
        {
            if (!File.Exists(path))
            {
                var altPath = Path.Combine(AppContext.BaseDirectory, path);
                if (File.Exists(altPath))
                {
                    path = altPath;
                }
                else
                {
                    Console.WriteLine($"Playlist-Datei {path} nicht gefunden.");
                    return new List<PlaylistItem>();
                }
            }

            var json = await File.ReadAllTextAsync(path).ConfigureAwait(false);
            try
            {
                return JsonConvert.DeserializeObject<List<PlaylistItem>>(json) ?? new List<PlaylistItem>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fehler beim Laden der Playlist: {ex.Message}");
                return new List<PlaylistItem>();
            }
        }
    }
}