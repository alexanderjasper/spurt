using Spurt.Data.Commands;
using Spurt.Data.Queries;
using Spurt.Domain.Players;

namespace Spurt.Domain.Games.Commands;

public class CreateGame(IAddGame addGame, IGetUser getUser) : ICreateGame
{
    private static readonly Random Random = new();

    public async Task<Game> Execute(Guid userId)
    {
        var user = await getUser.Execute(userId) ??
                   throw new ArgumentException("User not found", nameof(userId));

        var game = new Game
        {
            Code = GenerateUniqueCode(),
        };
        var player = new Player
        {
            IsCreator = true,
            User = user,
            UserId = userId,
            Game = game,
            GameId = game.Id,
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

public interface ICreateGame
{
    Task<Game> Execute(Guid userId);
}