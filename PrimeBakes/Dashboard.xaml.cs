using PrimeBakes.Order;

using PrimeOrdersLibrary.Data.Common;
using PrimeOrdersLibrary.DataAccess;
using PrimeOrdersLibrary.Models.Common;

namespace PrimeBakes;

public partial class Dashboard : ContentPage
{
	private const string _currentUserIdKey = "user_id";
	private readonly int _userId;

	public Dashboard(int userId)
	{
		InitializeComponent();

		_userId = userId;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();

		var user = await CommonData.LoadTableDataById<UserModel>(TableNames.User, _userId);
		Title = $"Welcome, {user.Name}";

		if (user.Order)
			orderButton.IsVisible = true;

		if (user.Sales)
			saleButton.IsVisible = true;
	}

	private async void LogOutButton_Clicked(object sender, EventArgs e)
	{
		HapticFeedback.Default.Perform(HapticFeedbackType.Click);
		SecureStorage.Remove(_currentUserIdKey);
		await Navigation.PopAsync(true);
	}

	private async void OrderButton_Clicked(object sender, EventArgs e)
	{
		HapticFeedback.Default.Perform(HapticFeedbackType.Click);
		await Navigation.PushAsync(new OrderPage(_userId), true);
	}
}