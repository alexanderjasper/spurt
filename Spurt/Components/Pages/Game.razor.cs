using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
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
    ISelectClue selectClue,
    IGameHubConnectionService gameHubConnectionService) : IAsyncDisposable
{
    [Parameter] public required string Code { get; init; }

    private Domain.Games.Game? CurrentGame { get; set; }
    private Guid? _currentUserId;
    private Player? _currentPlayer;
    private string? ErrorMessage { get; set; }

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
        gameHubConnectionService.RegisterOnGameUpdated(UpdateGameData);
    }

    private async Task UpdateGameData(Domain.Games.Game updatedGame)
    {
        CurrentGame = updatedGame;
        _currentPlayer = CurrentGame.Players.FirstOrDefault(p => p.UserId == _currentUserId);
        await InvokeAsync(StateHasChanged);
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

        await InvokeAsync(StateHasChanged);
    }

    private async Task StartGame()
    {
        if (_currentUserId == null) return;

        try
        {
            ErrorMessage = null;
            CurrentGame = await startGame.Execute(Code, _currentUserId.Value);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    private void ClearError()
    {
        ErrorMessage = null;
    }

    private async Task SelectClue(Clue clue)
    {
        if (CurrentGame == null) throw new InvalidOperationException("Game not loaded");
        await selectClue.Execute(CurrentGame.Code, clue.Id);
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