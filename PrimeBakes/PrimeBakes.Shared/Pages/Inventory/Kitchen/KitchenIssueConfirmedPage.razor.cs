using Microsoft.AspNetCore.Components;

namespace PrimeBakes.Shared.Pages.Inventory.Kitchen;

public partial class KitchenIssueConfirmedPage
{
	protected override async Task OnInitializedAsync()
	{
		await SoundService.PlaySound("checkout.mp3");
		VibrationService.VibrateWithTime(500);

		// Wait for 2 seconds then navigate to home
		await Task.Delay(2000);
		NavigationManager.NavigateTo("/Inventory-Dashboard", forceLoad: true);
	}
}