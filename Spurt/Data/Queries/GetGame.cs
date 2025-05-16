using Microsoft.EntityFrameworkCore;
using Spurt.Domain.Games;

namespace Spurt.Data.Queries;

public class GetGame(AppDbContext dbContext) : IGetGame
{
    public async Task<Game?> Execute(string code)
    {
        return await dbContext.Games
            .Include(g => g.Players)
            .ThenInclude(p => p.User)
            .FirstOrDefaultAsync(g => g.Code == code);
    }
}

public interface IGetGame
{
    Task<Game?> Execute(string code);
}