using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Spurt.Data.Queries;

namespace Spurt.Components.Pages;

public partial class Game(ILocalStorageService localStorage, NavigationManager navigation, IGetGame getGame)
{
    [Parameter] public required string Code { get; init; }

    private Domain.Games.Game? CurrentGame { get; set; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;

        var playerId = await localStorage.GetItemAsync<Guid?>("PlayerId");
        if (playerId == null)
        {
            navigation.NavigateTo("/registerplayer");
            return;
        }

        CurrentGame = await getGame.Execute(Code);

        if (CurrentGame == null) navigation.NavigateTo("/");
        StateHasChanged();
    }
}