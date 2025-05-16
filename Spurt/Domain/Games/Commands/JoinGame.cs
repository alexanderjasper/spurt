using Microsoft.AspNetCore.SignalR;
using Spurt.Data.Commands;
using Spurt.Data.Queries;

namespace Spurt.Domain.Games.Commands;

public class JoinGame(
    IGetGame getGame,
    IGetPlayer getPlayer,
    IUpdateGame updateGame,
    IHubContext<GameHub> hubContext) : IJoinGame
{
    public async Task<Game> Execute(string gameCode, Guid playerId)
    {
        var game = await getGame.Execute(gameCode) ??
                   throw new ArgumentException("Game not found", nameof(gameCode));

        var player = await getPlayer.Execute(playerId) ??
                     throw new ArgumentException("Player not found", nameof(playerId));

        if (game.Players.Any(p => p.Id == playerId)) return game;

        game.Players.Add(player);

        await updateGame.Execute(game);
        await hubContext.Clients.Group(gameCode).SendAsync(GameHub.Events.PlayerJoined, player.Name);

        return game;
    }
}

public interface IJoinGame
{
    Task<Game> Execute(string gameCode, Guid playerId);
}