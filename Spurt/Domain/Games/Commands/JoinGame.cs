using Spurt.Data.Commands;
using Spurt.Data.Queries;
using Spurt.Domain.Players;

namespace Spurt.Domain.Games.Commands;

public class JoinGame(
    IGetGame getGame,
    IGetUser getUser,
    IAddPlayer addPlayer,
    IGameHubNotificationService gameHubNotificationService) : IJoinGame
{
    public async Task<Game> Execute(string gameCode, Guid userId)
    {
        var game = await getGame.Execute(gameCode) ??
                   throw new ArgumentException("Game not found", nameof(gameCode));

        var user = await getUser.Execute(userId) ??
                   throw new ArgumentException("User not found", nameof(userId));

        var existingPlayer = game.Players.FirstOrDefault(p => p.UserId == userId);
        if (existingPlayer != null) return game;

        var player = new Player
        {
            User = user,
            UserId = userId,
            Game = game,
            GameId = game.Id,
        };

        game.Players.Add(player);
        await addPlayer.Execute(player);

        game = await getGame.Execute(gameCode) ??
               throw new InvalidOperationException("Game not found after adding player");
        await gameHubNotificationService.NotifyGameUpdated(game.Code);

        return game;
    }
}

public interface IJoinGame
{
    Task<Game> Execute(string gameCode, Guid userId);
}