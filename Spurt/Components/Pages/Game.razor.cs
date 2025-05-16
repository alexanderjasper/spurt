using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Spurt.Data.Queries;
using Spurt.Domain.Games;

namespace Spurt.Components.Pages;

public partial class Game(
    ILocalStorageService localStorage,
    NavigationManager navigation,
    IGetGame getGame) : IAsyncDisposable
{
    [Parameter] public required string Code { get; init; }

    private Domain.Games.Game? CurrentGame { get; set; }
    private HubConnection? _hubConnection;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;

        var userId = await localStorage.GetItemAsync<Guid?>("UserId");
        if (userId == null)
        {
            navigation.NavigateTo("/register");
            return;
        }

        await InitializeSignalR();
        await LoadGameData();
    }

    private async Task InitializeSignalR()
    {
        _hubConnection = new HubConnectionBuilder()
            .WithUrl(navigation.ToAbsoluteUri("/gamehub"))
            .WithAutomaticReconnect()
            .Build();

        _hubConnection.On<string>(GameHub.Events.PlayerJoined, async _ => await LoadGameData());

        try
        {
            await _hubConnection.StartAsync();
            await _hubConnection.SendAsync(GameHub.Methods.JoinGameGroup, Code);
        }
        catch
        {
            // Connection failed - game will still load but won't receive real-time updates
        }
    }

    private async Task LoadGameData()
    {
        CurrentGame = await getGame.Execute(Code);

        if (CurrentGame == null)
        {
            navigation.NavigateTo("/");
            return;
        }

        await InvokeAsync(StateHasChanged);
    }

    public async ValueTask DisposeAsync()
    {
        if (_hubConnection is not null) await _hubConnection.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}