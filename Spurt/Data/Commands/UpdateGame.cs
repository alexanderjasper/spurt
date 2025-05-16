using Microsoft.EntityFrameworkCore;
using Spurt.Domain.Games;

namespace Spurt.Data.Commands;

public class UpdateGame(AppDbContext dbContext) : IUpdateGame
{
    public async Task Execute(Game game)
    {
        var gameEntity = await dbContext.Games
            .Include(g => g.Players)
            .FirstOrDefaultAsync(g => g.Code == game.Code);
            
        if (gameEntity == null)
            throw new InvalidOperationException($"Game with code {game.Code} not found");
            
        gameEntity.State = game.State;
        gameEntity.CurrentChoosingPlayerId = game.CurrentChoosingPlayerId;
        
        await dbContext.SaveChangesAsync();
    }
}

public interface IUpdateGame
{
    Task Execute(Game game);
} 