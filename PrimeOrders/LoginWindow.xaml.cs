using System.Reflection;
using System.Windows;

using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.DataAccess;

using PrimeOrders.Common;

namespace PrimeOrders;

/// <summary>
/// Interaction logic for LoginWindow.xaml
/// </summary>
public partial class LoginWindow : Window
{
	public LoginWindow()
	{
		InitializeComponent();
		UpdateCheck();
	}

	private static async void UpdateCheck()
	{
		try
		{
			var isUpdateAvailable = await AadiSoftUpdater.AadiSoftUpdater.CheckForUpdates("aadipoddar", $"{Secrets.DatabaseName}", Assembly.GetExecutingAssembly().GetName().Version.ToString());

			if (!isUpdateAvailable) return;
			MessageBox.Show("New Version Available. Application will now update to the latest version.", "Update Available", MessageBoxButton.OK, MessageBoxImage.Information);
			await AadiSoftUpdater.AadiSoftUpdater.UpdateApp("aadipoddar", $"{Secrets.DatabaseName}", "PrimeBakes", "BF7E02F7-1471-4821-B98D-A914DF5998ED");
		}
		catch (Exception)
		{
			MessageBox.Show("No Internet Connection", "Network Error", MessageBoxButton.OK, MessageBoxImage.Error);
		}
	}

	private void Window_Loaded(object sender, RoutedEventArgs e) =>
		userPasswordBox.Focus();

	private async void userPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
	{
		if (userPasswordBox.Password.Length == 4)
		{
			var user = await UserData.LoadUserByPasscode(int.Parse(userPasswordBox.Password));
			if (user is not null)
			{
				userPasswordBox.Clear();

				if (user.Status is false)
				{
					MessageBox.Show("User is inactive. Please contact the administrator.", "Inactive User", MessageBoxButton.OK, MessageBoxImage.Error);
					return;
				}

				Dashboard dashboard = new(user);
				dashboard.Show();
				Hide();
			}
		}
	}

	private void numberTextBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e) =>
		WindowsHelper.ValidateIntegerInput(sender, e);
}
