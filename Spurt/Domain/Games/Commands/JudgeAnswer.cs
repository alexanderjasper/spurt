using Spurt.Data.Commands;
using Spurt.Data.Queries;
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

                var allClues = new List<Clue>();
                foreach (var player in game.Players)
                    allClues.AddRange(player.Category!.Clues);

                if (allClues.All(c => c.IsAnswered))
                {
                    game.State = GameState.Finished;
                }
                else
                {
                    game.CurrentChoosingPlayerId = DetermineNextChoosingPlayer(game, game.BuzzedPlayerId.Value);
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
        await notificationService.NotifyGameUpdated(result.Code);

        return result;
    }

    private static Guid DetermineNextChoosingPlayer(Game game, Guid preferredPlayerId)
    {
        var otherPlayersWithUnansweredClues = game.Players
            .Where(p => p.Id != preferredPlayerId && p.Category!.Clues.Any(c => !c.IsAnswered))
            .ToList();

        if (otherPlayersWithUnansweredClues.Count > 0)
            return preferredPlayerId;

        var otherPlayers = game.Players.Where(p => p.Id != preferredPlayerId).ToList();
        if (otherPlayers.Count <= 0)
            return preferredPlayerId;

        var random = new Random();
        return otherPlayers[random.Next(otherPlayers.Count)].Id;
    }
}

public interface IJudgeAnswer
{
    Task<Game> Execute(string gameCode, Guid judgingPlayerId, bool isCorrect);
}