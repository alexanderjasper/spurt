using System.ComponentModel.DataAnnotations;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Spurt.Domain.Player.Commands;

namespace Spurt.Components.Pages;

public partial class RegisterPlayer(IRegisterPlayer registerPlayer, ILocalStorageService localStorageService)
    : ComponentBase
{
    private class PlayerModel
    {
        [Required(ErrorMessage = "Indtast dit navn.")]
        [StringLength(20, MinimumLength = 2, ErrorMessage = "Navnet skal være mellem 2 og 20 tegn.")]
        public string? Name { get; set; }
    }

    [SupplyParameterFromForm] private PlayerModel? Model { get; set; }

    protected override void OnInitialized()
    {
        Model ??= new PlayerModel();
    }

    private async Task Submit()
    {
        if (Model == null || string.IsNullOrWhiteSpace(Model.Name)) return;

        var player = await registerPlayer.Execute(Model.Name);

        await localStorageService.SetItemAsync("PlayerId", player.Id);
    }
}