using Spurt.Domain.Games;

namespace Spurt.Data.Commands;

public class UpdateGame(AppDbContext dbContext) : IUpdateGame
{
    public async Task<Game> Execute(Game game)
    {
        var result = dbContext.Update(game);
        await dbContext.SaveChangesAsync();
        return result.Entity;
    }
}

public interface IUpdateGame
{
    Task<Game> Execute(Game game);
}