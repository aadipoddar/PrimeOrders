namespace PrimeBakes.Shared.Pages.Accounts;

public partial class FinancialAccountingConfirmedPage
{
	protected override async Task OnInitializedAsync()
	{
		await SoundService.PlaySound("checkout.mp3");
		VibrationService.VibrateWithTime(500);

		// Wait for 2 seconds then navigate to home
		await Task.Delay(2000);
		NavigationManager.NavigateTo("/", true);
	}
}