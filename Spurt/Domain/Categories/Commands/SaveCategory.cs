using Microsoft.AspNetCore.SignalR;
using Spurt.Data.Commands;
using Spurt.Domain.Games;

namespace Spurt.Domain.Categories.Commands;

public class SaveCategory(
    IAddCategory addCategory,
    IUpdateCategory updateCategory,
    IHubContext<GameHub>? hubContext = null) : ISaveCategory
{
    public async Task<Category> Execute(Category category, bool isSubmitting = false)
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

        Category savedCategory;
        var isNewCategory = category.Player?.Category == null;
        if (isNewCategory)
            savedCategory = await addCategory.Execute(category);
        else
            savedCategory = await updateCategory.Execute(category);

        if (category.IsSubmitted && hubContext != null && savedCategory.Player?.Game != null)
            await hubContext.Clients.Group(savedCategory.Player.Game.Code).SendAsync(GameHub.Events.CategorySubmitted);

        return savedCategory;
    }
}

public interface ISaveCategory
{
    Task<Category> Execute(Category category, bool isSubmitting = false);
}