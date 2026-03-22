using System;

namespace PlayoutServer.Core.Models
{
    public class PlaylistItem
    {
        public string FilePath { get; set; } = string.Empty;
        public int DurationSeconds { get; set; }
        public bool Enabled { get; set; } = true;
        public string Status { get; set; } = "queued";

        public string FileName => System.IO.Path.GetFileName(FilePath);
        public string MediaName => System.IO.Path.GetFileNameWithoutExtension(FilePath);
    }
}
