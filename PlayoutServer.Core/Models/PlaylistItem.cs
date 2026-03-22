using System;
using System.Collections.Generic;

namespace PlayoutServer.Core.Models
{
    public class PlaylistItem
    {
        public string FilePath { get; set; } = string.Empty;
        public int DurationSeconds { get; set; }
        public bool Enabled { get; set; } = true;
        public string Status { get; set; } = "queued";
        
        // Neue Felder für erweiterte Funktionalität
        public string? ThumbnailPath { get; set; }
        public Dictionary<string, object> StreamerbotMetadata { get; set; } = new();
        public bool SendToStreamerbot { get; set; } = false;
        public string? LastUpdated { get; set; }

        public string FileName => System.IO.Path.GetFileName(FilePath);
        public string MediaName => System.IO.Path.GetFileNameWithoutExtension(FilePath);
    }
}
