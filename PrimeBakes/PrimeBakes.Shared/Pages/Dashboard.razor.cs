﻿using System.Reflection;

using PrimeBakes.Shared.Services;

using PrimeBakesLibrary.Models.Common;

namespace PrimeBakes.Shared.Pages;

public partial class Dashboard
{
	private UserModel _user;

	// State management properties
	private bool _isLoading = true;
	private string _isLoadingText = "Loading dashboard...";
	private bool _hasConnectionError = false;
	private bool _isUpdating = false;
	private int _progressPercentage = 0;
	private string _estimatedTime = "";
	private string _currentFunFact = "";
	private DateTime _updateStartTime;

	#region Device Info
	private string Factor => FormFactor.GetFormFactor();
	private string Platform => FormFactor.GetPlatform();

	// Device information properties
	private string DeviceType =>
		GetDeviceTypeIcon();
	private static string AppVersion =>
		Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0.0";

	private string GetDeviceTypeIcon() =>
		Factor.ToLower() switch
		{
			"phone" => "📱",
			"tablet" => "📱",
			"desktop" => "💻",
			"tv" => "📺",
			_ => "🔧"
		};
	#endregion

	#region Connection
	// HttpClient for connectivity checking
	private readonly HttpClient _httpClient = new()
	{
		Timeout = TimeSpan.FromSeconds(10) // 10 second timeout for connection tests
	};

	// Reliable endpoints to test connectivity
	private readonly string[] _connectivityTestEndpoints = [
		"https://www.google.com",
		"https://www.microsoft.com",
		"https://httpbin.org/status/200"
	];

	private async Task CheckInternetConnectionAsync()
	{
		_isLoadingText = "Checking connection...";
		StateHasChanged();

		try
		{
			var isConnected = await PingTestEndpointsAsync();

			if (!isConnected)
				throw new Exception("No internet connection");
		}
		catch (Exception)
		{
			throw new Exception("No internet connection");
		}
	}

	private async Task<bool> PingTestEndpointsAsync()
	{
		try
		{
			// Try multiple reliable endpoints to ensure connectivity
			foreach (var endpoint in _connectivityTestEndpoints)
			{
				try
				{
					using var response = await _httpClient.GetAsync(endpoint);
					if (response.IsSuccessStatusCode)
						return true; // Successfully connected to at least one endpoint
				}
				catch (Exception)
				{
					// Continue to next endpoint if this one fails
					continue;
				}
			}

			// If all endpoints failed, return false
			return false;
		}
		catch (Exception)
		{
			return false;
		}
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

	public void Dispose() =>
		_httpClient?.Dispose();
	#endregion

	#region Updating
	private readonly string[] _funFacts = [
		"Did you know? Prime Bakes serves the freshest pastries in town! 🥐",
		"Fun fact: Our app updates automatically to bring you the best experience! ✨",
		"Tip: New features are coming in this update to make your work easier! 🚀",
		"Did you know? This update includes performance improvements! ⚡",
		"Fun fact: Our team works around the clock to improve your experience! 👨‍💻",
		"Tip: Updates help keep your data secure and protected! 🔒",
		"Did you know? This update may include new themes and designs! 🎨",
		"Fun fact: Automatic updates ensure you always have the latest features! 🆕"
	];

	private async Task StartUpdateProcess()
	{
		_isUpdating = true;
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

		await UpdateService.UpdateAppAsync("aadipoddar", "PrimeOrders", "com.aadisoft.primebakes", progress);
	}

	private static double GetCircumference() =>
		2 * Math.PI * 60; // radius = 60

	private double GetStrokeOffset() =>
		GetCircumference() - (_progressPercentage / 100.0 * GetCircumference());
	#endregion

	#region Load Data
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		try
		{
			await CheckInternetConnectionAsync();

			if (Factor == "Phone" && Platform.Contains("Android"))
			{
				var hasUpdate = await UpdateService.CheckForUpdatesAsync("aadipoddar", "PrimeOrders", AppVersion);
				if (hasUpdate)
					await StartUpdateProcess();
			}

			await LoadData();
		}
		catch (Exception)
		{
			_hasConnectionError = true;
			_isLoadingText = "Unable to connect to the internet";
		}
		finally
		{
			_isLoading = false;
			StateHasChanged();
		}
	}

	private async Task LoadData()
	{
		_isLoadingText = "Loading dashboard...";
		var authResult = await AuthService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService);
		_user = authResult.User;
		await NotificationService.RegisterDevicePushNotification(_user.Id.ToString());
	}

	private async Task Logout() =>
		await AuthService.Logout(DataStorageService, NavigationManager, NotificationService, VibrationService);
	#endregion
}