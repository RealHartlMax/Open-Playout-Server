using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PlayoutServer.Core.Services;

namespace PlayoutServer.Core.Providers
{
    public class ObsProvider : IPlayoutProvider
    {
        public event EventHandler? VideoEnded;

        public Task ConnectAsync(string ip, int port)
        {
            Console.WriteLine($"[OBS] Connect to {ip}:{port} (not implemented)");
            return Task.CompletedTask;
        }

        public Task PlayVideoAsync(string videoPath, int durationSeconds)
        {
            Console.WriteLine($"[OBS] Play video {videoPath} (not implemented)");
            if (durationSeconds > 0)
            {
                Task.Delay(TimeSpan.FromSeconds(durationSeconds)).ContinueWith(_ =>
                {
                    VideoEnded?.Invoke(this, EventArgs.Empty);
                });
            }
            return Task.CompletedTask;
        }

        public Task StopAsync()
        {
            Console.WriteLine("[OBS] Stop (not implemented)");
            return Task.CompletedTask;
        }

        public Task<List<MediaItem>> GetMediaLibraryAsync()
        {
            // OBS provider does not support media scan by now.
            return Task.FromResult(new List<MediaItem>());
        }
    }
}
