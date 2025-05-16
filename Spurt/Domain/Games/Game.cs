using Spurt.Domain.Players;

namespace Spurt.Domain.Games;

public class Game
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Code { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<Player> Players { get; set; } = [];

    public void ValidateCreator()
    {
        int creatorCount = Players.Count(p => p.IsCreator);
        if (creatorCount != 1)
        {
            throw new InvalidOperationException($"Game must have exactly one player marked as Creator. Found {creatorCount}.");
        }
    }
}