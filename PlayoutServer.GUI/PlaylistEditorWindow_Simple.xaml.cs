using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json;

namespace PlayoutServer.GUI;

public partial class PlaylistEditorWindow : Window
{
    private HttpClient _httpClient;

    public PlaylistEditorWindow(HttpClient httpClient)
    {
        InitializeComponent();
        _httpClient = httpClient;
        _ = LoadPlaylistAsync();
    }

    private async Task LoadPlaylistAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("api/playout/playlist");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var data = JsonConvert.DeserializeObject<dynamic>(content);
                MessageBox.Show("Playlist Editor - bald verfügbar!", "Info");
            }
        }
        catch (Exception ex)
        {
            FileLogger.LogError("[PlaylistEditor] Error", ex);
        }
    }
}
