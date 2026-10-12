using System.Net.Http;
using System.Net.Http.Json;
using System.Windows;

namespace AirGapDemo.Client;

public partial class MainWindow : Window
{
    private readonly HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(5)
    };

    public MainWindow()
    {
        InitializeComponent();
    }

    private string GetServerAddress()
    {
        return ServerAddressTextBox.Text.Trim().TrimEnd('/');
    }

    private async void TestServer_Click(
        object sender,
        RoutedEventArgs e)
    {
        try
        {
            ConnectionStatusText.Text = "Connecting...";
            ResponseTextBox.Text = string.Empty;

            string url =
                $"{GetServerAddress()}/api/status";

            ServerStatus? status =
                await _httpClient.GetFromJsonAsync<ServerStatus>(url);

            if (status == null)
            {
                ConnectionStatusText.Text = "Invalid response";
                return;
            }

            ConnectionStatusText.Text = "Connected";

            ResponseTextBox.Text =
                $"Status: {status.Status}\n" +
                $"Server: {status.ServerName}\n" +
                $"Time: {status.ServerTime}";
        }
        catch (Exception ex)
        {
            ConnectionStatusText.Text = "Disconnected";

            ResponseTextBox.Text =
                $"Connection failed:\n{ex.Message}";
        }
    }

    private async void SendMessage_Click(
        object sender,
        RoutedEventArgs e)
    {
        try
        {
            var request = new MessageRequest
            {
                ClientName = Environment.MachineName,
                Message = MessageTextBox.Text
            };

            string url =
                $"{GetServerAddress()}/api/message";

            using HttpResponseMessage response =
                await _httpClient.PostAsJsonAsync(url, request);

            response.EnsureSuccessStatusCode();

            MessageResponse? result =
                await response.Content
                    .ReadFromJsonAsync<MessageResponse>();

            if (result == null)
            {
                ResponseTextBox.Text =
                    "Server returned an invalid response.";

                return;
            }

            ConnectionStatusText.Text = "Connected";

            ResponseTextBox.Text =
                $"{result.Message}\n\n" +
                $"Server time: {result.ServerTime}";
        }
        catch (Exception ex)
        {
            ConnectionStatusText.Text = "Disconnected";

            ResponseTextBox.Text =
                $"Send failed:\n{ex.Message}";
        }
    }
}

public class ServerStatus
{
    public string Status { get; set; } = string.Empty;

    public string ServerName { get; set; } = string.Empty;

    public DateTime ServerTime { get; set; }
}

public class MessageRequest
{
    public string ClientName { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;
}

public class MessageResponse
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public DateTime ServerTime { get; set; }
}