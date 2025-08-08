#if ANDROID
using System.Reflection;

using PrimeBakes.Platforms.Android;
#endif

using PrimeOrdersLibrary.Data.Common;

namespace PrimeBakes;

public partial class LoginPage : ContentPage
{
	private const string _currentUserIdKey = "user_id";

	public LoginPage()
	{
		InitializeComponent();

#if ANDROID
		var currentVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();

		if (Task.Run(async () => await AadiSoftUpdater.CheckForUpdates("aadipoddar", "PrimeOrders", currentVersion)).Result)
			Task.Run(async () => await AadiSoftUpdater.UpdateApp("aadipoddar", "PrimeOrders", "com.aadisoft.primebakes"));
#endif

		var userId = SecureStorage.GetAsync(_currentUserIdKey).GetAwaiter().GetResult();
		if (userId is not null && userId is not "0")
			Navigation.PushAsync(new Dashboard(int.Parse(userId)), true);
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();
		SecureStorage.Remove(_currentUserIdKey);
	}

	private async void OnValueChanged(object sender, Syncfusion.Maui.Toolkit.OtpInput.OtpInputValueChangedEventArgs e)
	{
		var passcode = e.NewValue?.ToString() ?? string.Empty;
		if (passcode.Length != 4)
			return;

		var user = await UserData.LoadUserByPasscode(int.Parse(passcode));
		if (user is null || !user.Status)
		{
			await DisplayAlert("Error", "Invalid passcode or user is inactive.", "OK");
			passcodeInput.Value = string.Empty;
			return;
		}

		await SecureStorage.Default.SetAsync(_currentUserIdKey, user.Id.ToString());
		await Navigation.PushAsync(new Dashboard(user.Id));
		passcodeInput.Value = string.Empty;
	}
}
