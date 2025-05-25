using Spurt.Data.Commands;
using Spurt.Data.Queries;
using Spurt.Domain.Games;

namespace Spurt.Domain.Categories.Commands;

public class SaveCategory(
    IAddCategory addCategory,
    IUpdateCategory updateCategory,
    IGetGame getGame,
    IGetPlayer getPlayer,
    IGameHubNotificationService gameHubNotificationService) : ISaveCategory
{
    public async Task<Game> Execute(Category category, bool isSubmitting = false)
    {
        if (category.Clues.Any(c => c.PointValue < 100 || c.PointValue > 500 || c.PointValue % 100 != 0))
            throw new ArgumentException("Clue point values must be 100, 200, 300, 400, or 500.");

        if (isSubmitting)
        {
            category.IsSubmitted = true;

            var pointValues = category.Clues.Select(c => c.PointValue).OrderBy(pv => pv).ToList();
            var expectedPointValues = new List<int> { 100, 200, 300, 400, 500 };

            if (pointValues.Count != 5 || !pointValues.SequenceEqual(expectedPointValues))
                throw new ArgumentException(
                    "A submitted category must have exactly 5 clues with point values 100, 200, 300, 400, and 500.");
        }

        var player = await getPlayer.Execute(category.PlayerId) ??
                     throw new InvalidOperationException("Player not found for the category.");

        Category savedCategory;
        var isNewCategory = player.Category == null;
        if (isNewCategory)
            await addCategory.Execute(category);
        else
            await updateCategory.Execute(category);

        var gameCode = player.Game.Code;
        var game = await getGame.Execute(gameCode) ??
                   throw new InvalidOperationException("Game not found after saving category.");
        await gameHubNotificationService.NotifyGameUpdated(game);

        return game;
    }
}

public interface ISaveCategory
{
    Task<Game> Execute(Category category, bool isSubmitting = false);
}