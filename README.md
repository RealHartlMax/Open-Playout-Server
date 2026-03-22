(WIP) - Dieses Projekt ist noch in Entwicklung. Verbesserungsvorschläge sind herzlich willkommen, und Mitarbeit ist ausdrücklich erwünscht!

## Setup / Einrichtung (DE) / Setup (EN)

### Schnellstart / Quick Start
- **Windows**: Doppelklick auf `start_core.bat` (für Server) oder `start_gui.bat` (für GUI).  
  - (EN) Double-click `start_core.bat` (for server) or `start_gui.bat` (for GUI).
- **Linux/Mac**: `./start_core.sh` (für Server) oder `./start_gui.sh` (für GUI).  
  - (EN) Run `./start_core.sh` (for server) or `./start_gui.sh` (for GUI).  
  - (DE) Stelle sicher, dass die Scripts ausführbar sind: `chmod +x start_core.sh start_gui.sh`.

### Manuell / Manual
1. `dotnet restore`  
   - (DE) Abhängigkeiten wiederherstellen.  
   - (EN) Restore dependencies.
2. `dotnet build`  
   - (DE) Projekt kompilieren.  
   - (EN) Build the project.
3. Web-App starten: `dotnet run --project PlayoutServer.Core\PlayoutServer.Core.csproj`  
   - (DE) Startet die Web-Oberfläche.  
   - (EN) Starts the web interface.
4. Desktop-GUI starten: `dotnet run --project PlayoutServer.GUI\PlayoutServer.GUI.csproj`  
   - (DE) Startet die WPF-Desktopanwendung.  
   - (EN) Starts the WPF desktop application.

## Projektstruktur / Project Structure

- `PlayoutServer.Core\` - ASP.NET Core Web-App mit Playout-Logik und Web-GUI  
  - (EN) ASP.NET Core web app with playout logic and web UI
- `PlayoutServer.GUI\` - WPF Desktop-GUI  
  - (EN) WPF desktop UI
- `Providers/` - Anbieter-Implementierungen (CasparCG, OBS)  
  - (EN) provider implementations (CasparCG, OBS)
- `Models/` - Konfiguration und Playlist-Modelle  
  - (EN) configuration and playlist models
- `Services/` - Playlist-Manager und WebSocket-Server  
  - (EN) playlist manager and websocket server

## GUIs / Benutzeroberflächen

### Web-GUI
- Zugriff: `http://localhost:5000`  
  - (DE) Browser-basierte Oberfläche  
  - (EN) Browser-based interface
- Features: Live-Dashboard mit WebSocket-Events  
  - (DE) Live-Status, Playlist-Updates, Wiedergabe-Steuerung  
  - (EN) live status, playlist updates, playback control

### Desktop-GUI (WPF)
- Lokale App für Windows  
  - (EN) local Windows app
- Features: Status-Anzeige, Event-Liste, WebSocket-Verbindung  
  - (EN) status display, event log, websocket connection
- Einstellungen: Menü → "Einstellungen" → Verbindungsdetails (Host/Port)  
  - (EN) Settings: Menu → "Settings" → Connection details (host/port)

## Playlist Editor / Playlist-Bearbeitung

Der Playlist Editor ermöglicht es, die Reihenfolge der Videos in der Playlist interaktiv zu ändern.  
(EN) The Playlist Editor allows you to interactively reorder videos in the playlist.

### Web-GUI Playlist Editor
- **Zugriff**: Klicken Sie auf "Playlist Editor" Button in der Hauptanwendung oder öffnen Sie: `http://localhost:5000/playout/editor`  
  - (EN) Access: Click "Playlist Editor" button in main app or open: `http://localhost:5000/playout/editor`
- **Features**:
  - Ziehen und ablegen (Drag & Drop) zur Umordnung
  - (EN) Drag & drop for reordering
  - Hoch / Runter Buttons zur manuellen Bewegung
  - (EN) Up / Down buttons for manual movement
  - Löschen Button zum Entfernen von Videos
  - (EN) Delete button to remove videos
  - Speichern Button zum Aktualisieren der Playlist
  - (EN) Save button to update the playlist

### Desktop-GUI Playlist Editor (WPF)
- **Zugriff**: "Playlist Editor" Menu in der WPF-Anwendung  
  - (EN) Access: "Playlist Editor" menu in WPF app
- **Funktionen**: Identisch zur Web-Variante mit systemeigener Windows-Integration
  - (EN) Features: Identical to web version with native Windows integration


## Konfiguration / Configuration

### appsettings.json

```json
{
  "PlayoutMode": "CasparCG",  // oder "OBS"
  "CasparCG": {
    "IP": "127.0.0.1",
    "Port": 5250
  },
  "OBS": {
    "IP": "127.0.0.1",
    "Port": 4455,
    "Password": ""
  },
  "PlaylistPath": "playlist.json",
  "WebSocketPort": 8080,
  "LoopEnabled": true,
  "UseLocalMediaScanner": false,
  "StreamerbotEnabled": false,
  "Streamerbot": {
    "IP": "127.0.0.1",
    "Port": 8080,
    "Password": ""
  }
}
```

### Streamer.bot Integration

**Aktivierung:**
- (DE) `"StreamerbotEnabled": true` in `appsettings.json`
- (EN) Set `"StreamerbotEnabled": true` in `appsettings.json`

**Authentifizierung (optional):**
- Wenn Streamer.bot mit Passwort gesichert ist, setzen Sie das Passwort in `Streamerbot.Password`
- (EN) If Streamer.bot is password-protected, set the password in `Streamerbot.Password`

**WebSocket Server in Streamer.bot aktivieren:**
1. Streamer.bot → Settings → WebSocket
2. Enable WebSocket Server
3. Port: (default 8080)
4. Authentication: optional (empfohlen)

**Verwendung:**
- PlayoutServer.Core verbindet sich automatisch mit Streamer.bot
- Streamer.bot-Events werden in Logs protokolliert
- (EN) PlayoutServer.Core automatically connects to Streamer.bot
- (EN) Streamer.bot events are logged

## Verbesserungen (MVP) / Improvements (MVP)

- ✅ CasparCG Media Library via `LS` AMCP Command  
  - (DE) direkt aus CasparCG-Server, keine lokalen Datei-Scans  
  - (EN) directly from CasparCG server, no local file scans
- ✅ Streamer.bot WebSocket Integration  
  - (DE) Zwei-Wege-Kommunikation mit SHA256-basierter Authentifizierung  
  - (EN) two-way communication with SHA256-based authentication
- ✅ GUI Settings Panel  
  - (DE) Host/Port-Einstellungen im WPF-Menü  
  - (EN) Host/port settings in WPF menu
- ✅ Separate Log-Dateien  
  - (DE) `Logs/Log_core` und `Logs/Log_gui` mit Timestamp pro Session  
  - (EN) `Logs/Log_core` and `Logs/Log_gui` with per-session timestamps
- ✅ REST API Endpoints  
  - (DE) Vollständige HTTP-basierte API für Playout-Steuerung und Status-Abfragen  
  - (EN) Complete HTTP-based API for playback control and status queries
- ✅ Playlist Editor mit Drag & Drop  
  - (DE) Web-GUI und WPF-Desktop-Anwendung zum Neuordnen der Playlist  
  - (EN) web GUI and WPF desktop app for reordering playlist
  - Features: Ziehen und Ablegen, Hoch-/Runter-Buttons, Löschen per Video
  - (EN) Features: drag & drop, up/down buttons, delete per video
- CasparCGProvider: Asynchrone Antwort-Verarbeitung via separaten Read-Task  
  - (EN) asynchronous response handling via a separate read task
- Duration-basierte VideoEnded-Triggerung (Timer nach PLAY)  
  - (EN) duration-based video end trigger (timer after PLAY)
- OBS-Provider-Implementierung (advanced)  
  - (EN) OBS provider implementation (advanced)
- FFmpeg-Integration für genaue Video-Dauer  
  - (EN) FFmpeg integration for accurate video duration

- WebSocket Broadcast für VIDEO_STARTED / VIDEO_ENDED  
  - (EN) websocket broadcast for VIDEO_STARTED / VIDEO_ENDED
- Playlist-Loop mit zyklischem Abspielen  
  - (EN) playlist loop with cyclic playback
- Web-GUI und Desktop-GUI  
  - (EN) web GUI and desktop GUI

## Publish für Linux / Publish for Linux

`dotnet publish PlayoutServer.Core\PlayoutServer.Core.csproj -c Release -r linux-x64 --self-contained true /p:PublishSingleFile=true`  
- (EN) publish as single-file self-contained Linux binary

## Konfiguration / Configuration

- `PlayoutServer.Core\appsettings.json` kann den Modus und Verbindungsdaten wählen  
  - (EN) can choose mode and connection settings
- `PlayoutServer.Core\playlist.json` beinhaltet Song/Video-Liste  
  - (EN) contains the playlist of songs/videos
- `MediaFolderPath`: Pfad zum CasparCG Media-Ordner (z.B. `C:\CasparCG\media`)  
  - (EN) Path to CasparCG media folder (e.g. `C:\CasparCG\media`)

## Ton / Audio

- Videos werden standardmäßig mit Ton abgespielt. Falls kein Ton hörbar ist, prüfen Sie die CasparCG-Konfiguration (Audio-Channel aktiviert?) oder den Server-Status.  
  - (EN) Videos are played with audio by default. If no audio is heard, check CasparCG config (audio channel enabled?) or server status.

## ToDo / Aufgabenliste

Dieses Projekt ist noch in aktiver Entwicklung. Hier eine Liste geplanter Verbesserungen und Features:

### Geplante Features / Planned Features
- **Detaillierte Video-Duration**: Integration von FFmpeg für exakte Laufzeit-Berechnung anstatt Schätzung.  
  - (EN) Integrate FFmpeg for accurate video duration instead of estimation.
- **Persistenz von Änderungen**: Automatisches Speichern der Playlist-Änderungen in `playlist.json`.  
  - (EN) Auto-save playlist changes to `playlist.json`.
- **Mehr Provider**: Unterstützung für VLC, FFmpeg oder andere Playout-Systeme.  
  - (EN) Support for VLC, FFmpeg, or other playout systems.
- **Tests hinzufügen**: Unit- und Integrationstests für Stabilität.  
  - (EN) Add unit and integration tests for stability.
- **Erweiterte Media-Library**: Filter, Suche und Vorschaubilder.  
  - (EN) Advanced media library with filters, search, and thumbnails.
- **Logging und Monitoring**: Verbesserte Logs und Health-Checks.  
  - (EN) Improved logging and health monitoring.

### Bekannte Probleme / Known Issues
- Ton-Ausgabe abhängig von CasparCG-Server-Konfiguration.  
  - (EN) Audio output depends on CasparCG server configuration.
- Duration-Schätzung ungenau für variable Bitraten.  
  - (EN) Duration estimation inaccurate for variable bitrates.

Verbesserungsvorschläge und Pull-Requests sind herzlich willkommen!  
Improvement suggestions and pull requests are very welcome!

