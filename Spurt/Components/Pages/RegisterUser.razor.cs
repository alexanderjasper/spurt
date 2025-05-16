using System.ComponentModel.DataAnnotations;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Spurt.Domain.Users.Commands;

namespace Spurt.Components.Pages;

public partial class RegisterUser(
    IRegisterUser registerUser,
    ILocalStorageService localStorageService,
    NavigationManager navigationManager)
    : ComponentBase
{
    private class UserModel
    {
        [Required(ErrorMessage = "Indtast dit navn.")]
        [StringLength(20, MinimumLength = 2, ErrorMessage = "Navnet skal være mellem 2 og 20 tegn.")]
        public string? Name { get; set; }
    }

    [SupplyParameterFromForm] private UserModel? Model { get; set; }

    protected override void OnInitialized()
    {
        Model ??= new UserModel();
    }

    private async Task Submit()
    {
        if (Model == null || string.IsNullOrWhiteSpace(Model.Name)) return;

        var user = await registerUser.Execute(Model.Name);

        await localStorageService.SetItemAsync("UserId", user.Id);
        navigationManager.NavigateTo("/");
    }
}