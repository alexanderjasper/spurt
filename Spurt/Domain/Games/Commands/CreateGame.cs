using Spurt.Data.Commands;
using Spurt.Data.Queries;

namespace Spurt.Domain.Games.Commands;

public interface ICreateGame
{
    Task<Game> Execute(Guid playerId);
}

public class CreateGame(IAddGame addGame, IGetPlayer getPlayer) : ICreateGame
{
    private static readonly Random Random = new();

    public async Task<Game> Execute(Guid playerId)
    {
        var player = await getPlayer.Execute(playerId) ??
                     throw new ArgumentException("Players not found", nameof(playerId));

        var game = new Game
        {
            Code = GenerateUniqueCode(),
            Creator = player,
            CreatorId = player.Id,
        };

        game.Players.Add(player);

        await addGame.Execute(game);

        return game;
    }

    private static string GenerateUniqueCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        return new string(Enumerable.Range(0, 6)
            .Select(_ => chars[Random.Next(chars.Length)])
            .ToArray());
    }
}