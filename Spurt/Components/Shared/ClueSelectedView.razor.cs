using Microsoft.AspNetCore.Components;
using Spurt.Domain.Games;
using Spurt.Domain.Games.Commands;
using Spurt.Domain.Players;

namespace Spurt.Components.Shared;

public partial class ClueSelectedView(INoOneCanAnswer noOneCanAnswer)
{
    [Parameter] public required Game Game { get; set; }
    [Parameter] public Player? CurrentPlayer { get; set; }
    [Parameter] public required EventCallback OnBuzz { get; set; }

    private async Task OnNoOneCanAnswer()
    {
        if (CurrentPlayer?.Id == null || Game.SelectedClue?.Category.PlayerId != CurrentPlayer.Id)
            return;

        await noOneCanAnswer.Execute(Game.Code, CurrentPlayer.Id);
    }
}