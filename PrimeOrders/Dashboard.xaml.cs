using System.Windows;

using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Models.Common;

using PrimeOrders.Inventory;
using PrimeOrders.Sale;

namespace PrimeOrders;

/// <summary>
/// Interaction logic for Dashboard.xaml
/// </summary>
public partial class Dashboard : Window
{
	private readonly UserModel _user;

	public Dashboard(UserModel user)
	{
		InitializeComponent();

		_user = user;
	}

	private async void Window_Loaded(object sender, RoutedEventArgs e)
	{
		if (_user is null)
			Close();

		else
		{
			bool isSale = _user.Sales;
			bool isInventory = _user.Inventory;
			bool isAdmin = _user.Admin;

			if (isAdmin)
				saleButton.Visibility = Visibility.Visible;

			if (!isSale)
				saleButton.Visibility = Visibility.Collapsed;

			if (!isInventory)
			{
				kitchenIssueButton.Visibility = Visibility.Collapsed;
			}

			var location = await CommonData.LoadTableDataById<LocationModel>(TableNames.Location, _user.LocationId);

			userLabel.Text = $"Hello {_user.Name} to {location.Name}";
		}
	}

	private void saleButton_Click(object sender, RoutedEventArgs e)
	{
		SaleWindow saleWindow = new(_user);
		saleWindow.Show();
	}

	private void kitchenIssueButton_Click(object sender, RoutedEventArgs e)
	{
		KitchenIssueWindow kitchenIssueWindow = new(_user);
		kitchenIssueWindow.Show();
	}

	private void Window_Closed(object sender, EventArgs e) =>
		Application.Current.Shutdown();
}
