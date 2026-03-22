# Changelog

Alle bemerkenswerten Änderungen an diesem Projekt werden in dieser Datei dokumentiert.

Das Format basiert auf [Keep a Changelog](https://keepachangelog.com/de/1.0.0/),
und dieses Projekt folgt der [Semantic Versioning](https://semver.org/lang/de/).

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

