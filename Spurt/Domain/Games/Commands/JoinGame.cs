using Microsoft.AspNetCore.SignalR;
using Spurt.Data.Commands;
using Spurt.Data.Queries;
using Spurt.Domain.Players;

namespace Spurt.Domain.Games.Commands;

public class JoinGame(
    IGetGame getGame,
    IGetUser getUser,
    IAddPlayer addPlayer,
    IHubContext<GameHub> hubContext) : IJoinGame
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

        await addPlayer.Execute(player);
        await hubContext.Clients.Group(gameCode).SendAsync(GameHub.Events.PlayerJoined);

        return game;
    }
}

public interface IJoinGame
{
    Task<Game> Execute(string gameCode, Guid userId);
}