using Spurt.Data.Commands;
using Spurt.Data.Queries;

namespace Spurt.Domain.Games.Commands;

public class SelectClue(
    IGetGame getGame,
    IGetClue getClue,
    IUpdateGame updateGame,
    IGameHubNotificationService notificationService) : ISelectClue
{
    public async Task<Game> Execute(string gameCode, Guid clueId)
    {
        var game = await getGame.Execute(gameCode);

        if (game == null)
            throw new InvalidOperationException("Spillet findes ikke");

        if (game.State != GameState.InProgress)
            throw new InvalidOperationException("Spillet er ikke i gang");

        var clue = await getClue.Execute(clueId);
        if (clue == null || clue.IsAnswered)
            throw new InvalidOperationException("Ledetråden er allerede besvaret");

        game.SelectedClue = clue;
        game.SelectedClueId = clueId;

        game.State = GameState.ClueSelected;

        var result = await updateGame.Execute(game);
        await notificationService.NotifyGameUpdated(result.Code);

        return game;
    }
}

public interface ISelectClue
{
    Task<Game> Execute(string gameCode, Guid clueId);
}