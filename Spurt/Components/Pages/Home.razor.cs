using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Spurt.Data.Queries;
using Spurt.Domain.Players;

namespace Spurt.Components.Pages;

public partial class Home(
    ILocalStorageService localStorageService,
    NavigationManager navigationManager,
    IGetPlayer getPlayer)
{
    private Player? CurrentPlayer { get; set; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;

        var playerId = await localStorageService.GetItemAsync<Guid?>("PlayerId");
        if (playerId == null)
        {
            navigationManager.NavigateTo("/registerplayer");
            return;
        }

        CurrentPlayer = await getPlayer.Execute(playerId.Value);
        if (CurrentPlayer == null) navigationManager.NavigateTo("/registerplayer");
    }
}