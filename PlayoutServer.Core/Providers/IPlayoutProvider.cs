using System;
using System.Threading.Tasks;

using System.Collections.Generic;
using System.Threading.Tasks;
using PlayoutServer.Core.Services;

namespace PlayoutServer.Core.Providers
{
    public interface IPlayoutProvider
    {
        Task ConnectAsync(string ip, int port);
        Task PlayVideoAsync(string videoPath, int durationSeconds);
        Task StopAsync();
        Task<List<MediaItem>> GetMediaLibraryAsync();
        event EventHandler VideoEnded;
    }
}
