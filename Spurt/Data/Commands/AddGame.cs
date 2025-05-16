using Spurt.Domain.Games;

namespace Spurt.Data.Commands;

public class AddGame(AppDbContext dbContext) : IAddGame
{
    public async Task Execute(Game game)
    {
        game.ValidateCreator();
        await dbContext.Games.AddAsync(game);
        await dbContext.SaveChangesAsync();
    }
}

public interface IAddGame
{
    Task Execute(Game game);
}