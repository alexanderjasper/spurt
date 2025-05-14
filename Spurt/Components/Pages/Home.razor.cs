using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Spurt.Data.Queries;
using Spurt.Domain.Games.Commands;
using Spurt.Domain.Players;

namespace Spurt.Components.Pages;

public partial class Home(
    IGetPlayer getPlayer,
    ICreateGame createGame,
    ILocalStorageService localStorage,
    NavigationManager navigation)
{
    private Player? CurrentPlayer { get; set; }
    private bool IsCreatingGame { get; set; }

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
}