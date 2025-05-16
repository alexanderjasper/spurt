using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Spurt.Data.Queries;
using Spurt.Domain.Categories;
using Spurt.Domain.Games;
using Spurt.Domain.Games.Commands;
using Spurt.Domain.Players;

namespace Spurt.Components.Pages;

public partial class Game(
    ILocalStorageService localStorage,
    NavigationManager navigation,
    IGetGame getGame,
    IStartGame startGame) : IAsyncDisposable
{
    [Parameter] public required string Code { get; init; }

    private Domain.Games.Game? CurrentGame { get; set; }
    private HubConnection? _hubConnection;
    private Guid? _currentUserId;
    private Player? _currentPlayer;
    private bool _categorySubmitted;
    private bool _isCreator;
    private List<string> _categorySubmissions = [];

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;

        _currentUserId = await localStorage.GetItemAsync<Guid?>("UserId");
        if (_currentUserId == null)
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
        _hubConnection.On<string>(GameHub.Events.CategorySubmitted, async _ => await LoadGameData());
        _hubConnection.On<string>(GameHub.Events.GameStarted, async _ => await LoadGameData());
        _hubConnection.On<string>(GameHub.Events.ClueSelected, async _ => await LoadGameData());

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

        _currentPlayer = CurrentGame.Players.FirstOrDefault(p => p.UserId == _currentUserId);
        if (_currentPlayer != null)
        {
            _categorySubmitted = _currentPlayer.Category?.IsSubmitted ?? false;
            _isCreator = _currentPlayer.IsCreator;
        }

        _categorySubmissions = CurrentGame.Players
            .Where(p => p.Category?.IsSubmitted ?? false)
            .Select(p => p.User.Name)
            .ToList();

        await InvokeAsync(StateHasChanged);
    }

    private async Task StartGame()
    {
        if (_currentUserId == null) return;

        try
        {
            await startGame.Execute(Code, _currentUserId.Value);
            await LoadGameData();
        }
        catch (Exception)
        {
            // Handle the error - could show a message to the user
        }
    }

    private async Task SelectClue(Category category, int pointValue)
    {
        if (_hubConnection == null) return;

        throw new NotImplementedException();
        await LoadGameData();
    }

    private async Task OnCategorySaved()
    {
        await LoadGameData();
    }

    public async ValueTask DisposeAsync()
    {
        if (_hubConnection is not null) await _hubConnection.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}