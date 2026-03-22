using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PlayoutServer.Core.Models;
using PlayoutServer.Core.Providers;
using PlayoutServer.Core.Services;

namespace PlayoutServer.Core.Controllers
{
    /// <summary>
    /// REST API für PlayoutServer-Befehle
    /// (DE) Steuert Playout, Playlist und Media-Library
    /// (EN) Controls playout, playlist, and media library
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class PlayoutController : ControllerBase
    {
        private static PlaylistManager? _playlistManager;
        private static IPlayoutProvider? _provider;
        private static StreamerbotConnector? _streamerbot;

        // Wird von PlayoutService gesetzt
        public static void SetPlayoutManager(PlaylistManager? playlistManager, IPlayoutProvider? provider, StreamerbotConnector? streamerbot)
        {
            _playlistManager = playlistManager;
            _provider = provider;
            _streamerbot = streamerbot;
        }

        /// <summary>GET /api/playout/status - Liefert Status und aktuelle Playlist</summary>
        [HttpGet("status")]
        public async Task<IActionResult> GetStatus()
        {
            if (_playlistManager == null)
                return Ok(new { status = "initializing", message = "PlayoutServer wird initialisiert" });

            try
            {
                var status = new
                {
                    playlist = _playlistManager.GetPlaylist(),
                    playlistCount = _playlistManager.GetPlaylist().Count,
                    loopEnabled = _playlistManager.IsLoopEnabled(),
                    timestamp = DateTime.Now
                };

                return Ok(status);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>POST /api/playout/play - Startet Playout</summary>
        [HttpPost("play")]
        public async Task<IActionResult> Play()
        {
            if (_playlistManager == null)
                return BadRequest("PlayoutManager nicht initialisiert");

            try
            {
                FileLogger.Log("[REST-API] Play-Befehl empfangen");
                await _playlistManager.StartAsync(System.Threading.CancellationToken.None);
                return Ok(new { message = "Playout gestartet" });
            }
            catch (Exception ex)
            {
                FileLogger.LogError("[REST-API] Play-Fehler", ex);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>POST /api/playout/stop - Stoppt Playout</summary>
        [HttpPost("stop")]
        public async Task<IActionResult> Stop()
        {
            if (_playlistManager == null)
                return BadRequest("PlayoutManager nicht initialisiert");

            try
            {
                FileLogger.Log("[REST-API] Stop-Befehl empfangen");
                await _playlistManager.StopAsync();
                return Ok(new { message = "Playout gestoppt" });
            }
            catch (Exception ex)
            {
                FileLogger.LogError("[REST-API] Stop-Fehler", ex);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>POST /api/playout/skip - Skipt zu nächstem Video</summary>
        [HttpPost("skip")]
        public async Task<IActionResult> Skip()
        {
            if (_playlistManager == null)
                return BadRequest("PlayoutManager nicht initialisiert");

            try
            {
                FileLogger.Log("[REST-API] Skip-Befehl empfangen");
                await _playlistManager.SkipAsync();
                return Ok(new { message = "Zum nächsten Video gesprungen" });
            }
            catch (Exception ex)
            {
                FileLogger.LogError("[REST-API] Skip-Fehler", ex);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>POST /api/playout/replay - Spielt aktuelles Video neu ab</summary>
        [HttpPost("replay")]
        public async Task<IActionResult> Replay()
        {
            if (_playlistManager == null)
                return BadRequest("PlayoutManager nicht initialisiert");

            try
            {
                FileLogger.Log("[REST-API] Replay-Befehl empfangen");
                await _playlistManager.ReplayAsync();
                return Ok(new { message = "Video neugestartet" });
            }
            catch (Exception ex)
            {
                FileLogger.LogError("[REST-API] Replay-Fehler", ex);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>POST /api/playout/loop?enabled=true - Aktiviert/deaktiviert Loop</summary>
        [HttpPost("loop")]
        public IActionResult SetLoop([FromQuery] bool enabled)
        {
            if (_playlistManager == null)
                return BadRequest("PlayoutManager nicht initialisiert");

            _playlistManager.SetLoop(enabled);
            FileLogger.Log($"[REST-API] Loop {(enabled ? "aktiviert" : "deaktiviert")}");
            return Ok(new { loop = enabled });
        }

        /// <summary>GET /api/playout/playlist - Liefert aktuelle Playlist</summary>
        [HttpGet("playlist")]
        public IActionResult GetPlaylist()
        {
            if (_playlistManager == null)
                return BadRequest("PlayoutManager nicht initialisiert");

            return Ok(new { playlist = _playlistManager.GetPlaylist() });
        }

        /// <summary>GET /api/playout/media - Liefert verfügbare Media-Library</summary>
        [HttpGet("media")]
        public async Task<IActionResult> GetMediaLibrary()
        {
            if (_provider == null)
                return BadRequest("Provider nicht initialisiert");

            try
            {
                var media = await _provider.GetMediaLibraryAsync();
                return Ok(new { media = media });
            }
            catch (Exception ex)
            {
                FileLogger.LogWarning($"[REST-API] Media-Library Error: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>GET /api/playout/diagnostics - Liefert Diagnostics-Report</summary>
        [HttpGet("diagnostics")]
        public IActionResult GetDiagnostics()
        {
            var report = DiagnosticsService.GetDiagnosticsReport();
            return Ok(new { diagnostics = report });
        }

        /// <summary>GET /api/playout/health - Health Check</summary>
        [HttpGet("health")]
        public IActionResult Health()
        {
            var healthy = DiagnosticsService.IsServiceHealthy("WebSocketServer") || _streamerbot != null;
            return Ok(new 
            { 
                status = healthy ? "healthy" : "degraded",
                timestamp = DateTime.Now,
                streambotConnected = _streamerbot != null
            });
        }

        /// <summary>POST /api/playout/addmedia - Fügt Media-Item zur Playlist hinzu</summary>
        [HttpPost("addmedia")]
        public IActionResult AddMedia([FromBody] dynamic request)
        {
            if (_playlistManager == null)
                return BadRequest("PlayoutManager nicht initialisiert");

            try
            {
                string? filePath = request?.filePath;
                if (string.IsNullOrEmpty(filePath))
                    return BadRequest("filePath erforderlich");

                var item = new PlaylistItem
                {
                    FilePath = filePath,
                    DurationSeconds = (int?)request?.duration ?? 0,
                    Status = "queued"
                };

                _playlistManager.AddItem(item);
                FileLogger.Log($"[REST-API] Media hinzugefügt: {filePath}");
                return Ok(new { message = "Media hinzugefügt", item = item });
            }
            catch (Exception ex)
            {
                FileLogger.LogError("[REST-API] AddMedia-Fehler", ex);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>DELETE /api/playout/playlist/{index} - Entfernt Item</summary>
        [HttpDelete("playlist/{index}")]
        public IActionResult RemovePlaylistItem(int index)
        {
            if (_playlistManager == null)
                return BadRequest("PlayoutManager nicht initialisiert");

            try
            {
                var playlist = _playlistManager.GetPlaylist();
                if (index < 0 || index >= playlist.Count)
                    return BadRequest("Ungültiger Index");

                var item = playlist[index];
                _playlistManager.RemoveItem(index);
                FileLogger.Log($"[REST-API] Item entfernt: Index {index}");
                return Ok(new { message = "Item entfernt", removed = item });
            }
            catch (Exception ex)
            {
                FileLogger.LogError("[REST-API] RemoveItem-Fehler", ex);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>POST /api/playout/video/{index}/streamerbotconfig - Speichert Streamer.bot-Konfiguration</summary>
        [HttpPost("video/{index}/streamerbotconfig")]
        public IActionResult SaveStreamerbotConfig(int index, [FromBody] dynamic config)
        {
            if (_playlistManager == null)
                return BadRequest("PlayoutManager nicht initialisiert");

            try
            {
                var playlist = _playlistManager.GetPlaylist();
                if (index < 0 || index >= playlist.Count)
                    return BadRequest("Ungültiger Index");

                var item = playlist[index];
                item.SendToStreamerbot = config?.enabled ?? false;
                
                // Speichere Metadaten (Custom-Properties für Streamer.bot)
                if (config?.metadata != null)
                {
                    item.StreamerbotMetadata.Clear();
                    foreach (var prop in config.metadata)
                    {
                        item.StreamerbotMetadata[prop.Name] = prop.Value;
                    }
                }

                item.LastUpdated = DateTime.Now.ToString("O");
                _playlistManager.UpdateItem(index, item);
                
                FileLogger.Log($"[REST-API] Streamer.bot-Config gespeichert für Index {index}");
                return Ok(new { message = "Config gespeichert", item = item });
            }
            catch (Exception ex)
            {
                FileLogger.LogError("[REST-API] SaveStreamerbotConfig-Fehler", ex);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>GET /api/playout/playlist/export - Exportiert Playlist als JSON</summary>
        [HttpGet("playlist/export")]
        public IActionResult ExportPlaylist()
        {
            if (_playlistManager == null)
                return BadRequest("PlayoutManager nicht initialisiert");

            try
            {
                var playlist = _playlistManager.GetPlaylist();
                var fileName = $"playlist_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.json";
                
                FileLogger.Log($"[REST-API] Playlist exportiert: {fileName}");
                return Ok(new 
                { 
                    fileName = fileName,
                    playlist = playlist,
                    exportedAt = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                FileLogger.LogError("[REST-API] ExportPlaylist-Fehler", ex);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>POST /api/playout/playlist/import - Importiert Playlist aus JSON</summary>
        [HttpPost("playlist/import")]
        public IActionResult ImportPlaylist([FromBody] dynamic data)
        {
            if (_playlistManager == null)
                return BadRequest("PlayoutManager nicht initialisiert");

            try
            {
                if (data?.playlist == null)
                    return BadRequest("Playlist-Daten erforderlich");

                var items = new List<PlaylistItem>();
                foreach (var itemData in data.playlist)
                {
                    var item = new PlaylistItem
                    {
                        FilePath = itemData.filePath ?? itemData.FilePath ?? "",
                        DurationSeconds = (int?)itemData.durationSeconds ?? (int?)itemData.DurationSeconds ?? 0,
                        Enabled = (bool?)itemData.enabled ?? (bool?)itemData.Enabled ?? true,
                        Status = itemData.status ?? itemData.Status ?? "queued"
                    };
                    items.Add(item);
                }

                _playlistManager.UpdatePlaylist(items);
                FileLogger.Log($"[REST-API] Playlist importiert: {items.Count} Items");
                return Ok(new { message = "Playlist importiert", itemCount = items.Count });
            }
            catch (Exception ex)
            {
                FileLogger.LogError("[REST-API] ImportPlaylist-Fehler", ex);
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
