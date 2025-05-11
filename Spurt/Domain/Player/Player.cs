namespace Spurt.Domain.Player;

public class Player
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Name { get; set; }
}