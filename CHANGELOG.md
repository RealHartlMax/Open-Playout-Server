# Changelog

Alle bemerkenswerten Änderungen an diesem Projekt werden in dieser Datei dokumentiert.

Das Format basiert auf [Keep a Changelog](https://keepachangelog.com/de/1.0.0/),
und dieses Projekt folgt der [Semantic Versioning](https://semver.org/lang/de/).

---

## [0.0.1c] - 2026-03-22

**Playlist-Editor mit erweiterten Funktionen, Export/Import und Streamer.bot-Metadaten**

### Added
- **Erweiterte Playlist-Editor (Web-GUI)**
  - Professionelle Toolbar mit: Exportieren, Importieren, Video hinzufügen, Entfernen
  - Metadaten-Anzeige pro Video: Dateiname, Dauer (mm:ss), Status
  - Drag & Drop für Playlist-Reordering
  - Responsive Grid-Layout für Media-Auswahl
  - Status-Feedback für Benutzer-Aktionen

- **Export/Import-Funktionalen für Playlists**
  - `GET /api/playout/exportplaylist?filename=custom.json` - Playlist als JSON mit custom Dateiname exportieren
  - `POST /api/playout/importplaylist` - JSON-Datei hochladen und Playlist aktualisieren
  - JavaScript-basierter File-Handling (Blob-Download, FileReader-Upload)
  - Benutzerdefinierte Dateinamen möglich (z.B. "MyPlaylist_2026-03-22.json")
  - Automatische Validierung von Import-Dateien

- **Streamer.bot Metadaten-System (pro Video)**
  - `POST /api/playout/video/{index}/streamerbotconfig` - Speichern von Streamer.bot-Konfiguration
  - Configuration Modal im Editor für Metadaten-Eingabe
  - StreamerbotConfig-Klasse mit: `SendToStreamerbot` (Bool), `MetadataToSend` (Dictionary)
  - Optionale Felder: Title, Duration, Filename, CustomData

- **Neue REST API Endpoints**
  - `POST /api/playout/addmedia?filePath=media.mp4` - Video zur Playlist hinzufügen
  - `POST /api/playout/deleteitem/{index}` - Video an Index entfernen
  - `GET /api/playout/medialib` - Verfügbare Videos abrufen

- **PlaylistItem-Modell erweitert**
  - `StreamerbotConfig` Objekt mit Metadaten
  - `MediaName` Property (formatierter Dateiname)
  - Optional: `Thumbnail` URL für Video-Vorschau
  - Persistierbar in Export-JSON

- **JavaScript Event-Handler**
  - `exportPlaylist()` - Triggert File-Download mit Prompt für Dateiname
  - `importPlaylist(event)` - Liest JSON-Datei ein, parsed und aktualisiert Playlist
  - `triggerImport()` - Öffnet File-Picker für Import
  - `closeEditor()` - Schließt Editor-View
  - `escapeHtml()` - Sanitization für sichere HTML-Rendering

### Changed
- **PlaylistEditor.cshtml**
  - Neues Toolbar mit 4 Haupt-Buttons (Export, Import, Add, Remove)
  - Improved renderPlaylist() für korrektes Auslesen aller Item-Properties
  - Hidden File-Input für Import-Dialog
  - Metadaten-Display: Zeigt FilePath, DurationSeconds, Status pro Video
  - Status-Feedback-Bereich mit grüner Bestätigungsmeldung

- **PlayoutController.cs**
  - 5 neue Endpoints implementiert zur Playlist-Verwaltung
  - Export erstellt JSON-Response mit Content-Disposition-Header
  - Import akzeptiert JSON-Upload per FormData

- **CSS Styling**
  - Neue Klassen: `.toolbar`, `.toolbar-input`, `.btn-add`, `.btn-danger`
  - Professionelle Button-Styling mit Farben und Icons
  - Toolbar-Layout mit Flexbox für responsive Design

### Fixed
- **Metadaten-Laden-Bug**
  - Korrekte API-Response-Verarbeitung bei Playlist-Abfrage
  - Fehlerfreie Property-Extraktion aus PlaylistItem-Objekten
  - Validierung von Playlist-Struktur vor Rendering

- **File-Handling**
  - Blob-basierter Download funktioniert korrektuell in allen Browsern
  - FileReader-API für asynchrones Einlesen von Dateien
  - Fehlertolerante JSON-Parsing mit try-catch

- **Error Handling**
  - Alert-basierte Fehlerausgabe für ungültige Import-Dateien
  - Validierung: Erwartet JSON-Struktur `{ "playlist": [...] }`
  - Status-Feedback bei erfolgreicher Import/Export

### Known Limitations
- Thumbnails noch als Placeholders implementiert (CasparCG-Integration ausstehend)
- Media-Library-Endpoint liefert noch Dummy-Daten
- Streamer.bot-Metadaten werden noch nicht automatisch übertragen
- Keine Persistierung der Config zwischen Neustarts (in-Memory nur)

### Technical Details
- **Export-JSON Format:**
  ```json
  {
    "playlist": [
      {
        "filePath": "video.mp4",
        "durationSeconds": 120,
        "enabled": true,
        "status": "Ready",
        "streamerbotConfig": {
          "sendToStreamerbot": true,
          "metadataToSend": {"title": "My Video"}
        }
      }
    ]
  }
  ```

- **API Lifecycle:**
  - Export: GET `/api/playout/exportplaylist?filename=custom.json` → FileStream (application/json)
  - Import: POST `/api/playout/importplaylist` → JSON Body → PlaylistManager.UpdatePlaylistFromJson()
  - Add: POST `/api/playout/addmedia?filePath=...` → PlaylistManager.AddItem()
  - Delete: POST `/api/playout/deleteitem/{index}` → PlaylistManager.RemoveItem()

---

## [0.0.1b] - 2026-03-22

**Hybrid-Architektur: REST API + Streamer.bot Integration**

### Added
- **REST API für Core (Port 5000)**
  - `GET /api/playout/status` - Status & Playlist
  - `POST /api/playout/play` - Video abspielen
  - `POST /api/playout/stop` - Video stoppen
  - `POST /api/playout/skip` - Zum nächsten Video springen
  - `POST /api/playout/replay` - Video neu starten
  - `POST /api/playout/loop?enabled=true|false` - Loop-Modus
  - `GET /api/playout/playlist` - Aktuelle Playlist abrufen
  - `GET /api/playout/media` - Media-Bibliothek abrufen
  - `GET /api/playout/diagnostics` - Diagnostics-Report
  - `GET /api/playout/health` - Health-Check

- **Enhanced Logging-System**
  - Log-Level-System: Debug, Info, Warning, Error, Critical
  - Log-Level-Filtering (SetMinimumLogLevel)
  - Spezielle Methoden: LogError(), LogWarning(), LogDebug()
  - Millisekunden-Präzision in Timestamps

- **DiagnosticsService**
  - Service-Status-Tracking (Connected, Failed, Registered)
  - Connection-Attempt-Zähler
  - Response-Zeit-Messung
  - Detailliertes Error-Tracking
  - Health-Check: IsServiceHealthy()
  - Diagnostics-Report-Generator

- **GUI REST Client (HTTP-basiert)**
  - HttpClient statt WatsonWebsocket
  - Asynchrone REST-API-Requests zu Core
  - Auto-Status-Updates (Timer alle 2 Sekunden)
  - Service-Erkennung in Responses
  - Verbindungs-Diagnostics
  - GUI zeigt Connected/Disconnected Status

### Changed
- **Architektur-Umgestaltung (Client-Server-Modell)**
  - WebSocketServer wird **nicht mehr gestartet** im Core
  - Core läuft auf **Port 5000** (nicht 8080)
  - GUI verbindet sich zu Core via **REST API** (nicht WebSocket)
  - Streamer.bot läuft auf **Port 8081** (konfiguriert in appsettings.json)
  - **Keine Port-Konflikte** mehr zwischen Core und Streamer.bot

- **PlayoutService**
  - WebSocket-Server-Start entfernt (\_wsServer.Start() kommentiert)
  - PlayoutController registrierung mit PlaylistManager
  - Streamer.bot bleibt als WebSocket-Client

- **GUI Layout**
  - Port-Default von 8080 zu 5000 geändert
  - Label-Updates für REST API (nicht WebSocket)
  - Status-Display mit Playback-Informationen
  - Media-Library-Loading via REST API

### Removed
- WebSocketServer-Instanz aus PlayoutService (Server-Modus)
- WatsonWebsocket-Dependencies aus GUI (neu: HttpClient)
- WebSocket Command-Handler aus PlayoutService (nunmehr via REST)

### Fixed
- Port-Konflikt zwischen Core WebSocket (8080) und Streamer.bot (8081)
- GUI zeigt jetzt korrekten Service-Namen bei Connection
- REST API robuster gegen fehlende PlayoutManager-Initialisierung

### Technical Details
- **Port Allocation:**
  - PlayoutServer.Core: `http://localhost:5000` (REST API)
  - Streamer.bot: `ws://localhost:8081` (WebSocket Server)
  - GUI: HTTP Client zum Core

- **Communication Flow:**
  - GUI → REST HTTP Requests → Core
  - Core → CasparCG (AMCP/TCP)
  - Core → Streamer.bot (WebSocket Events mit Metadaten)
  - Streamer.bot → Actions (Trigger Stream Title, etc.)

---

## [0.0.1] - 2026-03-22

### Added
- **Kern-Architektur**
  - .NET 10 Konsolen- und WPF-Anwendung
  - ASP.NET Core MVC Web-App mit Razor Views
  - Provider-Pattern für Playout-Quellen (CasparCG, OBS)

- **Playout-Funktionen**
  - CasparCG AMCP-Provider (TCP-basiert)
  - Playlist-Management mit Loop-Funktion
  - Video-Play, Stop, Skip, Replay
  - Playlist-Updates via WebSocket
  - Duration-basierte VideoEnded-Events (Timer)

- **WebSocket-Server**
  - Echtzeit-Kommunikation zwischen Core und UI
  - Event-Broadcasting (PLAYLIST_UPDATE, VIDEO_STARTED/ENDED, PLAYBACK_COMPLETED)
  - Command-Handling (play, stop, skip, toggleloop, loadplaylist, scanmedia)

- **Media-Bibliothek**
  - CasparCG LS-Integration (direktes Abrufen von Media-Dateien)
  - Lokaler MediaScanner (optional, konfigurierbar)
  - Media-Item-Struktur mit Metadaten (Dateiname, Größe, Dauer)
  - Media-Library-Update-Events über WebSocket

- **Benutzeroberflächen**
  - **Web-GUI** (`http://localhost:5000`)
    - Live-Dashboard mit Echtzeit-Status
    - Playlist-Tabelle mit Checkboxen (Enable/Disable)
    - Media-Library mit List- und Thumbnail-View
    - Playback-Steuerung (Play, Stop, Next, Toggle Loop)
    - Event-Log mit Echtzeitstatus
  - **WPF Desktop-GUI** (Windows)
    - Verbindungsstatus
    - Event-Liste
    - Playback-Buttons (Play, Stop, Next)
    - Einstellungen-Panel (Host, Port)

- **Konfiguration**
  - `appsettings.json` für alle Settings
  - Wählbare PlayoutMode (CasparCG / OBS)
  - WebSocket-Port-Configuration
  - Loop-Enabled-Flag
  - Local/Remote Media-Scanner-Umschaltung
  - Streamer.bot Integration (enable/disable, Host, Port, Password)

- **Logging**
  - Separate Log-Verzeichnisse: `Logs/Log_core` und `Logs/Log_gui`
  - Zeitgestempelte Log-Dateien pro Session (Format: `PREFIX_YYYY-MM-DD_HH-mm-ss.txt`)
  - Konsolenausgabe + Datei-Logging
  - FileLogger mit Thread-Safety

- **Streamer.bot Integration**
  - WebSocket-Verbindung zu Streamer.bot
  - SHA256-basierte Authentifizierung (Handshake mit Salt & Challenge)
  - DoAction-Support (Ausführung von Streamer.bot-Aktionen)
  - Subscribe-Support für Events
  - Automatische Authentifizierung bei Bedarf
  - Fehlertoleranz mit Fallback

- **Start-Scripts**
  - Windows: `start_core.bat`, `start_gui.bat`
  - Linux/Mac: `start_core.sh`, `start_gui.sh`
  - Einfaches Starten ohne Konsolen-Befehle

- **Dokumentation**
  - Bilingual README (Deutsch/Englisch)
  - Quick-Start-Anweisungen
  - Setup-Instruktionen (Windows, Linux, Mac)
  - Konfiguration & Streamer.bot Integration Docs
  - Projekt-Struktur Übersicht

### Changed
- GUI-MediaScanner jetzt Provider-basiert (CasparCG LS als Primary)
- MediaScanner nur noch Secondary-Methode (falls LS fehlschlägt)
- PlayoutService startet **nicht** automatisch Playlist (nur Prepare)

### Fixed
- WebSocket ACL-Fehler (Fallback auf 0.0.0.0 Binding)
- Null-Reference-Warnings in Playlist-Update
- CasparCG Media-Parsing mit Multi-Line-Responses (ReadLinesUntilOkAsync)
- GUI-Syntax-Fehler in ConnectButton_Click

### Removed
- Automatisches Auto-Play beim Core-Start (nun manuell via Button)
- Eigene MediaScanner als Primary-Quelle (nur noch Fallback)

---

## [0.0.0] - Unreleased

- Initial project setup (Repository created, no release)

---

## Geplante Features (Roadmap)

### v0.1.0
- [ ] OBS Studio Provider vollständig implementieren
- [ ] FFmpeg-Integration für genaue Video-Dauer
- [ ] Drag-and-Drop Playlist-Reordering in Web-UI
- [ ] Playlist Auto-Save zu JSON nach Änderungen
- [ ] erweiterte Media-Filter (nach Typ, Größe, etc.)

### v0.2.0
- [ ] Mehrsprachige UI-Unterstützung (Lokalisierung)
- [ ] API-Tests (Unit & Integration)
- [ ] Streamer.bot Action-Callbacks
- [ ] Custom Trigger-System
- [ ] Echtzeit-Video-Preview in Web-UI

### v0.3.0
- [ ] Speichern von Einstellungen (GUI) in Local-Storage
- [ ] Multi-Playlist-Support
- [ ] Scheduler für automatisierte Wiedergabe
- [ ] Export/Import von Playlisten

### Future
- [ ] Docker-Container
- [ ] Remote-Monitoring-Dashboard
- [ ] Erweiterte Analytics & Logging
- [ ] Mobile Web-UI
- [ ] REST-API neben WebSocket

---

## Technologie-Stack

- **Language**: C# (.NET 10)
- **Web**: ASP.NET Core MVC + Razor Views
- **Desktop**: WPF (Windows Presentation Foundation)
- **Real-Time**: WatsonWebsocket
- **Serialization**: Newtonsoft.Json
- **External Integration**: CasparCG (AMCP), OBS Studio HTTP API, Streamer.bot WebSocket

---

## Lizenz

(Lizenz hier einfügen, z.B. MIT, GPL, etc.)

