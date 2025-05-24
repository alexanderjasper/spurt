using Spurt.Data.Commands;
using Spurt.Data.Queries;
using System.Linq;
using Spurt.Domain.Categories;

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
            var buzzedPlayer = game.Players.FirstOrDefault(p => p.Id == game.BuzzedPlayerId);
            if (buzzedPlayer != null)
            {
                game.SelectedClue.AnsweredByPlayerId = buzzedPlayer.Id;
                game.SelectedClue.AnsweredByPlayer = buzzedPlayer;
                buzzedPlayer.AnsweredClues.Add(game.SelectedClue);
                
                // TODO: If BuzzedPlayer is the only one with remaining clues, select the lowest value clue from BuzzedPlayer
                // TODO: If no clues left, progress to next game state
                game.CurrentChoosingPlayerId = game.BuzzedPlayerId;

                var allClues = new List<Clue>();
                foreach (var player in game.Players)
                {
                    if (player.Category != null)
                    {
                        allClues.AddRange(player.Category.Clues);
                    }
                }
                
                bool allAnswered = true;
                foreach (var c in allClues)
                {
                    if (!c.IsAnswered)
                    {
                        allAnswered = false;
                        break;
                    }
                }
                
                if (allAnswered)
                {
                    game.State = GameState.Finished;
                }
                else
                {
                    game.CurrentChoosingPlayerId = game.BuzzedPlayerId;
                    game.State = GameState.InProgress;
                }
                
                game.SelectedClue = null;
                game.SelectedClueId = null;
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