using Spurt.Domain.Players;

namespace Spurt.Data.Commands;

public class AddPlayer(AppDbContext dbContext) : IAddPlayer
{
    public async Task<Player> Execute(Player player)
    {
        var result = await dbContext.Players.AddAsync(player);
        await dbContext.SaveChangesAsync();
        return result.Entity;
    }
}

public interface IAddPlayer
{
    Task<Player> Execute(Player player);
}