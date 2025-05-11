using Microsoft.EntityFrameworkCore;
using Spurt.Domain.Players;

namespace Spurt.Data.Queries;

public interface IGetPlayer
{
    Task<Player?> Execute(Guid playerId);
}

public class GetPlayer(AppDbContext dbContext) : IGetPlayer
{
    public async Task<Player?> Execute(Guid playerId)
    {
        return await dbContext.Players.FirstOrDefaultAsync(p => p.Id == playerId);
    }
}