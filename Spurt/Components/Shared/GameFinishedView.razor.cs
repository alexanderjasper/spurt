using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components;
using Spurt.Domain.Games;
using Spurt.Domain.Players;

namespace Spurt.Components.Shared;

public partial class GameFinishedView(NavigationManager NavigationManager)
{
    [Parameter] [Required] public required Game Game { get; set; }

    [Parameter] [Required] public Player? CurrentPlayer { get; set; }
    
    private void GoToHome() => NavigationManager.NavigateTo("/");
}