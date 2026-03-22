using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Newtonsoft.Json;

namespace PlayoutServer.GUI;

/// <summary>
/// GUI für PlayoutServer mit REST API Client
/// </summary>
public partial class MainWindow : Window
{
    private HttpClient _httpClient = new();
    private string _coreHost = "localhost";
    private int _corePort = 5000;
    private Timer? _statusUpdateTimer;
    private bool _isConnected = false;

    public MainWindow()
    {
        InitializeComponent();

        HostTextBox.Text = _coreHost;
        PortTextBox.Text = _corePort.ToString();
        
        // Status auto-update every 2 seconds
        _statusUpdateTimer = new Timer(UpdateStatusAsync, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2));
    }

    private void ConnectButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_isConnected)
        {
            try
            {
                if (!int.TryParse(PortTextBox.Text, out _corePort))
                {
                    MessageBox.Show("Ungültiger Port.");
                    return;
                }

                _coreHost = string.IsNullOrWhiteSpace(HostTextBox.Text) ? "localhost" : HostTextBox.Text.Trim();
                
                var baseUri = $"http://{_coreHost}:{_corePort}";
                _httpClient.BaseAddress = new Uri(baseUri);
                _isConnected = true;

                FileLogger.Log($"[REST-Client] Verbinde mit Core: {baseUri}");
                StatusText.Text = $"Verbunden mit {_coreHost}:{_corePort}";
                ConnectButton.Content = "Trennen";
            }
            catch (Exception ex)
            {
                FileLogger.LogError("[REST-Client] Verbindungsfehler", ex);
                MessageBox.Show($"Verbindungsfehler: {ex.Message}");
            }
        }
        else
        {
            _isConnected = false;
            _httpClient.BaseAddress = null;
            StatusText.Text = "Getrennt";
            ConnectButton.Content = "Verbinden";
            FileLogger.Log("[REST-Client] Getrennt durch Benutzer");
        }
    }

    private async void UpdateStatusAsync(object? state)
    {
        if (!_isConnected || _httpClient.BaseAddress == null) return;

        try
        {
            var response = await _httpClient.GetAsync("api/playout/status");
            if (response.IsSuccessStatusCode)
            {
                var jsonContent = await response.Content.ReadAsStringAsync();
                var status = JsonConvert.DeserializeObject<dynamic>(jsonContent);
                
                Dispatcher.Invoke(() =>
                {
                    try
                    {
                        var isRunning = (bool?)status?.isRunning ?? false;
                        var currentIndex = (int?)status?.currentIndex ?? 0;
                        var loopEnabled = (bool?)status?.loopEnabled ?? false;
                        
                        StatusText.Text = $"Status: {(isRunning ? "▶ Läuft" : "■ Gestoppt")} | Index: {currentIndex} | Loop: {(loopEnabled ? "✓" : "✗")}";
                        FileLogger.LogDebug($"[REST-Status] Running={isRunning}, Index={currentIndex}");
                    }
                    catch { }
                });
            }
        }
        catch
        {
            // Silent - Connection könnte nicht verfügbar sein
        }
    }

    private async void PlayButton_Click(object sender, RoutedEventArgs e) => await SendCommandAsync("play");
    private async void StopButton_Click(object sender, RoutedEventArgs e) => await SendCommandAsync("stop");
    private async void SkipButton_Click(object sender, RoutedEventArgs e) => await SendCommandAsync("skip");

    private void OpenPlaylistEditor_Click(object sender, RoutedEventArgs e)
    {
        if (!_isConnected || _httpClient.BaseAddress == null)
        {
            MessageBox.Show("Nicht verbunden. Bitte zuerst Verbindung herstellen.");
            return;
        }

        try
        {
            // Web-GUI Playlist Editor öffnen
            var editorUrl = $"{_httpClient.BaseAddress.Scheme}://{_httpClient.BaseAddress.Host}:{_httpClient.BaseAddress.Port}/playout/editor";
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = editorUrl,
                UseShellExecute = true
            });
            FileLogger.Log($"[MainWindow] Öffne Playlist Editor: {editorUrl}");
        }
        catch (Exception ex)
        {
            FileLogger.LogError("[MainWindow] Playlist Editor Error", ex);
            MessageBox.Show($"Fehler beim Öffnen: {ex.Message}");
        }
    }

    private async Task SendCommandAsync(string command)
    {
        if (!_isConnected || _httpClient.BaseAddress == null)
        {
            MessageBox.Show("Nicht verbunden. Bitte zuerst Verbindung herstellen.");
            return;
        }

        try
        {
            FileLogger.Log($"[REST-Command] Sende: {command}");
            var response = await _httpClient.PostAsync($"api/playout/{command}", null);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                FileLogger.Log($"[REST-Command] OK: {command}");
                EventsList.Items.Add($"[{DateTime.Now:HH:mm:ss}] {command.ToUpper()} ✓");
            }
            else
            {
                FileLogger.LogWarning($"[REST-Command] Error {response.StatusCode}: {command}");
                MessageBox.Show($"Fehler: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            FileLogger.LogError($"[REST-Command] {command}", ex);
            MessageBox.Show($"Fehler: {ex.Message}");
        }
    }

    private async void LoadMediaLibraryButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_isConnected || _httpClient.BaseAddress == null)
        {
            MessageBox.Show("Nicht verbunden.");
            return;
        }

        try
        {
            FileLogger.Log("[REST-API] Lade Media Library...");
            var response = await _httpClient.GetAsync("api/playout/media");
            
            if (response.IsSuccessStatusCode)
            {
                FileLogger.Log("[REST-API] Media Library erfolgreich geladen");
                EventsList.Items.Add($"[{DateTime.Now:HH:mm:ss}] Media Library geladen ✓");
            }
        }
        catch (Exception ex)
        {
            FileLogger.LogError("[REST-API] Media Library", ex);
        }
    }

    private void OpenSettings_Click(object sender, RoutedEventArgs e)
    {
        SettingsPanel.Visibility = Visibility.Visible;
    }

    private void SaveSettings_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(PortTextBox.Text, out int port))
        {
            SettingsStatus.Text = "Ungültiger Port.";
            return;
        }

        var host = string.IsNullOrWhiteSpace(HostTextBox.Text) ? "localhost" : HostTextBox.Text.Trim();
        SettingsStatus.Text = $"Einstellungen gespeichert: {host}:{port}";
        FileLogger.Log($"[Settings] Core: {host}:{port}");
    }
}