using Microsoft.AspNetCore.Components;
using Spurt.Domain.Categories;
using Spurt.Domain.Games;
using Spurt.Domain.Players;

namespace Spurt.Components.Shared;

public partial class GameInProgressView
{
    [Parameter] public required Game Game { get; set; }
    [Parameter] public Player? CurrentPlayer { get; set; }
    [Parameter] public EventCallback<Clue> OnClueSelected { get; set; }
}