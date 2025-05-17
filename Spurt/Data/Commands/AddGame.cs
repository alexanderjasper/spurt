using Spurt.Domain.Games;

namespace Spurt.Data.Commands;

public class AddGame(AppDbContext dbContext) : IAddGame
{
    public async Task<Game> Execute(Game game)
    {
        game.ValidateCreator();
        var result = await dbContext.Games.AddAsync(game);
        await dbContext.SaveChangesAsync();
        return result.Entity;
    }
}

public interface IAddGame
{
    Task<Game> Execute(Game game);
}