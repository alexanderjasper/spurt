using Spurt.Domain.Players;

namespace Spurt.Domain.Users;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Name { get; set; }
    public ICollection<Player> Players { get; set; } = [];
}