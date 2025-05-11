using Spurt.Domain.Players;

namespace Spurt.Data.Commands;

public interface IAddPlayer
{
    Task Execute(Player player);
}

public class AddPlayer(AppDbContext dbContext) : IAddPlayer
{
    public async Task Execute(Player player)
    {
        await dbContext.Players.AddAsync(player);
        await dbContext.SaveChangesAsync();
    }
}