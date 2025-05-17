using Spurt.Data.Commands;
using Spurt.Data.Queries;

namespace Spurt.Domain.Games.Commands;

public class JudgeAnswer(
    IGetGame getGame,
    IUpdateGame updateGame,
    IGameHubNotificationService notificationService) : IJudgeAnswer
{
    public async Task<Game> Execute(string gameCode, Guid judgingPlayerId, bool isCorrect)
    {
        var game = await getGame.Execute(gameCode);

        if (game == null)
            throw new InvalidOperationException($"Game with code {gameCode} not found.");

        if (game.State != GameState.BuzzerPressed)
            throw new InvalidOperationException("Cannot judge answer when buzzer has not been pressed.");

        if (game.SelectedClue == null)
            throw new InvalidOperationException("No clue is currently selected.");

        if (game.BuzzedPlayerId == null)
            throw new InvalidOperationException("No player has buzzed in.");

        var clueCategory = game.SelectedClue.Category;
        if (clueCategory.PlayerId != judgingPlayerId)
            throw new InvalidOperationException("Only the owner of the clue can judge the answer.");

        if (isCorrect)
        {
            var playerToUpdate = game.Players.FirstOrDefault(p => p.Id == game.BuzzedPlayerId);
            if (playerToUpdate != null)
            {
                playerToUpdate.Score += game.SelectedClue.PointValue;
                game.CurrentChoosingPlayerId = game.BuzzedPlayerId;
                game.SelectedClue.IsAnswered = true;
                game.SelectedClue = null;
                game.SelectedClueId = null;

                game.State = GameState.InProgress;
            }
        }
        else
        {
            game.State = GameState.ClueSelected;
        }

        game.BuzzedPlayerId = null;
        game.BuzzedPlayer = null;
        game.BuzzedTime = null;


        var result = await updateGame.Execute(game);
        await notificationService.NotifyGameUpdated(result);

        return result;
    }
}

public interface IJudgeAnswer
{
    Task<Game> Execute(string gameCode, Guid judgingPlayerId, bool isCorrect);
}