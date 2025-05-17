using Microsoft.EntityFrameworkCore;
using Spurt.Domain.Games;

namespace Spurt.Data.Queries;

public class GetGame(AppDbContext dbContext) : IGetGame
{
    public async Task<Game?> Execute(string code, bool withTracking = true)
    {
        var query = dbContext.Games
            .Include(g => g.Players)
            .ThenInclude(p => p.User)
            .Include(g => g.Players)
            .ThenInclude(p => p.Category)
            .ThenInclude(c => c!.Clues)
            .Include(g => g.SelectedClue)
            .Include(g => g.BuzzedPlayer)
            .AsQueryable();

        if (!withTracking) query = query.AsNoTracking();

        return await query.FirstOrDefaultAsync(g => g.Code == code);
    }
}

public interface IGetGame
{
    Task<Game?> Execute(string code, bool withTracking = true);
}