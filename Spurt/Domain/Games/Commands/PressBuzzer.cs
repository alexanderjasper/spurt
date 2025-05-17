using Spurt.Data.Commands;
using Spurt.Data.Queries;

namespace Spurt.Domain.Games.Commands;

public class PressBuzzer(IGetGame getGame, IUpdateGame updateGame) : IPressBuzzer
{
    public async Task<Game> Execute(string gameCode, Guid playerId)
    {
        var game = await getGame.Execute(gameCode);
        if (game == null)
            throw new InvalidOperationException($"Game with code {gameCode} not found.");

        if (game.State != GameState.ClueSelected)
            throw new InvalidOperationException("Buzzer cannot be pressed in the current game state.");

        if (game.SelectedClue == null)
            throw new InvalidOperationException("No clue is currently selected.");

        var player = game.Players.FirstOrDefault(p => p.Id == playerId);
        if (player == null)
            throw new InvalidOperationException("Player not found in this game.");

        var clueCategory = game.SelectedClue.Category;
        if (clueCategory.PlayerId == player.Id)
            throw new InvalidOperationException("You cannot buzz for your own clue.");

        if (game.BuzzedPlayerId != null) return game;

        game.BuzzedPlayerId = player.Id;
        game.BuzzedPlayer = player;
        game.BuzzedTime = DateTime.UtcNow;
        game.State = GameState.BuzzerPressed;

        await updateGame.Execute(game);

        return game;
    }
}

public interface IPressBuzzer
{
    Task<Game> Execute(string gameCode, Guid playerId);
}