#if ANDROID
using System.Reflection;

using PrimeBakes.Services.Android;
#endif

using Microsoft.AspNetCore.Components;

using PrimeBakes.Services;

using PrimeOrdersLibrary.Models.Common;

namespace PrimeBakes.Components.Pages;

public partial class Dashboard
{
	[Inject] public NavigationManager NavManager { get; set; }

	private UserModel _user;
	private bool _isLoading = true;
	private string _isLoadingText = "Loading dashboard...";

	protected override async Task OnInitializedAsync()
	{
#if ANDROID
		try
		{
			var currentVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();

			if (Task.Run(async () => await AadiSoftUpdater.CheckForUpdates("aadipoddar", "PrimeOrders", currentVersion)).Result)
			{
				_isLoadingText = "Updating application...";
				StateHasChanged();
				await Task.Run(async () => await AadiSoftUpdater.UpdateApp("aadipoddar", "PrimeOrders", "com.aadisoft.primebakes"));
			}
		}
		catch (Exception)
		{
			_isLoadingText = "Please check your Internet Connection.";
			StateHasChanged();
			return;
		}
#endif

		_user = await AuthService.AuthenticateCurrentUser(NavManager);
		_isLoading = false;
	}

	private void Logout()
	{
		SecureStorage.Default.RemoveAll();

		if (Vibration.Default.IsSupported)
			Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(500));

		NavManager.NavigateTo("/Login", true);
	}
}