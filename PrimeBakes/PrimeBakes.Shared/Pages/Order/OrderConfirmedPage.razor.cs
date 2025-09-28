namespace PrimeBakes.Shared.Pages.Order;

public partial class OrderConfirmedPage
{
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		await SoundService.PlaySound("checkout.mp3");
		VibrationService.VibrateWithTime(500);

		// Wait for 2 seconds then navigate to home
		await Task.Delay(2000);
		NavigationManager.NavigateTo("/", forceLoad: true);
	}
}