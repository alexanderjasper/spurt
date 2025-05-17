using Microsoft.EntityFrameworkCore;
using Spurt.Domain.Games;

namespace Spurt.Data.Commands;

public class UpdateGame(AppDbContext dbContext) : IUpdateGame
{
    public async Task Execute(Game game)
    {
        dbContext.Update(game);
        await dbContext.SaveChangesAsync();
    }
}

public interface IUpdateGame
{
    Task Execute(Game game);
}