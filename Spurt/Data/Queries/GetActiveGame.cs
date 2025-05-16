using Microsoft.EntityFrameworkCore;
using Spurt.Domain.Games;

namespace Spurt.Data.Queries;

public class GetActiveGame(AppDbContext dbContext) : IGetActiveGame
{
    public async Task<Game?> Execute(Guid userId)
    {
        return await dbContext.Games
            .Include(g => g.Players)
            .ThenInclude(p => p.User)
            .Where(g => g.Players.Any(p => p.UserId == userId))
            .OrderByDescending(g => g.CreatedAt)
            .FirstOrDefaultAsync();
    }
}

public interface IGetActiveGame
{
    Task<Game?> Execute(Guid userId);
}