using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Spurt.Data.Commands;
using Spurt.Data.Queries;
using Spurt.Domain.Categories;
using Spurt.Domain.Games;
using Spurt.Domain.Games.Commands;
using Spurt.Domain.Players;

namespace Spurt.Components.Pages;

// TODO: Players should not be able to chooes clues from their own category
// TODO: If choosing player is the only one with remaining clues, let another player choose a clue
// TODO: Add negative scoring if no one can answer the question
// TODO: Add an option to leave the game (confirmation dialog)
// TODO: Add an option to kick a player. Confirmation dialog. Perhaps all should agree? If multiple players are kicked, only users not up for kicking have to agree.
// TODO: Add negative scoring for incorrect answers?
// TODO: Handle disconnected players? Auto-skip turn?
// TODO: Let the next player(s) answer if the quickest player can't answer correctly
// TODO: Make game expire after X time of inactivity
// TODO: Option to reveal the correct answer after answering or if no one can answer
// TODO: Timeout for answering?
// TODO: Other timeouts?
// TODO: Spectator mode?
// TODO: Add sounds for buzzer press
// TODO: Add sounds for correct/incorrect answers
// TODO: Add option to continue to a new round after all clues are answered
// TODO: Add game statistics at the end (fastest answer, most points in one turn, etc.)
// TODO: Add visual indicator for whose turn it is to choose a clue
// TODO: Add game options/settings that can be configured before starting (timer duration, scoring rules, etc.)
// TODO: Consider randomizing the first player to choose a clue
// TODO: Add option for tournament mode with multiple rounds

public partial class Game(
    ILocalStorageService localStorage,
    NavigationManager navigation,
    IGetGame getGame,
    IStartGame startGame,
    ISelectClue selectClue,
    IPressBuzzer pressBuzzer,
    IClearContext clearContext,
    IGameHubConnectionService gameHubConnectionService) : IAsyncDisposable
{
    [Parameter] public required string Code { get; init; }

    private Domain.Games.Game? CurrentGame { get; set; }
    private Guid? _currentUserId;
    private Player? _currentPlayer;

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
        gameHubConnectionService.RegisterOnGameUpdated(LoadGameData);
    }

    private async Task LoadGameData()
    {
        clearContext.Execute();
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

        await startGame.Execute(Code, _currentUserId.Value);
    }

    private async Task SelectClue(Clue clue)
    {
        if (CurrentGame == null) throw new InvalidOperationException("Game not loaded");
        await selectClue.Execute(CurrentGame.Code, clue.Id);
    }

    private async Task PressBuzz()
    {
        if (CurrentGame == null) throw new InvalidOperationException("Game not loaded");
        if (_currentPlayer == null) throw new InvalidOperationException("Player not found");

        await pressBuzzer.Execute(CurrentGame.Code, _currentPlayer.Id);
    }

    public async ValueTask DisposeAsync()
    {
        await gameHubConnectionService.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}