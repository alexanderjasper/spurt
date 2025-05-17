using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using System.Text.Json.Serialization;

namespace Spurt.Domain.Games;

public interface IGameHubConnectionService : IAsyncDisposable
{
    Task Initialize(string gameCode);
    bool IsConnected { get; }
    void RegisterOnGameUpdated(Func<Game, Task> handler);
}

public class GameHubConnectionService(NavigationManager navigation) : IGameHubConnectionService
{
    private HubConnection? _hubConnection;

    public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;

    public async Task Initialize(string gameCode)
    {
        if (_hubConnection != null) return;

        _hubConnection = new HubConnectionBuilder()
            .WithUrl(navigation.ToAbsoluteUri("/gamehub"))
            .WithAutomaticReconnect()
            .AddJsonProtocol(options =>
            {
                options.PayloadSerializerOptions.ReferenceHandler = ReferenceHandler.Preserve;
            })
            .Build();

        try
        {
            await _hubConnection.StartAsync();
            await _hubConnection.SendAsync(GameHub.Methods.JoinGameGroup, gameCode);
        }
        catch
        {
            // Connection failed - game will still load but won't receive real-time updates
        }
    }

    public void RegisterOnGameUpdated(Func<Game, Task> handler)
    {
        _hubConnection?.On<Game>(GameHub.Events.GameUpdated, handler);
    }

    public async ValueTask DisposeAsync()
    {
        if (_hubConnection != null) await _hubConnection.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}