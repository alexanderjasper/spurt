using Spurt.Domain.Categories;
using Spurt.Domain.Players;

namespace Spurt.Domain.Games;

public enum GameState
{
    WaitingForCategories,
    InProgress,
    Finished,
}

public class Game
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Code { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<Player> Players { get; set; } = [];
    public GameState State { get; set; } = GameState.WaitingForCategories;
    public Guid? CurrentChoosingPlayerId { get; set; }
    public Clue? SelectedClue { get; set; }
    public Guid? SelectedClueId { get; set; }

    public void ValidateCreator()
    {
        var creatorCount = Players.Count(p => p.IsCreator);
        if (creatorCount != 1)
            throw new InvalidOperationException(
                $"Game must have exactly one player marked as Creator. Found {creatorCount}.");
    }

    public bool AllPlayersSubmittedCategories()
    {
        return Players.Count > 0 && Players.All(p => p.Category?.IsSubmitted ?? false);
    }
}