using PrimeOrdersLibrary.Data.Common;
using PrimeOrdersLibrary.DataAccess;
using PrimeOrdersLibrary.Models.Common;
using PrimeOrdersLibrary.Models.Order;

namespace PrimeBakes.Order;

public partial class CartPage : ContentPage
{
	private readonly int _userId;
	private UserModel _user;
	private readonly List<OrderProductCartModel> _cart;

	public CartPage(int userId, List<OrderProductCartModel> cart)
	{
		InitializeComponent();

		_userId = userId;
		_cart = cart;
		cartItemsCollectionView.ItemsSource = _cart;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();

		_user = await CommonData.LoadTableDataById<UserModel>(TableNames.User, _userId);
		cartItemsLabel.Text = $"{_cart.Sum(_ => _.Quantity)} Items";

		var locations = await CommonData.LoadTableDataByStatus<LocationModel>(TableNames.Location);
		locations.Remove(locations.FirstOrDefault(c => c.Id == 1));

		locationComboBox.ItemsSource = locations;
		locationComboBox.DisplayMemberPath = nameof(LocationModel.Name);
		locationComboBox.SelectedValuePath = nameof(LocationModel.Id);

		var location = await CommonData.LoadTableDataById<LocationModel>(TableNames.Location, _user.LocationId);
		if (!location.MainLocation)
		{
			locationSelectionGrid.IsVisible = false;
			locationComboBox.SelectedValue = _user.LocationId;
		}

		else
		{
			locationSelectionGrid.IsVisible = true;
			locationComboBox.SelectedValue = locations.FirstOrDefault().Id;
		}
	}

	private void quantityNumericEntry_ValueChanged(object sender, Syncfusion.Maui.Inputs.NumericEntryValueChangedEventArgs e)
	{
		if ((sender as Syncfusion.Maui.Inputs.SfNumericEntry).Parent.BindingContext is not OrderProductCartModel item)
			return;

		var quantity = Convert.ToInt32(e.NewValue);

		var existingCart = _cart.FirstOrDefault(x => x.ProductId == item.ProductId);
		if (existingCart is not null)
			_cart.Remove(existingCart);

		if (quantity is not 0)
			_cart.Add(new()
			{
				ProductId = item.ProductId,
				ProductName = item.ProductName,
				Quantity = quantity
			});

		cartItemsLabel.Text = $"{_cart.Sum(_ => _.Quantity)} Items";
	}

	private void CheckoutButton_Clicked(object sender, EventArgs e)
	{

	}
}