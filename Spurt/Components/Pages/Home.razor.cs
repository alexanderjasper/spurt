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
    private bool IsCreatingGame { get; set; }
    private bool IsJoiningGame { get; set; }
    private bool IsCheckingForExistingGames { get; set; }
    private string GameCode { get; set; } = string.Empty;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;

        var playerId = await localStorage.GetItemAsync<Guid?>("PlayerId");
        if (playerId == null)
        {
            navigation.NavigateTo("/registerplayer");
            return;
        }

        CurrentPlayer = await getPlayer.Execute(playerId.Value);
        if (CurrentPlayer == null) navigation.NavigateTo("/registerplayer");

        IsCheckingForExistingGames = true;
        StateHasChanged();

        var activeGame = await getActiveGame.Execute(CurrentPlayer.Id);
        if (activeGame != null)
        {
            navigation.NavigateTo($"/game/{activeGame.Code}");
            return;
        }

        IsCheckingForExistingGames = false;
        StateHasChanged();
    }

    private async Task CreateNewGame()
    {
        if (CurrentPlayer == null) return;

        IsCreatingGame = true;
        StateHasChanged();

        var game = await createGame.Execute(CurrentPlayer.Id);
        navigation.NavigateTo($"/game/{game.Code}");
    }

    private async Task JoinGame()
    {
        if (CurrentPlayer == null || string.IsNullOrWhiteSpace(GameCode)) return;

        IsJoiningGame = true;
        StateHasChanged();

        try
        {
            var game = await joinGame.Execute(GameCode.Trim().ToUpper(), CurrentPlayer.Id);
            navigation.NavigateTo($"/game/{game.Code}");
        }
        catch
        {
            IsJoiningGame = false;
            StateHasChanged();
        }
    }
}