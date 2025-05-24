using Microsoft.AspNetCore.Components;
using Spurt.Domain.Categories;
using Spurt.Domain.Categories.Commands;

namespace Spurt.Components.Pages;

public partial class CategoryEditor
{
    [Parameter] public Guid PlayerId { get; set; }
    [Parameter] public Category? ExistingCategory { get; set; }
    [Parameter] public EventCallback<Domain.Games.Game> OnCategorySaved { get; set; }
    [Parameter] public EventCallback<Domain.Games.Game> OnCategorySubmitted { get; set; }

    public required Category Category { get; set; }
    private bool IsEditing => ExistingCategory != null;

    [Inject] private ISaveCategory SaveCategory { get; set; } = null!;

    protected override void OnInitialized()
    {
        if (ExistingCategory != null)
        {
            Category = ExistingCategory;

            foreach (var pointValue in new[] { 100, 200, 300, 400, 500 }) EnsureClueExists(pointValue);
        }
        else
        {
            // Initialize a new category
            Category = new Category
            {
                Id = Guid.NewGuid(),
                Title = "",
                PlayerId = PlayerId,
                Clues = new List<Clue>(),
                // Player will be set by the data layer
                Player = null!,
            };
        }

        base.OnInitialized();
    }

    private bool IsValid =>
        !string.IsNullOrWhiteSpace(Category.Title) &&
        Category.Clues.Count == 5 &&
        Category.Clues.All(c => !string.IsNullOrWhiteSpace(c.Answer) && !string.IsNullOrWhiteSpace(c.Question));

    private int PointValueToIndex(int pointValue)
    {
        return pointValue / 100 - 1;
    }

    private void EnsureClueExists(int pointValue)
    {
        var index = PointValueToIndex(pointValue);

        while (Category.Clues.Count <= index)
            Category.Clues.Add(new Clue
            {
                Answer = "",
                Question = "",
                PointValue = pointValue,
                CategoryId = Category.Id,
                Category = Category,
            });

        if (Category.Clues[index].PointValue != pointValue)
        {
            var existingClueWithPointValue = Category.Clues.FirstOrDefault(c => c.PointValue == pointValue);
            if (existingClueWithPointValue != null && existingClueWithPointValue != Category.Clues[index])
            {
                var existingIndex = Category.Clues.IndexOf(existingClueWithPointValue);
                var temp = Category.Clues[index];
                Category.Clues[index] = existingClueWithPointValue;
                Category.Clues[existingIndex] = temp;
            }
            else
            {
                Category.Clues[index].PointValue = pointValue;
            }
        }
    }

    private async Task SaveDraft()
    {
        var game = await SaveCategory.Execute(Category);
        await OnCategorySaved.InvokeAsync(game);
    }

    private async Task SubmitCategory()
    {
        var game = await SaveCategory.Execute(Category, true);
        await OnCategorySaved.InvokeAsync(game);
    }
}