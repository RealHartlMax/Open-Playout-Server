using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PlayoutServer.GUI;

public partial class PlaylistEditorWindow : Window
{
    private HttpClient _httpClient;
    private string _coreBaseUrl;
    private ObservableCollection<PlaylistItemVM> _playlistItems = new();
    private List<MediaItem> _mediaLibrary = new();
    private int _dragSourceIndex = -1;
    private int _currentConfigIndex = -1;

    public class PlaylistItemVM
    {
        public string Name { get; set; } = "";
        public int Duration { get; set; }
        public string FilePath { get; set; } = "";
        public bool SendToStreamerbot { get; set; }
        public string StreamerbotStatus => SendToStreamerbot ? "✓ Streamer.bot" : "○ Lokal";
        public Dictionary<string, bool> StreamerbotMetadata { get; set; } = new();

        public PlaylistItemVM() { }
        public PlaylistItemVM(string name, int duration = 0, string filePath = "")
        {
            Name = name;
            Duration = duration;
            FilePath = filePath;
        }
    }

    public class MediaItem
    {
        public string Name { get; set; } = "";
        public string Path { get; set; } = "";
        public int Duration { get; set; }
    }

    public PlaylistEditorWindow(HttpClient httpClient, string coreBaseUrl)
    {
        InitializeComponent();
        _httpClient = httpClient;
        _coreBaseUrl = coreBaseUrl;
        PlaylistListBox.ItemsSource = _playlistItems;
        
        _ = LoadPlaylistAsync();
        _ = LoadMediaLibraryAsync();
    }

    private async Task LoadPlaylistAsync()
    {
        try
        {
            FileLogger.Log("[PlaylistEditor] Lade Playlist...");
            var response = await _httpClient.GetAsync("api/playout/playlist");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var data = JsonConvert.DeserializeObject<dynamic>(content);
                var playlist = data?.playlist as JArray;
                
                _playlistItems.Clear();
                if (playlist != null)
                {
                    foreach (var item in playlist)
                    {
                        var name = item["mediaName"]?.ToString() ?? item["name"]?.ToString() ?? "Unbekannt";
                        var duration = (int?)item["durationSeconds"] ?? (int?)item["duration"] ?? 0;
                        var filePath = item["filePath"]?.ToString() ?? "";
                        var sendStreamerbot = (bool?)item["sendToStreamerbot"] ?? false;
                        
                        _playlistItems.Add(new PlaylistItemVM(name, duration, filePath)
                        {
                            SendToStreamerbot = sendStreamerbot
                        });
                    }
                }

                UpdateEmptyState();
                UpdateStatusBar();
                FileLogger.Log($"[PlaylistEditor] Playlist geladen: {_playlistItems.Count} Videos");
            }
        }
        catch (Exception ex)
        {
            FileLogger.LogError("[PlaylistEditor] Fehler beim Laden", ex);
            MessageBox.Show($"Fehler beim Laden der Playlist: {ex.Message}");
        }
    }

    private async Task LoadMediaLibraryAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("api/playout/media");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var data = JsonConvert.DeserializeObject<dynamic>(content);
                var media = data?.media as JArray;
                
                _mediaLibrary.Clear();
                if (media != null)
                {
                    foreach (var item in media)
                    {
                        _mediaLibrary.Add(new MediaItem
                        {
                            Name = item["name"]?.ToString() ?? "Unknown",
                            Path = item["path"]?.ToString() ?? "",
                            Duration = (int?)item["duration"] ?? 0
                        });
                    }
                }
            }
        }
        catch (Exception ex)
        {
            FileLogger.LogDebug($"[PlaylistEditor] Media-Library Fehler: {ex.Message}");
        }
    }

    private void UpdateEmptyState()
    {
        if (_playlistItems.Count == 0)
        {
            PlaylistListBox.Visibility = Visibility.Collapsed;
            EmptyStateText.Visibility = Visibility.Visible;
        }
        else
        {
            PlaylistListBox.Visibility = Visibility.Visible;
            EmptyStateText.Visibility = Visibility.Collapsed;
        }
    }

    private void UpdateStatusBar()
    {
        StatusText.Text = $"{_playlistItems.Count} Videos";
    }

    private void AddMediaButton_Click(object sender, RoutedEventArgs e)
    {
        ShowAddMediaModal();
    }

    private void ShowAddMediaModal()
    {
        AddMediaGrid.Children.Clear();
        
        if (_mediaLibrary.Count == 0)
        {
            var noMediaText = new TextBlock 
            { 
                Text = "Keine Medien verfügbar", 
                Foreground = System.Windows.Media.Brushes.Gray,
                Padding = new Thickness(10),
                TextAlignment = TextAlignment.Center
            };
            AddMediaGrid.Children.Add(noMediaText);
        }
        else
        {
            foreach (var media in _mediaLibrary)
            {
                var button = new Button
                {
                    Content = $"🎬 {media.Name} ({media.Duration}s)",
                    Padding = new Thickness(10, 8, 10, 8),
                    Margin = new Thickness(5),
                    Background = System.Windows.Media.Brushes.DarkSlateBlue,
                    Foreground = System.Windows.Media.Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Tag = media
                };
                button.Click += (s, args) => AddMediaToPlaylist(media);
                AddMediaGrid.Children.Add(button);
            }
        }

        // Show modal
        AddMediaModal.Visibility = Visibility.Visible;
        AddMediaModal.IsHitTestVisible = true;
        AddMediaModal.Opacity = 1;
    }

    private void CloseAddMediaModal_Click(object sender, RoutedEventArgs e)
    {
        AddMediaModal.IsHitTestVisible = false;
        AddMediaModal.Opacity = 0;
        AddMediaModal.Visibility = Visibility.Collapsed;
    }

    private void AddMediaToPlaylist(MediaItem media)
    {
        var item = new PlaylistItemVM(media.Name, media.Duration, media.Path);
        _playlistItems.Add(item);
        FileLogger.Log($"[PlaylistEditor] Video hinzugefügt: {media.Name}");
        AddMediaModal.IsHitTestVisible = false;
        AddMediaModal.Opacity = 0;
        AddMediaModal.Visibility = Visibility.Collapsed;
        UpdateStatusBar();
    }

    private void RemoveButton_Click(object sender, RoutedEventArgs e)
    {
        if (PlaylistListBox.SelectedItem is PlaylistItemVM item)
        {
            _playlistItems.Remove(item);
            FileLogger.Log($"[PlaylistEditor] Gelöscht: {item.Name}");
            UpdateStatusBar();
            UpdateEmptyState();
        }
        else
        {
            MessageBox.Show("Bitte wähle zuerst ein Video aus.");
        }
    }

    private void ExportButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "JSON Dateien (*.json)|*.json",
            DefaultExt = ".json",
            FileName = $"Playlist_{DateTime.Now:yyyy-MM-dd}"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                var playlistData = new
                {
                    playlist = _playlistItems.Select(x => new
                    {
                        name = x.Name,
                        filePath = x.FilePath,
                        durationSeconds = x.Duration,
                        sendToStreamerbot = x.SendToStreamerbot,
                        streamerbotMetadata = x.StreamerbotMetadata
                    }).ToList()
                };

                var json = JsonConvert.SerializeObject(playlistData, Formatting.Indented);
                File.WriteAllText(dialog.FileName, json, Encoding.UTF8);
                
                FileLogger.Log($"[PlaylistEditor] Exportiert: {dialog.FileName}");
                StatusText.Text = $"✓ Exportiert: {Path.GetFileName(dialog.FileName)}";
                MessageBox.Show($"Playlist erfolgreich exportiert zu:\n{dialog.FileName}", "Erfolg", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                FileLogger.LogError("[PlaylistEditor] Export-Fehler", ex);
                MessageBox.Show($"Fehler beim Exportieren: {ex.Message}");
            }
        }
    }

    private void ImportButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "JSON Dateien (*.json)|*.json",
            DefaultExt = ".json"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                var json = File.ReadAllText(dialog.FileName, Encoding.UTF8);
                var data = JsonConvert.DeserializeObject<JObject>(json);
                var playlist = data?["playlist"] as JArray;

                if (playlist == null)
                {
                    MessageBox.Show("Ungültiges Dateiformat. Erwartet: { \"playlist\": [...] }");
                    return;
                }

                _playlistItems.Clear();
                foreach (var item in playlist)
                {
                    var name = item["name"]?.ToString() ?? "Unbekannt";
                    var duration = (int?)item["durationSeconds"] ?? 0;
                    var filePath = item["filePath"]?.ToString() ?? "";
                    var sendStreamerbot = (bool?)item["sendToStreamerbot"] ?? false;
                    
                    _playlistItems.Add(new PlaylistItemVM(name, duration, filePath)
                    {
                        SendToStreamerbot = sendStreamerbot
                    });
                }

                UpdateStatusBar();
                UpdateEmptyState();
                FileLogger.Log($"[PlaylistEditor] Importiert: {dialog.FileName} ({_playlistItems.Count} Videos)");
                StatusText.Text = $"✓ Importiert: {_playlistItems.Count} Videos";
                MessageBox.Show($"Playlist mit {_playlistItems.Count} Videos erfolgreich importiert!", "Erfolg", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                FileLogger.LogError("[PlaylistEditor] Import-Fehler", ex);
                MessageBox.Show($"Fehler beim Importieren: {ex.Message}");
            }
        }
    }

    private void ConfigButton_Click(object sender, RoutedEventArgs e)
    {
        var button = sender as Button;
        var videoName = button?.Tag?.ToString();
        
        var item = _playlistItems.FirstOrDefault(x => x.Name == videoName);
        if (item != null)
        {
            _currentConfigIndex = _playlistItems.IndexOf(item);
            
            ConfigModalTitle.Text = $"Konfiguration: {item.Name}";
            EnableStreamerbotCheck.IsChecked = item.SendToStreamerbot;
            MetaTitleCheck.IsChecked = item.StreamerbotMetadata.ContainsKey("title") && item.StreamerbotMetadata["title"];
            MetaDurationCheck.IsChecked = item.StreamerbotMetadata.ContainsKey("duration") && item.StreamerbotMetadata["duration"];
            MetaFilenameCheck.IsChecked = item.StreamerbotMetadata.ContainsKey("filename") && item.StreamerbotMetadata["filename"];
            
            ConfigModal.IsHitTestVisible = true;
            ConfigModal.Opacity = 1;
        }
    }

    private void EnableStreamerbot_Changed(object sender, RoutedEventArgs e)
    {
        StreamerbotOptionsPanel.Visibility = (EnableStreamerbotCheck.IsChecked == true) ? Visibility.Visible : Visibility.Collapsed;
    }

    private void SaveConfigModal_Click(object sender, RoutedEventArgs e)
    {
        if (_currentConfigIndex >= 0 && _currentConfigIndex < _playlistItems.Count)
        {
            var item = _playlistItems[_currentConfigIndex];
            item.SendToStreamerbot = EnableStreamerbotCheck.IsChecked == true;
            
            item.StreamerbotMetadata.Clear();
            if (MetaTitleCheck.IsChecked == true) item.StreamerbotMetadata["title"] = true;
            if (MetaDurationCheck.IsChecked == true) item.StreamerbotMetadata["duration"] = true;
            if (MetaFilenameCheck.IsChecked == true) item.StreamerbotMetadata["filename"] = true;
            
            FileLogger.Log($"[PlaylistEditor] Konfiguration gespeichert: {item.Name}");
            
            // Refresh UI
            var index = _currentConfigIndex;
            _playlistItems[index] = item;
            
            ConfigModal.IsHitTestVisible = false;
            ConfigModal.Opacity = 0;
            ConfigModal.Visibility = Visibility.Collapsed;
            _currentConfigIndex = -1;
        }
    }

    private void CloseConfigModal_Click(object sender, RoutedEventArgs e)
    {
        ConfigModal.IsHitTestVisible = false;
        ConfigModal.Opacity = 0;
        ConfigModal.Visibility = Visibility.Collapsed;
        _currentConfigIndex = -1;
    }

    // Drag & Drop Handler
    private void PlaylistListBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var item = (sender as ListBox)?.SelectedItem;
        if (item != null)
        {
            _dragSourceIndex = PlaylistListBox.SelectedIndex;
            DragDrop.DoDragDrop(PlaylistListBox, item, DragDropEffects.Move);
        }
    }

    private void PlaylistListBox_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            var listBox = sender as ListBox;
            if (listBox?.SelectedItem != null)
            {
                DragDrop.DoDragDrop(listBox, listBox.SelectedItem, DragDropEffects.Move);
            }
        }
    }

    private void PlaylistListBox_PreviewDrop(object sender, DragEventArgs e)
    {
        e.Effects = DragDropEffects.Move;
        e.Handled = true;
    }

    private void PlaylistListBox_Drop(object sender, DragEventArgs e)
    {
        var listBox = sender as ListBox;
        if (listBox == null) return;

        var dropTarget = e.GetPosition(listBox);
        var targetIndex = GetIndexAtPosition(listBox, dropTarget);

        if (targetIndex >= 0 && _dragSourceIndex >= 0 && _dragSourceIndex != targetIndex)
        {
            var item = _playlistItems[_dragSourceIndex];
            _playlistItems.RemoveAt(_dragSourceIndex);
            
            if (targetIndex > _dragSourceIndex)
                targetIndex--;
            
            _playlistItems.Insert(targetIndex, item);
            listBox.SelectedIndex = targetIndex;
            
            FileLogger.Log($"[PlaylistEditor] Item verschoben: {_dragSourceIndex} → {targetIndex}");
        }

        _dragSourceIndex = -1;
    }

    private int GetIndexAtPosition(ListBox listBox, Point position)
    {
        for (int i = 0; i < listBox.Items.Count; i++)
        {
            var item = listBox.ItemContainerGenerator.ContainerFromIndex(i) as ListBoxItem;
            if (item != null)
            {
                var bounds = new Rect(item.RenderSize);
                var relativePosition = item.PointFromScreen(listBox.PointToScreen(position));
                
                if (relativePosition.Y < bounds.Height / 2 && i > 0)
                    return i - 1;
                else if (relativePosition.Y >= bounds.Height / 2)
                    return i;
            }
        }
        return _playlistItems.Count - 1;
    }

    private void ItemBorder_MouseEnter(object sender, MouseEventArgs e)
    {
        if (sender is Border border)
            border.Background = System.Windows.Media.Brushes.DarkSlateGray;
    }

    private void ItemBorder_MouseLeave(object sender, MouseEventArgs e)
    {
        if (sender is Border border)
            border.Background = System.Windows.Media.Brushes.DarkSlateGray;
    }

    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        RemoveButton_Click(sender, e);
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            StatusText.Text = "Speichern...";
            
            var newPlaylist = _playlistItems.Select(x => new 
            { 
                name = x.Name, 
                filePath = x.FilePath,
                duration = x.Duration,
                durationSeconds = x.Duration,
                sendToStreamerbot = x.SendToStreamerbot,
                streamerbotMetadata = x.StreamerbotMetadata
            }).ToList();
            
            var json = JsonConvert.SerializeObject(new { items = newPlaylist });
            
            FileLogger.Log("[PlaylistEditor] Speichern Playlist...");
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("api/playout/updateplaylist", content);
            
            if (response.IsSuccessStatusCode)
            {
                FileLogger.Log("[PlaylistEditor] Playlist gespeichert!");
                StatusText.Text = "✓ Gespeichert!";
                MessageBox.Show("Playlist erfolgreich gespeichert!", "Erfolg", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            else
            {
                StatusText.Text = "❌ Fehler beim Speichern";
                MessageBox.Show($"Fehler beim Speichern: {response.StatusCode}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            StatusText.Text = "❌ Fehler";
            FileLogger.LogError("[PlaylistEditor] Fehler beim Speichern", ex);
            MessageBox.Show($"Fehler: {ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
