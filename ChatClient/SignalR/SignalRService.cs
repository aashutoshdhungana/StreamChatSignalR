using Microsoft.AspNetCore.SignalR.Client;

namespace ChatClient.SignalR;

public class SignalRService
{
    private HubConnection? _hubConnection;
    private readonly string _hubUrl;

    public SignalRService(string hubUrl)
    {
        _hubUrl = hubUrl;
    }

    public async Task ConnectAsync()
    {
        _hubConnection = new HubConnectionBuilder()
            .WithUrl(_hubUrl)
            .Build();

        await _hubConnection.StartAsync();
    }

    public async Task DisconnectAsync()
    {
        if (_hubConnection != null)
        {
            await _hubConnection.DisposeAsync();
            _hubConnection = null;
        }
    }

    public IAsyncEnumerable<string> StreamResponseAsync(string message)
    {
        if (_hubConnection == null)
            throw new InvalidOperationException("Connection not established. Call ConnectAsync first.");

        return _hubConnection.StreamAsync<string>("StreamResponse", message);
    }

    public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;
}