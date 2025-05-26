using Spurt.Data.Commands;
using Spurt.Data.Queries;
using Spurt.Domain.Categories;

namespace Spurt.Domain.Games.Commands;

public class NoOneCanAnswer(
    IGetGame getGame,
    IUpdateGame updateGame,
    IGameHubNotificationService notificationService) : INoOneCanAnswer
{
    public async Task<Game> Execute(string gameCode, Guid judgingPlayerId)
    {
        var game = await getGame.Execute(gameCode);
        if (game == null)
            throw new InvalidOperationException($"Game with code {gameCode} not found.");
        if (game.State != GameState.ClueSelected)
            throw new InvalidOperationException("Cannot mark clue as unanswered when no clue is selected.");
        if (game.SelectedClue == null)
            throw new InvalidOperationException("No clue is currently selected.");

        var clueCategory = game.SelectedClue.Category;
        if (clueCategory.PlayerId != judgingPlayerId)
            throw new InvalidOperationException("Only the owner of the clue can mark it as unanswered.");

        var clueOwner = game.Players.FirstOrDefault(p => p.Id == judgingPlayerId);
        if (clueOwner != null)
        {
            game.SelectedClue.NoOneCouldAnswer = true;
            game.SelectedClue.AnsweredByPlayerId = clueOwner.Id;
            game.SelectedClue.AnsweredByPlayer = clueOwner;
            clueOwner.AnsweredClues.Add(game.SelectedClue);
        }

        if (game.Players.SelectMany(p => p.Category!.Clues).All(c => c.IsAnswered))
        {
            game.State = GameState.Finished;
        }
        else
        {
            game.CurrentChoosingPlayerId = DetermineNextChoosingPlayer(game, judgingPlayerId);
            game.State = GameState.InProgress;
        }

        game.SelectedClue = null;
        game.SelectedClueId = null;
        game.BuzzedPlayerId = null;
        game.BuzzedPlayer = null;
        game.BuzzedTime = null;

        var result = await updateGame.Execute(game);
        await notificationService.NotifyGameUpdated(result.Code);

        return result;
    }

    private static Guid DetermineNextChoosingPlayer(Game game, Guid currentPlayerId)
    {
        var otherPlayersWithUnansweredClues = game.Players
            .Where(p => p.Id != currentPlayerId && p.Category!.Clues.Any(c => !c.IsAnswered))
            .ToList();

        if (otherPlayersWithUnansweredClues.Count > 0)
        {
            var random = new Random();
            return otherPlayersWithUnansweredClues[random.Next(otherPlayersWithUnansweredClues.Count)].Id;
        }

        var otherPlayers = game.Players.Where(p => p.Id != currentPlayerId).ToList();
        if (otherPlayers.Count <= 0)
            return currentPlayerId;

        var randomPlayer = new Random();
        return otherPlayers[randomPlayer.Next(otherPlayers.Count)].Id;
    }
}

public interface INoOneCanAnswer
{
    Task<Game> Execute(string gameCode, Guid judgingPlayerId);
}