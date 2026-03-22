using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WatsonWebsocket;

namespace PlayoutServer.GUI;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private WatsonWsClient? _wsClient;
    private string _wsHost = "localhost";
    private int _wsPort = 8080;

    public MainWindow()
    {
        InitializeComponent();

        HostTextBox.Text = _wsHost;
        PortTextBox.Text = _wsPort.ToString();
    }

    private void ConnectButton_Click(object sender, RoutedEventArgs e)
    {
        if (_wsClient == null)
        {
            try
            {
                if (!int.TryParse(PortTextBox.Text, out _wsPort))
                {
                    MessageBox.Show("Ungültiger Port.");
                    return;
                }

                _wsHost = string.IsNullOrWhiteSpace(HostTextBox.Text) ? "localhost" : HostTextBox.Text.Trim();
                var wsUri = new Uri($"ws://{_wsHost}:{_wsPort}");

                FileLogger.Log($"Versuche WebSocket-Verbindung zu öffnen: {wsUri}");
                _wsClient = new WatsonWsClient(wsUri);
                _wsClient.MessageReceived += WsClient_MessageReceived;
                _wsClient.ServerConnected += (s, args) =>
                {
                    Dispatcher.Invoke(() => StatusText.Text = "Verbunden");
                    FileLogger.Log("WebSocket verbunden");
                };
                _wsClient.ServerDisconnected += (s, args) =>
                {
                    Dispatcher.Invoke(() => StatusText.Text = "Getrennt");
                    FileLogger.Log("WebSocket getrennt");
                };

                _wsClient.Start();
                ConnectButton.Content = "Trennen";
            }
            catch (Exception ex)
            {
                FileLogger.Log($"Verbindungsfehler: {ex.Message}");
                MessageBox.Show($"Verbindungsfehler: {ex.Message}");
            }
        }
        else
        {
            _wsClient.Stop();
            _wsClient = null;
            StatusText.Text = "Getrennt";
            ConnectButton.Content = "Verbinden";
            FileLogger.Log("WebSocket getrennt durch Benutzer");
        }
    }

    private void WsClient_MessageReceived(object? sender, MessageReceivedEventArgs e)
    {
        var message = Encoding.UTF8.GetString(e.Data);
        FileLogger.Log($"WS empfangen: {message}");
        Dispatcher.Invoke(() => EventsList.Items.Add(message));
    }

    private void OpenSettings_Click(object sender, RoutedEventArgs e)
    {
        SettingsPanel.Visibility = Visibility.Visible;
    }

    private void SaveSettings_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(PortTextBox.Text, out _wsPort))
        {
            SettingsStatus.Text = "Ungültiger Port.";
            return;
        }

        _wsHost = string.IsNullOrWhiteSpace(HostTextBox.Text) ? "localhost" : HostTextBox.Text.Trim();
        
        SettingsStatus.Text = $"Einstellungen gespeichert: {_wsHost}:{_wsPort}";
        FileLogger.Log(SettingsStatus.Text);
    }

    private void SendCommand(string command)
    {
        if (_wsClient != null && _wsClient.Connected)
        {
            var payload = $"{{\"command\":\"{command}\"}}";
            FileLogger.Log($"Sende Befehl: {payload}");
            _wsClient.SendAsync(Encoding.UTF8.GetBytes(payload));
        }
        else
        {
            FileLogger.Log($"Befehl nicht gesendet, WebSocket nicht verbunden: {command}");
        }
    }

    private void PlayButton_Click(object sender, RoutedEventArgs e) => SendCommand("play");
    private void StopButton_Click(object sender, RoutedEventArgs e) => SendCommand("stop");
    private void SkipButton_Click(object sender, RoutedEventArgs e) => SendCommand("skip");
}