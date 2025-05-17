using Microsoft.AspNetCore.Components;
using Spurt.Domain.Games;
using Spurt.Domain.Games.Commands;
using Spurt.Domain.Players;

namespace Spurt.Components.Shared;

public partial class BuzzerPressedView(IJudgeAnswer judgeAnswer)
{
    [Parameter] public required Game Game { get; set; }
    [Parameter] public Player? CurrentPlayer { get; set; }

    private bool IsClueOwner =>
        CurrentPlayer?.Id != null &&
        Game.SelectedClue?.Category.PlayerId == CurrentPlayer.Id;

    private async Task JudgeAnswer(bool isCorrect)
    {
        if (!IsClueOwner)
            return;

        await judgeAnswer.Execute(Game.Code, CurrentPlayer!.Id, isCorrect);
    }
}