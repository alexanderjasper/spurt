using Microsoft.AspNetCore.Components;
using Spurt.Domain.Categories;
using Spurt.Domain.Categories.Commands;
using Spurt.Domain.Games;
using Spurt.Domain.Players;

namespace Spurt.Components.Pages;

public partial class CategoryEditor(ISaveCategory saveCategory, IGameHubNotificationService gameHubNotificationService)
{
    private class ClueForm
    {
        public string Question { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
        public int PointValue { get; set; } = 100;
    }

    private class CategoryForm
    {
        public string Title { get; set; } = string.Empty;
        public List<ClueForm> Clues { get; set; } = [];
    }

    [Parameter] public required Player CurrentPlayer { get; set; }
    private CategoryForm Form { get; } = new();

    private bool IsEditing => CurrentPlayer.Category != null;

    protected override void OnInitialized()
    {
        var existingCategory = CurrentPlayer.Category;
        if (existingCategory != null)
        {
            Form.Title = existingCategory.Title;
            Form.Clues = existingCategory.Clues
                .Select(c => new ClueForm
                {
                    Question = c.Question,
                    Answer = c.Answer,
                    PointValue = c.PointValue,
                })
                .ToList();
        }

        base.OnInitialized();
    }

    private bool IsValid =>
        !string.IsNullOrWhiteSpace(Form.Title) &&
        Form.Clues.Count == 5 &&
        Form.Clues.All(c => !string.IsNullOrWhiteSpace(c.Answer) && !string.IsNullOrWhiteSpace(c.Question));

    private int PointValueToIndex(int pointValue)
    {
        return pointValue / 100 - 1;
    }

    private void EnsureClueExists(int pointValue)
    {
        var index = PointValueToIndex(pointValue);

        while (Form.Clues.Count <= index)
            Form.Clues.Add(new ClueForm
            {
                Answer = "",
                Question = "",
                PointValue = pointValue,
            });

        if (Form.Clues[index].PointValue != pointValue)
        {
            var existingClueWithPointValue = Form.Clues.FirstOrDefault(c => c.PointValue == pointValue);
            if (existingClueWithPointValue != null && existingClueWithPointValue != Form.Clues[index])
            {
                var existingIndex = Form.Clues.IndexOf(existingClueWithPointValue);
                var temp = Form.Clues[index];
                Form.Clues[index] = existingClueWithPointValue;
                Form.Clues[existingIndex] = temp;
            }
            else
            {
                Form.Clues[index].PointValue = pointValue;
            }
        }
    }

    private async Task SaveDraft()
    {
        FillExistingCategory();
        if (CurrentPlayer.Category == null)
            throw new InvalidOperationException("No existing category");
        await saveCategory.Execute(CurrentPlayer.Category);

        await gameHubNotificationService.NotifyGameUpdated(CurrentPlayer.Game.Code);
    }

    private async Task Submit()
    {
        FillExistingCategory();
        if (CurrentPlayer.Category == null)
            throw new InvalidOperationException("No existing category");
        await saveCategory.Execute(CurrentPlayer.Category, true);

        await gameHubNotificationService.NotifyGameUpdated(CurrentPlayer.Game.Code);
    }

    private void FillExistingCategory()
    {
        CurrentPlayer.Category ??= new Category
        {
            Player = CurrentPlayer,
            PlayerId = CurrentPlayer.Id,
        };
        var category = CurrentPlayer.Category;
        category.Title = Form.Title;
        category.Clues = Form.Clues
            .Select(c => new Clue
            {
                Question = c.Question,
                Answer = c.Answer,
                PointValue = c.PointValue,
                CategoryId = category.Id,
                Category = category,
            })
            .ToList();
    }
}