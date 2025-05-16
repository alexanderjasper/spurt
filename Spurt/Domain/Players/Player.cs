using Spurt.Domain.Games;
using Spurt.Domain.Users;

namespace Spurt.Domain.Players;

public class Player
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public bool IsCreator { get; set; }
    public required User User { get; set; }
    public required Guid UserId { get; set; }
    public required Game Game { get; set; }
    public required Guid GameId { get; set; }
}