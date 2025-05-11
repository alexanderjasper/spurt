using Spurt.Domain.Player;

namespace Spurt.Data.Commands;

public interface IAddPlayer
{
    void Execute(Player player);
}

public class AddPlayer(AppDbContext dbContext) : IAddPlayer
{
    public void Execute(Player player)
    {
        dbContext.Players.Add(player);
        dbContext.SaveChanges();
    }
} 