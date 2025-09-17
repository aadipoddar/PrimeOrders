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
	private bool _hasConnectionError = false;
	private int _progressPercentage = 0;
	private string _estimatedTime = "";
	private string _currentFunFact = "";
	private DateTime _updateStartTime;

	private readonly string[] _funFacts =
	[
		"Did you know? Prime Bakes serves the freshest pastries in town! 🥐",
		"Fun fact: Our app updates automatically to bring you the best experience! ✨",
		"Tip: New features are coming in this update to make your work easier! 🚀",
		"Did you know? This update includes performance improvements! ⚡",
		"Fun fact: Our team works around the clock to improve your experience! 👨‍💻",
		"Tip: Updates help keep your data secure and protected! 🔒",
		"Did you know? This update may include new themes and designs! 🎨",
		"Fun fact: Automatic updates ensure you always have the latest features! 🆕"
	];

	readonly INotificationRegistrationService _notificationRegistrationService;

	public Dashboard(INotificationRegistrationService service) =>
		_notificationRegistrationService = service;

	protected override async Task OnInitializedAsync()
	{
#if ANDROID
		try
		{
			var currentVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();

			if (Task.Run(async () => await AadiSoftUpdater.CheckForUpdates("aadipoddar", "PrimeOrders", currentVersion)).Result)
			{
				_updateStartTime = DateTime.Now;
				_currentFunFact = _funFacts[new Random().Next(_funFacts.Length)];
				_isLoadingText = "Updating application... 0%";
				StateHasChanged();

				var progress = new Progress<int>(percentage =>
				{
					_progressPercentage = percentage;
					_isLoadingText = $"Updating application... {percentage}%";

					// Calculate estimated time remaining
					if (percentage > 0)
					{
						var elapsed = DateTime.Now - _updateStartTime;
						var totalEstimated = TimeSpan.FromMilliseconds(elapsed.TotalMilliseconds * 100 / percentage);
						var remaining = totalEstimated - elapsed;

						if (remaining.TotalSeconds > 0)
						{
							_estimatedTime = remaining.TotalMinutes >= 1
								? $"~{remaining.Minutes}m {remaining.Seconds}s remaining"
								: $"~{remaining.Seconds}s remaining";
						}
						else
							_estimatedTime = "Almost done...";
					}

					// Change fun fact every 25%
					if (percentage > 0 && percentage % 25 == 0)
						_currentFunFact = _funFacts[new Random().Next(_funFacts.Length)];

					InvokeAsync(StateHasChanged);
				});

				await Task.Run(async () => await AadiSoftUpdater.UpdateApp("aadipoddar", "PrimeOrders", "com.aadisoft.primebakes", progress));
			}
		}
		catch (Exception)
		{
			_hasConnectionError = true;
			_isLoadingText = "Please check your Internet Connection.";
			StateHasChanged();
			return;
		}
#endif

		_user = await AuthService.AuthenticateCurrentUser(NavManager);

#if ANDROID
		await Permissions.RequestAsync<Permissions.PostNotifications>();
		await _notificationRegistrationService.RegisterDeviceAsync(_user.Id.ToString());
#endif

		_isLoading = false;
	}

	private async Task RetryConnection()
	{
		_hasConnectionError = false;
		_isLoading = true;
		_isLoadingText = "Retrying connection...";
		StateHasChanged();

		// Wait a moment for visual feedback
		await Task.Delay(1000);

		// Restart the initialization process
		await OnInitializedAsync();
	}

	private async Task Logout()
	{
		SecureStorage.Default.RemoveAll();

		File.Delete(Path.Combine(FileSystem.Current.AppDataDirectory, StorageFileNames.OrderCart));
		File.Delete(Path.Combine(FileSystem.Current.AppDataDirectory, StorageFileNames.KitchenIssueCart));
		File.Delete(Path.Combine(FileSystem.Current.AppDataDirectory, StorageFileNames.SaleCart));
		File.Delete(Path.Combine(FileSystem.Current.AppDataDirectory, StorageFileNames.Sale));

		await _notificationRegistrationService.DeregisterDeviceAsync();

		if (Vibration.Default.IsSupported)
			Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(500));

		NavManager.NavigateTo("/Login", true);
	}

	// Helper methods for the progress ring
	private static double GetCircumference() =>
		2 * Math.PI * 60; // radius = 60

	private double GetStrokeOffset() =>
		GetCircumference() - (_progressPercentage / 100.0 * GetCircumference());
}