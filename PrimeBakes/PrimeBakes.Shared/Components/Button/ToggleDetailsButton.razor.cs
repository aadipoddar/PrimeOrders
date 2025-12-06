using Microsoft.AspNetCore.Components;

namespace PrimeBakes.Shared.Components.Button;

public partial class ToggleDetailsButton
{
	[Parameter]
	public bool Disabled { get; set; } = false;

	[Parameter]
	public bool ShowAllColumns { get; set; } = false;

	[Parameter]
	public EventCallback OnToggle { get; set; }

	private async Task HandleClick()
	{
		await OnToggle.InvokeAsync();
	}
}
