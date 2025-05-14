using Spurt.Domain.Players;

namespace Spurt.Data.Commands;

public class AddPlayer(AppDbContext dbContext) : IAddPlayer
{
    public async Task Execute(Player player)
    {
        await dbContext.Players.AddAsync(player);
        await dbContext.SaveChangesAsync();
    }
}

public interface IAddPlayer
{
    Task Execute(Player player);
}