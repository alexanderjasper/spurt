using Microsoft.AspNetCore.Components;
using Spurt.Domain.Games;
using Spurt.Domain.Players;

namespace Spurt.Components.Shared;

public partial class GameWaitingForCategoriesView
{
    [Parameter] public required Game Game { get; set; }
    [Parameter] public Player? CurrentPlayer { get; set; }
    [Parameter] public EventCallback OnCategorySaved { get; set; }
    [Parameter] public EventCallback OnStartGame { get; set; }
} 