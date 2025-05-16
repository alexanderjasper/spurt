using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Spurt.Data.Queries;
using Spurt.Domain.Games.Commands;
using Spurt.Domain.Players;

namespace Spurt.Components.Pages;

public partial class Home(
    IGetPlayer getPlayer,
    IGetActiveGame getActiveGame,
    ICreateGame createGame,
    IJoinGame joinGame,
    ILocalStorageService localStorage,
    NavigationManager navigation)
{
    private Player? CurrentPlayer { get; set; }
    private bool IsLoading { get; set; }
    private string GameCode { get; set; } = string.Empty;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;

        if (!await LoadCurrentPlayer())
            return;

        await CheckForActiveGame();
    }

    private async Task<bool> LoadCurrentPlayer()
    {
        var playerId = await localStorage.GetItemAsync<Guid?>("PlayerId");
        if (playerId == null)
        {
            NavigateToPlayerRegistration();
            return false;
        }

        CurrentPlayer = await getPlayer.Execute(playerId.Value);
        if (CurrentPlayer == null)
        {
            NavigateToPlayerRegistration();
            return false;
        }

        return true;
    }

    private void NavigateToPlayerRegistration()
    {
        navigation.NavigateTo("/registerplayer");
    }

    private async Task CheckForActiveGame()
    {
        SetLoading(true);

        var activeGame = await getActiveGame.Execute(CurrentPlayer!.Id);
        if (activeGame != null)
        {
            NavigateToGame(activeGame);
            return;
        }

        SetLoading(false);
    }

    private void NavigateToGame(Domain.Games.Game game)
    {
        navigation.NavigateTo($"/game/{game.Code}");
    }

    private void SetLoading(bool isLoading)
    {
        IsLoading = isLoading;
        StateHasChanged();
    }

    private async Task CreateNewGame()
    {
        if (CurrentPlayer == null) return;

        SetLoading(true);

        var game = await createGame.Execute(CurrentPlayer.Id);
        NavigateToGame(game);
    }

    private async Task JoinGame()
    {
        if (CurrentPlayer == null || string.IsNullOrWhiteSpace(GameCode)) return;

        SetLoading(true);

        try
        {
            var game = await joinGame.Execute(GameCode.Trim().ToUpper(), CurrentPlayer.Id);
            NavigateToGame(game);
        }
        catch
        {
            SetLoading(false);
        }
    }
}