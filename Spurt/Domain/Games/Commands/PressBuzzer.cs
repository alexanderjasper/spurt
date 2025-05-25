using System.Collections.Concurrent;
using Spurt.Data.Commands;
using Spurt.Data.Queries;

namespace Spurt.Domain.Games.Commands;

public class PressBuzzer(
    IGetGame getGame,
    IUpdateGame updateGame,
    IGameHubNotificationService notificationService) : IPressBuzzer
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> _gameLocks = new();

    public async Task<Game> Execute(string gameCode, Guid playerId)
    {
        var gameLock = _gameLocks.GetOrAdd(gameCode, _ => new SemaphoreSlim(1, 1));

        try
        {
            await gameLock.WaitAsync();

            var game = await getGame.Execute(gameCode);
            var untrackedGame = await getGame.Execute(gameCode, false);

            if (game == null || untrackedGame == null)
                throw new InvalidOperationException($"Game with code {gameCode} not found.");

            if (untrackedGame.State != GameState.ClueSelected && untrackedGame.State != GameState.BuzzerPressed)
                throw new InvalidOperationException("Buzzer cannot be pressed in the current game state.");

            if (untrackedGame.SelectedClue == null)
                throw new InvalidOperationException("No clue is currently selected.");

            var player = untrackedGame.Players.FirstOrDefault(p => p.Id == playerId);
            if (player == null)
                throw new InvalidOperationException("Player not found in this game.");

            var clueCategory = untrackedGame.SelectedClue.Category;
            if (clueCategory.PlayerId == player.Id)
                throw new InvalidOperationException("You cannot buzz for your own clue.");

            if (untrackedGame.BuzzedPlayerId != null) return game;

            var buzzerTimestamp = DateTime.UtcNow;

            game.BuzzedPlayerId = player.Id;
            game.BuzzedPlayer = player;
            game.BuzzedTime = buzzerTimestamp;
            game.State = GameState.BuzzerPressed;

            var result = await updateGame.Execute(game);
            await notificationService.NotifyGameUpdated(result.Code);

            return result;
        }
        finally
        {
            gameLock.Release();
        }
    }
}

public interface IPressBuzzer
{
    Task<Game> Execute(string gameCode, Guid playerId);
}