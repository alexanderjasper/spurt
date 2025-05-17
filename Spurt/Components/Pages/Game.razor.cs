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
    IStartGame startGame,
    IGameHubConnectionService gameHubConnectionService) : IAsyncDisposable
{
    [Parameter] public required string Code { get; init; }

    private Domain.Games.Game? CurrentGame { get; set; }
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

        await InitializeGameHub();
        await LoadGameData();
    }

    private async Task InitializeGameHub()
    {
        await gameHubConnectionService.Initialize(Code);
        
        gameHubConnectionService.RegisterOnPlayerJoined(LoadGameData);
        gameHubConnectionService.RegisterOnCategorySubmitted(LoadGameData);
        gameHubConnectionService.RegisterOnGameStarted(LoadGameData);
        gameHubConnectionService.RegisterOnClueSelected(LoadGameData);
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

    private async Task SelectClue(Clue clue)
    {
        if (!gameHubConnectionService.IsConnected) return;

        throw new NotImplementedException();
        await LoadGameData();
    }

    private async Task OnCategorySaved()
    {
        await LoadGameData();
    }

    public async ValueTask DisposeAsync()
    {
        await gameHubConnectionService.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}