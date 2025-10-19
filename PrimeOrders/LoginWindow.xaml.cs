using System.Windows;

using PrimeBakesLibrary.Data.Common;

using PrimeOrders.Common;

namespace PrimeOrders;

/// <summary>
/// Interaction logic for LoginWindow.xaml
/// </summary>
public partial class LoginWindow : Window
{
	public LoginWindow() =>
		InitializeComponent();

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
