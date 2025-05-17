using Microsoft.AspNetCore.Components;

namespace Spurt.Components.Shared;

public partial class BuzzerButton
{
    [Parameter] public required EventCallback OnBuzz { get; set; }
    [Parameter] public bool Disabled { get; set; }

    private async Task OnBuzzerClick()
    {
        if (!Disabled) await OnBuzz.InvokeAsync();
    }
}