using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Spurt.Data.Queries;
using Spurt.Domain.Games.Commands;
using Spurt.Domain.Users;

namespace Spurt.Components.Pages;

public partial class Home(
    IGetUser getUser,
    IGetActiveGame getActiveGame,
    ICreateGame createGame,
    IJoinGame joinGame,
    ILocalStorageService localStorage,
    NavigationManager navigation)
{
    private User? CurrentUser { get; set; }
    private bool IsLoading { get; set; }
    private string GameCode { get; set; } = string.Empty;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;

        if (!await LoadCurrentUser())
            return;

        await CheckForActiveGame();
    }

    private async Task<bool> LoadCurrentUser()
    {
        var userId = await localStorage.GetItemAsync<Guid?>("UserId");
        if (userId == null)
        {
            NavigateToUserRegistration();
            return false;
        }

        CurrentUser = await getUser.Execute(userId.Value);
        if (CurrentUser == null)
        {
            NavigateToUserRegistration();
            return false;
        }

        return true;
    }

    private void NavigateToUserRegistration()
    {
        navigation.NavigateTo("/register");
    }

    private async Task CheckForActiveGame()
    {
        SetLoading(true);

        var activeGame = await getActiveGame.Execute(CurrentUser!.Id);
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
        if (CurrentUser == null) return;

        SetLoading(true);

        var game = await createGame.Execute(CurrentUser.Id);
        NavigateToGame(game);
    }

    private async Task JoinGame()
    {
        if (CurrentUser == null || string.IsNullOrWhiteSpace(GameCode)) return;

        SetLoading(true);

        try
        {
            var game = await joinGame.Execute(GameCode.Trim().ToUpper(), CurrentUser.Id);
            NavigateToGame(game);
        }
        catch
        {
            SetLoading(false);
        }
    }
}