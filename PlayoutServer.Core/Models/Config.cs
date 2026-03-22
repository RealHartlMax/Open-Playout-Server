namespace PlayoutServer.Core.Models
{
    public class PlayoutProviderConfig
    {
        public string IP { get; set; } = "127.0.0.1";
        public int Port { get; set; } = 5250;
        public string Password { get; set; } = string.Empty;
    }

    public class AppConfig
    {
        public string PlayoutMode { get; set; } = "CasparCG";
        public PlayoutProviderConfig CasparCG { get; set; } = new PlayoutProviderConfig();
        public PlayoutProviderConfig OBS { get; set; } = new PlayoutProviderConfig();
        public string PlaylistPath { get; set; } = "playlist.json";
        public int WebSocketPort { get; set; } = 8080;
        public bool LoopEnabled { get; set; } = true;
        public int DefaultFrameRate { get; set; } = 25;
        public bool UseLocalMediaScanner { get; set; } = false;
        public string MediaFolderPath { get; set; } = @"C:\CasparCG\media"; // (DE) Pfad zum CasparCG Media-Ordner / (EN) Path to CasparCG media folder
        public bool StreamerbotEnabled { get; set; } = false;
        public PlayoutProviderConfig Streamerbot { get; set; } = new PlayoutProviderConfig { IP = "127.0.0.1", Port = 8080 };
    }
}
