using Spurt.Domain.Players;

namespace Spurt.Domain.Categories;

public class Clue
{
    public Guid Id { get; set; }
    public required string Answer { get; set; }
    public required string Question { get; set; }
    public required int PointValue { get; set; }
    public bool IsAnswered => AnsweredByPlayerId != null;
    public Guid? AnsweredByPlayerId { get; set; }
    public Player? AnsweredByPlayer { get; set; }
    public bool NoOneCouldAnswer { get; set; }
    public required Guid CategoryId { get; set; }
    public required Category Category { get; set; }
}