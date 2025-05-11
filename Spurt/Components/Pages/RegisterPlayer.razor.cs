using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components;

namespace Spurt.Components.Pages;

public partial class RegisterPlayer : ComponentBase
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

    private void Submit()
    {
        if (Model == null || string.IsNullOrWhiteSpace(Model.Name)) return;
    }
}