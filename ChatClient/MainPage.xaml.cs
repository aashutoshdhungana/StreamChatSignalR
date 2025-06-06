using System.Collections.ObjectModel;
using System.Text.Json;
using System.Threading.Tasks;
using ChatClient.SignalR;
using Microsoft.Maui.Dispatching;

namespace ChatClient
{
    public partial class MainPage : ContentPage
    {
        private readonly HttpClient _httpClient;
        private readonly ObservableCollection<string> _messages;
        private CancellationTokenSource _cancellationTokenSource;
        private string _currentStreamedMessage = "";

        public MainPage()
        {
            InitializeComponent();
            _httpClient = new HttpClient();
            _messages = new ObservableCollection<string>();
            MessagesListView.ItemsSource = _messages;
        }

        private async void OnStartStreamingClicked(object sender, EventArgs e)
        {
            StartButton.IsEnabled = false;
            _messages.Clear();
            _currentStreamedMessage = ""; // Reset the current message

            string userMessage = MessageEntry.Text?.Trim();
            if (string.IsNullOrWhiteSpace(userMessage))
            {
                _messages.Add("Please enter a message.");
                StartButton.IsEnabled = true;
                return;
            }

            string selectedMethod = StreamingMethodGroup.Children
                .OfType<RadioButton>()
                .FirstOrDefault(rb => rb.IsChecked)?.Content?.ToString();

            _cancellationTokenSource = new CancellationTokenSource();

            // Add user message to the display
            _messages.Add($"User: {userMessage}");
            _messages.Add("Assistant: "); // Add placeholder for the assistant's response

            try
            {
                switch (selectedMethod.ToLower())
                {
                    case "http":
                        await StreamMessages(userMessage, _cancellationTokenSource.Token);
                        break;
                    case "sse":
                        await StreamMessagesSSE(userMessage, _cancellationTokenSource.Token);
                        break;
                    case "signalr":
                        await StreamMessagesSignalR(userMessage, _cancellationTokenSource.Token);
                        break;
                    default:
                        _messages.Add("Please select a streaming method.");
                        break;
                }
            }
            catch (OperationCanceledException)
            {
                _messages.Add("Streaming cancelled");
            }
            catch (Exception ex)
            {
                _messages.Add($"Error: {ex.Message}");
            }
            finally
            {
                StartButton.IsEnabled = true;
            }
        }

        private async Task StreamMessages(string message, CancellationToken cancellationToken)
        {
            var baseUrl = "https://localhost:7075";
            var streamUrl = $"{baseUrl}/api/Stream/strings?message={Uri.EscapeDataString(message)}";

            using var request = new HttpRequestMessage(HttpMethod.Get, streamUrl);
            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = new StreamReader(stream);

            string line;
            while ((line = await reader.ReadLineAsync()) != null && !cancellationToken.IsCancellationRequested)
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    try
                    {
                        var messageData = JsonSerializer.Deserialize<MessageData>(line);
                        _currentStreamedMessage += messageData.Message;
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            if (_messages.Count > 0)
                            {
                                _messages[_messages.Count - 1] = $"Assistant: {_currentStreamedMessage}";
                            }
                        });
                    }
                    catch (JsonException)
                    {
                        _currentStreamedMessage += line;

                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            if (_messages.Count > 0)
                            {
                                _messages[_messages.Count - 1] = $"Assistant: {_currentStreamedMessage}";
                            }
                        });
                    }
                }
            }
        }

        private async Task StreamMessagesSignalR(string message, CancellationToken cancellationToken)
        {
            var baseUrl = "https://localhost:7075";
            var signalRClient = new SignalRService($"{baseUrl}/aiHub");

            await signalRClient.ConnectAsync();
            var response = signalRClient.StreamResponseAsync(message);
            await foreach (var chunk in response)
            {
                _currentStreamedMessage += chunk;
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (_messages.Count > 0)
                    {
                        _messages[_messages.Count - 1] = $"Assistant: {_currentStreamedMessage}";
                    }
                });
            }
            await signalRClient.DisconnectAsync();
        }

        private async Task StreamMessagesSSE(string message, CancellationToken cancellationToken)
        {
            var baseUrl = "https://localhost:7075";
            var streamUrl = $"{baseUrl}/api/Stream/strings-sse?message={Uri.EscapeDataString(message)}";

            using var request = new HttpRequestMessage(HttpMethod.Get, streamUrl);
            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = new StreamReader(stream);

            string line;
            while ((line = await reader.ReadLineAsync()) != null && !cancellationToken.IsCancellationRequested)
            {
                if (line.StartsWith("data: "))
                {
                    var jsonData = line.Substring(6);

                    try
                    {
                        var messageData = JsonSerializer.Deserialize<MessageData>(jsonData);

                        if (messageData.End)
                        {
                            MainThread.BeginInvokeOnMainThread(() =>
                            {
                                _messages.Add("Stream ended");
                            });
                            break;
                        }
                        _currentStreamedMessage += messageData.Message;

                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            if (_messages.Count > 0)
                            {
                                _messages[_messages.Count - 1] = $"Assistant: {_currentStreamedMessage}";
                            }
                        });
                    }
                    catch (JsonException)
                    {
                        var rawData = jsonData;
                        _currentStreamedMessage += rawData;

                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            if (_messages.Count > 0)
                            {
                                _messages[_messages.Count - 1] = $"Assistant: {_currentStreamedMessage}";
                            }
                        });
                    }
                }
            }
        }

        protected override void OnDisappearing()
        {
            _cancellationTokenSource?.Cancel();
            base.OnDisappearing();
        }
    }

    public class MessageData
    {
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }
        public bool End { get; set; }
    }
}