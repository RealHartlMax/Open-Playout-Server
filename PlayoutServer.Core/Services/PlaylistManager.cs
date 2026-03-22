using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PlayoutServer.Core.Models;
using PlayoutServer.Core.Providers;
using PlayoutServer.Core.Services;

namespace PlayoutServer.Core.Services
{
    /// <summary>
    /// Playlist-Manager steuert die Wiedergabe, Loop-Funktion und WebSocket-Events.
    /// (DE) Verwalten der Playlist und Steuerung:
    /// - Start, Stop, Skip, Replay
    /// - Loop on/off
    /// - Spielstatus-Updates via WebSocket an die UI
    /// (EN) Manage playlist and playback:
    /// - start, stop, skip, replay
    /// - loop on/off
    /// - send status updates via WebSocket to UI
    /// </summary>
    public class PlaylistManager : IDisposable
    {
        // Provider für die Wiedergabe (z.B. CasparCG, OBS)
        // provider for playback (e.g. CasparCG, OBS)
        private readonly IPlayoutProvider _provider;
        private readonly WebSocketServer _wsServer;
        private readonly List<PlaylistItem> _playlist;
        private int _currentIndex = -1;
        private bool _running;
        private bool _loopEnabled;
        private DateTime? _currentStartTime;
        private double _currentDurationSeconds;
        private CancellationTokenSource? _progressCts;
        private readonly SemaphoreSlim _lock = new(1, 1);

        public PlaylistManager(IPlayoutProvider provider, WebSocketServer wsServer, IReadOnlyList<PlaylistItem> playlist)
        {
            _provider = provider;
            _wsServer = wsServer;
            _playlist = playlist.ToList();
            _provider.VideoEnded += OnVideoEnded;
            _loopEnabled = true;
        }

        /// <summary>
        /// Playlist vollständig neu setzen.
        /// (DE) Alle Items ersetzen, Startindex zurücksetzen.
        /// (EN) Replace all items, reset current index.
        /// </summary>
        public void UpdatePlaylist(IEnumerable<PlaylistItem> songs)
        {
            lock (_lock)
            {
                _playlist.Clear();
                _playlist.AddRange(songs.Select(x => new PlaylistItem
                {
                    FilePath = x.FilePath,
                    DurationSeconds = x.DurationSeconds,
                    Enabled = x.Enabled,
                    Status = "queued"
                }));
                _currentIndex = -1;
            }

            BroadcastPlaylistAsync().ConfigureAwait(false);
        }

        public void SetLoop(bool enable)
        {
            _loopEnabled = enable;
            _wsServer.BroadcastAsync(new { eventType = "LOOP_STATE", enabled = enable }).ConfigureAwait(false);
        }

        public bool IsLoopEnabled() => _loopEnabled;

        public List<PlaylistItem> GetPlaylist() => _playlist.ToList();

        public async Task StartAsync(CancellationToken token = default)
        {
            if (_running || _playlist.Count == 0) return;
            _running = true;
            FileLogger.Log("Playlist start requested");
            await BroadcastPlaylistAsync().ConfigureAwait(false);
            await PlayNextAsync(token).ConfigureAwait(false);
        }

        public async Task StopAsync()
        {
            if (!_running) return;
            _running = false;
            FileLogger.Log("Playlist stop requested");
            StopProgressLoop();
            await _provider.StopAsync().ConfigureAwait(false);
            await _wsServer.BroadcastAsync(new { eventType = "PLAYBACK_STOPPED" }).ConfigureAwait(false);
        }

        public async Task SkipAsync()
        {
            if (!_running) return;
            await _provider.StopAsync().ConfigureAwait(false);
            StopProgressLoop();
            await PlayNextAsync().ConfigureAwait(false);
        }

        public async Task ReplayAsync()
        {
            if (!_running || _currentIndex < 0) return;
            var current = _playlist[_currentIndex];
            await _provider.StopAsync().ConfigureAwait(false);
            await _provider.PlayVideoAsync(current.FilePath, current.DurationSeconds).ConfigureAwait(false);
            await _wsServer.BroadcastAsync(new { eventType = "VIDEO_STARTED", file = current.FileName }).ConfigureAwait(false);
        }

        private async void OnVideoEnded(object? sender, EventArgs e)
        {
            if (_currentIndex >= 0 && _currentIndex < _playlist.Count)
            {
                var current = _playlist[_currentIndex];
                current.Status = "completed";
                await _wsServer.BroadcastAsync(new { eventType = "VIDEO_ENDED", file = current.FileName }).ConfigureAwait(false);
                await BroadcastPlaylistAsync().ConfigureAwait(false);
            }

            await PlayNextAsync().ConfigureAwait(false);
        }

        private async Task PlayNextAsync(CancellationToken token = default)
        {
            if (!_running || _playlist.Count == 0) return;

            await _lock.WaitAsync(token).ConfigureAwait(false);
            try
            {
                if (_currentIndex >= 0 && _currentIndex < _playlist.Count)
                {
                    _playlist[_currentIndex].Status = "completed";
                }

                if (!_loopEnabled && _currentIndex + 1 >= _playlist.Count)
                {
                    Console.WriteLine("[PlaylistManager] End of playlist reached (no loop). Stop.");
                    _running = false;
                    await _wsServer.BroadcastAsync(new { eventType = "PLAYBACK_COMPLETED" }).ConfigureAwait(false);
                    return;
                }

                var searchIndex = _currentIndex;
                PlaylistItem? next = null;

                for (int i = 1; i <= _playlist.Count; i++)
                {
                    int candidate = (searchIndex + i) % _playlist.Count;
                    if (_playlist[candidate].Enabled)
                    {
                        next = _playlist[candidate];
                        _currentIndex = candidate;
                        break;
                    }
                }

                if (next == null)
                {
                    Console.WriteLine("[PlaylistManager] No enabled playlist item found.");
                    _running = false;
                    await _wsServer.BroadcastAsync(new { eventType = "PLAYBACK_COMPLETED" }).ConfigureAwait(false);
                    return;
                }

                next.Status = "playing";
                _currentDurationSeconds = next.DurationSeconds;
                _currentStartTime = DateTime.UtcNow;
                StartProgressLoop();

                Console.WriteLine($"[PlaylistManager] Playing: {next.FileName} ({next.DurationSeconds}s)");
                await BroadcastPlaylistAsync().ConfigureAwait(false);
                await _wsServer.BroadcastAsync(new { eventType = "VIDEO_STARTED", file = next.FileName, duration = next.DurationSeconds, enabled = next.Enabled, status = next.Status, startedAt = _currentStartTime }).ConfigureAwait(false);
                await _provider.PlayVideoAsync(next.FilePath, next.DurationSeconds).ConfigureAwait(false);
            }
            catch (TaskCanceledException)
            {
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PlaylistManager] Fehler: {ex.Message}");

                if (_currentIndex >= 0 && _currentIndex < _playlist.Count)
                {
                    _playlist[_currentIndex].Status = "error";
                    await _wsServer.BroadcastAsync(new { eventType = "VIDEO_ERROR", file = _playlist[_currentIndex].FileName, error = ex.Message }).ConfigureAwait(false);
                    await BroadcastPlaylistAsync().ConfigureAwait(false);
                }

                await Task.Delay(TimeSpan.FromSeconds(5), token).ConfigureAwait(false);
                if (_running && !token.IsCancellationRequested)
                {
                    _currentIndex = (_currentIndex + 1) % _playlist.Count; // überspringe das fehlerhafte Item
                    await PlayNextAsync(token).ConfigureAwait(false);
                }
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <summary>
        /// Startet intern einen Timer, der regelmäßig Fortschritts-Daten an neue UI sendet.
        /// (DE) Fortlaufender Progress-Update-Event (VIDEO_PROGRESS).
        /// (EN) Polling progress and sending VIDEO_PROGRESS events.
        /// </summary>
        private void StartProgressLoop()
        {
            StopProgressLoop();
            _progressCts = new CancellationTokenSource();
            var token = _progressCts.Token;

            _ = Task.Run(async () =>
            {
                while (!token.IsCancellationRequested && _running && _currentStartTime.HasValue)
                {
                    var elapsed = (DateTime.UtcNow - _currentStartTime.Value).TotalMilliseconds;
                    var remaining = Math.Max(0, _currentDurationSeconds * 1000 - elapsed);

                    await _wsServer.BroadcastAsync(new
                    {
                        eventType = "VIDEO_PROGRESS",
                        elapsedMs = Math.Min((long)elapsed, (long)(_currentDurationSeconds * 1000)),
                        remainingMs = (long)remaining,
                        durationMs = (long)(_currentDurationSeconds * 1000)
                    }).ConfigureAwait(false);

                    if (remaining <= 0)
                    {
                        break;
                    }

                    await Task.Delay(250, token).ConfigureAwait(false);
                }
            }, token);
        }

        private void StopProgressLoop()
        {
            try
            {
                if (_progressCts != null && !_progressCts.IsCancellationRequested)
                {
                    _progressCts.Cancel();
                }
            }
            catch { }
            finally
            {
                _progressCts?.Dispose();
                _progressCts = null;
            }
        }

        private Task BroadcastPlaylistAsync()
        {
            return _wsServer.BroadcastAsync(new
            {
                eventType = "PLAYLIST_UPDATE",
                playlist = _playlist
            });
        }

        public void Dispose()
        {
            StopProgressLoop();
            _provider.VideoEnded -= OnVideoEnded;
            _lock.Dispose();
        }
    }
}
