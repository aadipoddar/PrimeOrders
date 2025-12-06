using Microsoft.AspNetCore.Components;

namespace PrimeBakes.Shared.Components;

public partial class ToggleReturnsButton
{
	[Parameter]
	public bool Disabled { get; set; } = false;

	[Parameter]
	public bool ShowReturns { get; set; } = false;

	[Parameter]
	public EventCallback OnToggle { get; set; }

	private async Task HandleClick()
	{
		await OnToggle.InvokeAsync();
	}
}
