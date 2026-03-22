using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Newtonsoft.Json;

namespace PlayoutServer.GUI;

public partial class PlaylistEditorWindow : Window
{
    private HttpClient _httpClient;
    private string _coreBaseUrl;
    private ObservableCollection<PlaylistItemVM> _playlistItems = new();
    private int _dragSourceIndex = -1;

    public class PlaylistItemVM
    {
        public string Name { get; set; } = "";
        public int Duration { get; set; }

        public PlaylistItemVM() { }
        public PlaylistItemVM(string name, int duration = 0)
        {
            Name = name;
            Duration = duration;
        }
    }

    public PlaylistEditorWindow(HttpClient httpClient, string coreBaseUrl)
    {
        InitializeComponent();
        _httpClient = httpClient;
        _coreBaseUrl = coreBaseUrl;
        PlaylistListBox.ItemsSource = _playlistItems;
        
        _ = LoadPlaylistAsync();
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
                var playlist = data?.playlist as Newtonsoft.Json.Linq.JArray;
                
                _playlistItems.Clear();
                if (playlist != null)
                {
                    foreach (var item in playlist)
                    {
                        var name = item["name"]?.ToString() ?? "Unknown";
                        var duration = (int?)item["duration"] ?? 0;
                        _playlistItems.Add(new PlaylistItemVM(name, duration));
                    }
                }

                FileLogger.Log($"[PlaylistEditor] Playlist geladen: {_playlistItems.Count} Videos");
            }
        }
        catch (Exception ex)
        {
            FileLogger.LogError("[PlaylistEditor] Fehler beim Laden", ex);
            MessageBox.Show($"Fehler beim Laden der Playlist: {ex.Message}");
        }
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
            border.Background = System.Windows.Media.Brushes.DimGray;
    }

    private void ItemBorder_MouseLeave(object sender, MouseEventArgs e)
    {
        if (sender is Border border)
            border.Background = (System.Windows.Media.Brush)FindResource("SystemControlBackgroundListMediumBrush") ?? System.Windows.Media.Brushes.DarkSlateGray;
    }

    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        var button = sender as Button;
        var videoName = button?.Tag?.ToString();
        
        if (!string.IsNullOrEmpty(videoName))
        {
            var item = _playlistItems.FirstOrDefault(x => x.Name == videoName);
            if (item != null)
            {
                _playlistItems.Remove(item);
                FileLogger.Log($"[PlaylistEditor] Gelöscht: {videoName}");
            }
        }
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var newPlaylist = _playlistItems.Select(x => new { name = x.Name, duration = x.Duration }).ToList();
            var json = JsonConvert.SerializeObject(new { items = newPlaylist });
            
            FileLogger.Log("[PlaylistEditor] Speichern Playlist...");
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("api/playout/updateplaylist", content);
            
            if (response.IsSuccessStatusCode)
            {
                FileLogger.Log("[PlaylistEditor] Playlist gespeichert!");
                MessageBox.Show("Playlist erfolgreich gespeichert!", "Erfolg", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show($"Fehler beim Speichern: {response.StatusCode}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
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
