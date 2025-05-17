using Microsoft.AspNetCore.Components;
using Spurt.Domain.Games;
using Spurt.Domain.Players;

namespace Spurt.Components.Shared;

public partial class ClueSelectedView
{
    [Parameter] public required Game Game { get; set; }
    [Parameter] public Player? CurrentPlayer { get; set; }
    [Parameter] public required EventCallback OnBuzz { get; set; }
}