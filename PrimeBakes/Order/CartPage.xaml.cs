using System.Collections.ObjectModel;

#if ANDROID
using Plugin.LocalNotification;
#endif
using Plugin.Maui.Audio;

using PrimeOrdersLibrary.Data.Common;
using PrimeOrdersLibrary.Data.Order;
using PrimeOrdersLibrary.DataAccess;
using PrimeOrdersLibrary.Models.Common;
using PrimeOrdersLibrary.Models.Order;

namespace PrimeBakes.Order;

public partial class CartPage : ContentPage
{
	private const string _fileName = "cart.json";
	private readonly int _userId;
	private UserModel _user;
	private readonly ObservableCollection<OrderProductCartModel> _cart = [];
	private readonly OrderPage _orderPage;
	private bool _isRefreshing = false;

	public CartPage(int userId, OrderPage orderPage)
	{
		InitializeComponent();

		_userId = userId;
		_orderPage = orderPage;

		_cart.Clear();
		var fullPath = Path.Combine(FileSystem.Current.AppDataDirectory, _fileName);
		if (File.Exists(fullPath))
		{
			var items = System.Text.Json.JsonSerializer.Deserialize<List<OrderProductCartModel>>(File.ReadAllText(fullPath)) ?? [];
			foreach (var item in items)
				_cart.Add(item);
		}
		cartItemsCollectionView.ItemsSource = _cart;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();

		_user = await CommonData.LoadTableDataById<UserModel>(TableNames.User, _userId);

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

		orderNoTextBox.Text = await GenerateCodes.GenerateOrderBillNo(new()
		{
			Id = 0,
			LocationId = !location.MainLocation ? _user.LocationId : int.Parse(locationComboBox.SelectedValue.ToString()),
			OrderDate = DateOnly.FromDateTime(orderDatePicker.Date),
			OrderNo = "",
			Remarks = remarksTextBox.Text ?? string.Empty,
			SaleId = null,
			UserId = _user.Id,
			Status = true,
		});

		await RefreshFileAndCart();
	}

	private async void quantityNumericEntry_ValueChanged(object sender, Syncfusion.Maui.Inputs.NumericEntryValueChangedEventArgs e)
	{
		if ((sender as Syncfusion.Maui.Inputs.SfNumericEntry).Parent.BindingContext is not OrderProductCartModel item)
			return;

		if (_isRefreshing)
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

		await RefreshFileAndCart();

		HapticFeedback.Default.Perform(HapticFeedbackType.Click);
	}

	private async void RemoveFromCartButton_Clicked(object sender, EventArgs e)
	{
		if ((sender as Button).Parent.BindingContext is not OrderProductCartModel item)
			return;

		var existingCart = _cart.FirstOrDefault(x => x.ProductId == item.ProductId);
		if (existingCart is not null)
			_cart.Remove(existingCart);

		await RefreshFileAndCart();
		HapticFeedback.Default.Perform(HapticFeedbackType.Click);
	}

	private async Task RefreshFileAndCart()
	{
		if (_isRefreshing)
			return;

		_isRefreshing = true;
		await File.WriteAllTextAsync(Path.Combine(FileSystem.Current.AppDataDirectory, _fileName), System.Text.Json.JsonSerializer.Serialize(_cart));
		cartItemsLabel.Text = $"Swipe To Checkout {_cart.Sum(_ => _.Quantity)} Items";
		_isRefreshing = false;
	}

	private async void SlideCompleted(object sender, EventArgs e)
	{
		if (_cart.Count == 0)
		{
			await DisplayAlert("Empty Cart", "Your cart is empty. Please add items to your cart before proceeding.", "OK");
			return;
		}

		var order = await InsertOrder();
		await InsertOrderDetails(order);

		_cart.Clear();

		var fullPath = Path.Combine(FileSystem.Current.AppDataDirectory, _fileName);
		if (File.Exists(fullPath))
			File.Delete(fullPath);

		if (Vibration.Default.IsSupported)
			Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(500));

		AudioManager.Current.CreatePlayer(await FileSystem.OpenAppPackageFileAsync("checkout.mp3")).Play();

#if ANDROID
		var request = new NotificationRequest
		{
			NotificationId = 100,
			Title = "Order Placed",
			Subtitle = "Order Confirmation",
			Description = $"Your order #{order.OrderNo} has been successfully placed. {order.Remarks}",
			CategoryType = NotificationCategoryType.Reminder,
			BadgeNumber = 1,
			Schedule = new NotificationRequestSchedule
			{
				NotifyTime = DateTime.Now.AddSeconds(1),
				RepeatType = NotificationRepeat.No
			},
			Android =
			{
				ChannelId = "order_channel",
				Priority = Plugin.LocalNotification.AndroidOption.AndroidPriority.Max
			}
		};

		await LocalNotificationCenter.Current.Show(request);
#endif

		Navigation.RemovePage(_orderPage);
		Navigation.RemovePage(this);
		await Navigation.PushAsync(new Dashboard(_userId));
	}

	private async Task<OrderModel> InsertOrder()
	{
		var location = await CommonData.LoadTableDataById<LocationModel>(TableNames.Location, _user.LocationId);
		var order = new OrderModel()
		{
			Id = 0,
			LocationId = !location.MainLocation ? _user.LocationId : int.Parse(locationComboBox.SelectedValue.ToString()),
			OrderDate = DateOnly.FromDateTime(orderDatePicker.Date),
			OrderNo = "",
			Remarks = remarksTextBox.Text ?? string.Empty,
			SaleId = null,
			UserId = _user.Id,
			Status = true,
		};

		order.OrderNo = await GenerateCodes.GenerateOrderBillNo(order);
		order.Id = await OrderData.InsertOrder(order);
		return order;
	}

	private async Task InsertOrderDetails(OrderModel order)
	{
		foreach (var item in _cart)
			await OrderData.InsertOrderDetail(new()
			{
				Id = 0,
				OrderId = order.Id,
				ProductId = item.ProductId,
				Quantity = item.Quantity,
				Status = true
			});
	}
}