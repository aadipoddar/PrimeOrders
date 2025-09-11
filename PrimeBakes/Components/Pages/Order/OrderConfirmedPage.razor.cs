using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

using Plugin.Maui.Audio;

namespace PrimeBakes.Components.Pages.Order;

public partial class OrderConfirmedPage
{
	[Inject] public NavigationManager NavManager { get; set; }
	[Inject] public IJSRuntime JS { get; set; }

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		AudioManager.Current.CreatePlayer(await FileSystem.OpenAppPackageFileAsync("checkout.mp3")).Play();

		if (Vibration.Default.IsSupported)
			Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(500));

		// Wait for 2 seconds then navigate to home
		await Task.Delay(2000);
		NavManager.NavigateTo("/", forceLoad: true);
	}
}