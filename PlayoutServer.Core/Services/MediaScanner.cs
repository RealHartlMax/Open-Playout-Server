using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PlayoutServer.Core.Models;

namespace PlayoutServer.Core.Services
{
    /// <summary>
    /// Media-Scanner für CasparCG Media-Ordner.
    /// (DE) Scannt den Media-Ordner nach Video-Dateien und extrahiert Metadaten.
    /// (EN) Scans the media folder for video files and extracts metadata.
    /// </summary>
    public class MediaScanner
    {
        private readonly string _mediaFolderPath;

        public MediaScanner(string mediaFolderPath)
        {
            _mediaFolderPath = mediaFolderPath;
        }

        /// <summary>
        /// Scannt den Ordner nach unterstützten Video-Dateien.
        /// (DE) Gibt Liste von MediaItems zurück.
        /// (EN) Returns list of media items.
        /// </summary>
        public List<MediaItem> ScanMedia()
        {
            if (!Directory.Exists(_mediaFolderPath))
            {
                Console.WriteLine($"[MediaScanner] Media-Ordner nicht gefunden: {_mediaFolderPath}");
                return new List<MediaItem>();
            }

            var extensions = new[] { ".mp4", ".mov", ".avi", ".mkv", ".flv", ".wmv" };
            var files = Directory.GetFiles(_mediaFolderPath, "*.*", SearchOption.AllDirectories)
                .Where(f => extensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                .ToList();

            var mediaItems = new List<MediaItem>();
            foreach (var file in files)
            {
                try
                {
                    var fi = new FileInfo(file);
                    var mediaItem = new MediaItem
                    {
                        FilePath = file,
                        FileName = Path.GetFileName(file),
                        MediaName = Path.GetFileNameWithoutExtension(file),
                        SizeBytes = fi.Length,
                        LastModified = fi.LastWriteTime,
                        // Duration estimation (rough, could be improved with FFmpeg)
                        DurationSeconds = EstimateDuration(fi.Length)
                    };
                    mediaItems.Add(mediaItem);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[MediaScanner] Fehler bei {file}: {ex.Message}");
                }
            }

            Console.WriteLine($"[MediaScanner] {mediaItems.Count} Medien gefunden.");
            return mediaItems;
        }

        private int EstimateDuration(long sizeBytes)
        {
            // Rough estimation: assume 50MB/s bitrate for video
            const long bytesPerSecond = 50 * 1024 * 1024; // 50 MB/s
            return (int)(sizeBytes / bytesPerSecond);
        }
    }

    /// <summary>
    /// Repräsentiert ein Media-Item aus dem Scanner.
    /// (DE) Enthält Metadaten für die Media-Library.
    /// (EN) Contains metadata for the media library.
    /// </summary>
    public class MediaItem
    {
        public string FilePath { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string MediaName { get; set; } = string.Empty;
        public long SizeBytes { get; set; }
        public DateTime LastModified { get; set; }
        public int DurationSeconds { get; set; }

        public string SizeFormatted => $"{SizeBytes / (1024 * 1024)} MB";
        public string DurationFormatted => $"{DurationSeconds / 60}:{(DurationSeconds % 60):D2}";
    }
}