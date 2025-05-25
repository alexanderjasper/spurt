using Spurt.Domain.Players;

namespace Spurt.Domain.Categories;

public class Category
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public bool IsSubmitted { get; set; } = false;
    public List<Clue> Clues { get; set; } = [];
    public required Guid PlayerId { get; set; }
    public required Player Player { get; set; }
}