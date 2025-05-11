namespace Spurt.Domain.Players;

public class Player
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Name { get; set; }
}