using Microsoft.AspNetCore.Components;
using Spurt.Domain.Players;

namespace Spurt.Components.Shared;

public partial class PlayerList
{
    [Parameter] public required IReadOnlyList<Player> Players { get; set; }
} 