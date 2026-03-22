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

## Verbesserungen (MVP) / Improvements (MVP)

- CasparCGProvider: Asynchrone Antwort-Verarbeitung via separaten Read-Task  
  - (EN) asynchronous response handling via a separate read task
- Duration-basierte VideoEnded-Triggerung (Timer nach PLAY)  
  - (EN) duration-based video end trigger (timer after PLAY)
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
- **Drag-and-Drop für Playlist**: Interaktive Reihenfolge-Änderung in der Web-UI.  
  - (EN) Drag-and-drop for playlist reordering in web UI.
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

