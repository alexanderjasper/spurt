using Microsoft.AspNetCore.Components;
using Spurt.Domain.Categories;

namespace Spurt.Components.Shared;

public partial class GameBoard
{
    [Parameter] public required IEnumerable<Category> Categories { get; set; }
    [Parameter] public required EventCallback<Clue> OnClueSelected { get; set; }
} 