namespace Spurt.Domain.Categories;

public class Clue
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Answer { get; set; }
    public required string Question { get; set; }
    public required int PointValue { get; set; }

    // Category relationship
    public required Guid CategoryId { get; set; }
    public required Category Category { get; set; }
}