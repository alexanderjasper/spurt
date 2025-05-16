using Microsoft.EntityFrameworkCore;
using Spurt.Domain.Games;

namespace Spurt.Data.Queries;

public class GetActiveGame(AppDbContext dbContext) : IGetActiveGame
{
    public async Task<Game?> Execute(Guid playerId)
    {
        return await dbContext.Games
            .Include(g => g.Players)
            .Include(g => g.Creator)
            .Where(g => g.Players.Any(p => p.Id == playerId) || g.CreatorId == playerId)
            .OrderByDescending(g => g.CreatedAt)
            .FirstOrDefaultAsync();
    }
}

public interface IGetActiveGame
{
    Task<Game?> Execute(Guid playerId);
} 