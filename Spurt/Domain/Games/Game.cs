using Spurt.Domain.Players;

namespace Spurt.Domain.Games;

public class Game
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Code { get; set; }
    public required Player Creator { get; set; }
    public Guid CreatorId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<Player> Players { get; set; } = [];
}