using Microsoft.AspNetCore.Components;

namespace PrimeBakes.Shared.Components.Button;

public partial class ToggleDeletedButton
{
	[Parameter]
	public bool Disabled { get; set; } = false;

	[Parameter]
	public bool ShowDeleted { get; set; } = false;

	[Parameter]
	public EventCallback OnToggle { get; set; }

	private async Task HandleClick()
	{
		await OnToggle.InvokeAsync();
	}
}
