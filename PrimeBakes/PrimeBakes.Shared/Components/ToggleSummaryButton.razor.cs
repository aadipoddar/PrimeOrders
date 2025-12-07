using Microsoft.AspNetCore.Components;

namespace PrimeBakes.Shared.Components;

public partial class ToggleSummaryButton
{
    [Parameter]
    public bool ShowSummary { get; set; }

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public EventCallback OnToggle { get; set; }

    private async Task HandleClick()
    {
        if (!Disabled)
        {
            await OnToggle.InvokeAsync();
        }
    }
}
