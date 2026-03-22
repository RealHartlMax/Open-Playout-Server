using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PlayoutServer.Core.Services;

namespace PlayoutServer.Core.Providers
{
    public class CasparCGProvider : IPlayoutProvider, IDisposable
    {
        private TcpClient? _client;
        private NetworkStream? _stream;

        public event EventHandler? VideoEnded;

        public async Task ConnectAsync(string ip, int port)
        {
            _client = new TcpClient();
            await _client.ConnectAsync(ip, port).ConfigureAwait(false);
            _stream = _client.GetStream();
            Console.WriteLine($"[CasparCG] Verbunden mit {ip}:{port}");
        }

        public async Task PlayVideoAsync(string videoPath, int durationSeconds)
        {
            if (_stream == null) throw new InvalidOperationException("Nicht mit CasparCG verbunden.");

            var mediaName = Path.GetFileNameWithoutExtension(videoPath);
            // PLAY-Befehl mit Ton (Standard)
            var command = $"PLAY 1-1 \"{mediaName}\"\r\n";
            var buffer = Encoding.UTF8.GetBytes(command);
            await _stream.WriteAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
            await _stream.FlushAsync().ConfigureAwait(false);

            FileLogger.Log($"[CasparCG] Play-Befehl gesendet: {mediaName}");

            var response = await ReadLineAsync().ConfigureAwait(false);
            FileLogger.Log($"[CasparCG] Antwort: {response}");

            if (response == null)
            {
                throw new InvalidOperationException("Kein Antwort-Frame von CasparCG.");
            }

            if (response.Contains("404") || response.Contains("504") || response.Contains("PLAY FAILED"))
            {
                throw new InvalidOperationException($"CasparCG PLAY failed: {response}");
            }

            if (durationSeconds > 0)
            {
                _ = Task.Delay(TimeSpan.FromSeconds(durationSeconds)).ContinueWith(_ =>
                {
                    VideoEnded?.Invoke(this, EventArgs.Empty);
                }, TaskScheduler.Default);
            }
        }


        public async Task StopAsync()
        {
            if (_stream == null) return;
            var command = "STOP 1-1\r\n";
            var buffer = Encoding.UTF8.GetBytes(command);
            await _stream.WriteAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
            await _stream.FlushAsync().ConfigureAwait(false);
            FileLogger.Log("[CasparCG] Stop-Befehl gesendet");
        }

        public async Task<List<MediaItem>> GetMediaLibraryAsync()
        {
            if (_stream == null) throw new InvalidOperationException("Nicht mit CasparCG verbunden.");

            var command = "LS\r\n";
            var buffer = Encoding.UTF8.GetBytes(command);
            await _stream.WriteAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
            await _stream.FlushAsync().ConfigureAwait(false);

            var responseLines = await ReadLinesUntilOkAsync().ConfigureAwait(false);
            var list = new List<MediaItem>();

            foreach (var line in responseLines)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;

                // Beispiel: "VIDEO    sample.mp4" oder "AUDIO    sound.mp3" oder "CG    logo.png"
                var parts = trimmed.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 2) continue;

                var fileName = parts[1];
                list.Add(new MediaItem
                {
                    FilePath = fileName,
                    FileName = fileName,
                    MediaName = Path.GetFileNameWithoutExtension(fileName),
                    SizeBytes = 0,
                    LastModified = DateTime.MinValue,
                    DurationSeconds = 0
                });
            }

            FileLogger.Log($"[CasparCG] LS report returned {list.Count} entries");
            return list;
        }

        private async Task<string?> ReadLineAsync()
        {
            if (_stream == null) return null;

            var buffer = new byte[1024];
            var sb = new StringBuilder();

            while (true)
            {
                var bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
                if (bytesRead == 0) break;

                sb.Append(Encoding.UTF8.GetString(buffer, 0, bytesRead));

                var text = sb.ToString();
                var idx = text.IndexOf("\r\n", StringComparison.Ordinal);
                if (idx >= 0)
                {
                    var line = text.Substring(0, idx);
                    return line;
                }
            }

            return sb.Length == 0 ? null : sb.ToString();
        }

        private async Task<List<string>> ReadLinesUntilOkAsync()
        {
            var lines = new List<string>();

            while (true)
            {
                var line = await ReadLineAsync().ConfigureAwait(false);
                if (line == null) break;

                if (line.StartsWith("200", StringComparison.Ordinal) || line.StartsWith("201", StringComparison.Ordinal))
                {
                    break;
                }

                if (line.StartsWith("400", StringComparison.Ordinal) || line.StartsWith("404", StringComparison.Ordinal) || line.StartsWith("504", StringComparison.Ordinal))
                {
                    break;
                }

                lines.Add(line);
            }

            return lines;
        }

        public void Dispose()
        {
            _stream?.Dispose();
            _client?.Dispose();
        }
    }
}

