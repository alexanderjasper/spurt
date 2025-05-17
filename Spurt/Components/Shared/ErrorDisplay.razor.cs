using Microsoft.AspNetCore.Components;

namespace Spurt.Components.Shared;

public partial class ErrorDisplay
{
    [Parameter] public string? ErrorMessage { get; set; }
    [Parameter] public bool ShowDismissButton { get; set; } = true;
    [Parameter] public EventCallback OnErrorDismissed { get; set; }

    private async Task Dismiss()
    {
        ErrorMessage = null;
        await OnErrorDismissed.InvokeAsync();
    }
} 