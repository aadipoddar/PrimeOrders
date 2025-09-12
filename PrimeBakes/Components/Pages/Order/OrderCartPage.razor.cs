using Microsoft.AspNetCore.Components;

#if ANDROID
using Plugin.LocalNotification;
#endif

using PrimeBakes.Services;

using PrimeOrdersLibrary.Data.Common;
using PrimeOrdersLibrary.Data.Order;
using PrimeOrdersLibrary.DataAccess;
using PrimeOrdersLibrary.Exporting.Order;
using PrimeOrdersLibrary.Models.Common;
using PrimeOrdersLibrary.Models.Order;

using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Popups;

namespace PrimeBakes.Components.Pages.Order;

public partial class OrderCartPage
{
	[Inject] public NavigationManager NavManager { get; set; }

	private UserModel _user;
	private bool _isLoading = true;
	private bool _isSaving = false;

	private const string _fileName = "orderCart.json";

	private bool _orderDetailsDialogVisible = false;
	private bool _orderConfirmationDialogVisible = false;
	private bool _validationErrorDialogVisible = false;

	private List<LocationModel> _locations = [];
	private readonly List<OrderProductCartModel> _cart = [];

	private LocationModel _userLocation;
	private readonly OrderModel _order = new() { OrderDateTime = DateTime.Now, Id = 0, SaleId = null, Status = true, Remarks = "" };
	private readonly List<ValidationError> _validationErrors = [];

	// Validation Error Model
	public class ValidationError
	{
		public string Field { get; set; } = string.Empty;
		public string Message { get; set; } = string.Empty;
	}

	private SfGrid<OrderProductCartModel> _sfGrid;
	private SfDialog _sfOrderDetailsDialog;
	private SfDialog _sfOrderConfirmationDialog;
	private SfDialog _sfValidationErrorDialog;

	protected override async Task OnInitializedAsync()
	{
		_user = await AuthService.AuthenticateCurrentUser(NavManager);

		await LoadData();
		_isLoading = false;
	}

	private async Task LoadData()
	{
		_locations = await CommonData.LoadTableDataByStatus<LocationModel>(TableNames.Location);
		_userLocation = _locations.FirstOrDefault(c => c.Id == _user.LocationId);
		_locations.RemoveAll(c => c.MainLocation);

		_order.LocationId = _user.LocationId == 1 ? _locations.FirstOrDefault().Id : _user.LocationId;
		_order.OrderNo = await GenerateCodes.GenerateOrderBillNo(_order);
		_order.UserId = _user.Id;

		_cart.Clear();
		var fullPath = Path.Combine(FileSystem.Current.AppDataDirectory, _fileName);
		if (File.Exists(fullPath))
		{
			var items = System.Text.Json.JsonSerializer.Deserialize<List<OrderProductCartModel>>(await File.ReadAllTextAsync(fullPath)) ?? [];
			foreach (var item in items)
				_cart.Add(item);
		}

		_cart.Sort((x, y) => string.Compare(x.ProductName, y.ProductName, StringComparison.Ordinal));

		if (_sfGrid is not null)
			await _sfGrid.Refresh();

		StateHasChanged();
	}

	private async Task UpdateQuantity(OrderProductCartModel item, decimal newQuantity)
	{
		if (item is null || _isSaving)
			return;

		item.Quantity = Math.Max(0, newQuantity);

		if (item.Quantity == 0)
			_cart.Remove(item);

		await _sfGrid.Refresh();
		StateHasChanged();

		if (_cart.Count == 0 && File.Exists(Path.Combine(FileSystem.Current.AppDataDirectory, _fileName)))
			File.Delete(Path.Combine(FileSystem.Current.AppDataDirectory, _fileName));
		else
			await File.WriteAllTextAsync(Path.Combine(FileSystem.Current.AppDataDirectory, _fileName), System.Text.Json.JsonSerializer.Serialize(_cart.Where(_ => _.Quantity > 0)));

		HapticFeedback.Default.Perform(HapticFeedbackType.Click);
	}

	private async Task ClearCart()
	{
		if (_isSaving)
			return;

		_cart.Clear();
		File.Delete(Path.Combine(FileSystem.Current.AppDataDirectory, _fileName));

		if (Vibration.Default.IsSupported)
			Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(500));

		await _sfGrid.Refresh();
		StateHasChanged();
	}

	private bool ValidateForm()
	{
		_validationErrors.Clear();

		_order.OrderDateTime = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateOnly.FromDateTime(_order.OrderDateTime)
			.ToDateTime(new TimeOnly(DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second)),
			"India Standard Time");

		_order.UserId = _user.Id;

		if (!_user.Admin || !_userLocation.MainLocation)
			_order.LocationId = _user.LocationId;

		if (_order.LocationId <= 0)
		{
			_validationErrors.Add(new()
			{
				Field = "Location",
				Message = "Please select a valid location for the order."
			});
			return false;
		}

		if (_cart.Count == 0)
		{
			_validationErrors.Add(new()
			{
				Field = "Cart Items",
				Message = "Please add at least one product to the order."
			});
			return false;
		}

		StateHasChanged();
		return true;
	}

	private async Task InsertOrderDetails()
	{
		foreach (var cartItem in _cart)
			await OrderData.InsertOrderDetail(new()
			{
				Id = 0,
				OrderId = _order.Id,
				ProductId = cartItem.ProductId,
				Quantity = cartItem.Quantity,
				Status = true
			});
	}

	private async Task SaveOrder()
	{
		if (_isSaving)
			return;

		_isSaving = true;
		StateHasChanged();

		try
		{
			if (!ValidateForm())
			{
				_validationErrorDialogVisible = true;
				_orderConfirmationDialogVisible = false;
				return;
			}

			_order.OrderNo = await GenerateCodes.GenerateOrderBillNo(_order);
			_order.Id = await OrderData.InsertOrder(_order);
			if (_order.Id <= 0)
			{
				_validationErrors.Add(new()
				{
					Field = "Order",
					Message = "Failed to save the order. Please try again."
				});

				_validationErrorDialogVisible = true;
				_orderConfirmationDialogVisible = false;
				return;
			}

			await InsertOrderDetails();

			_cart.Clear();

			var fullPath = Path.Combine(FileSystem.Current.AppDataDirectory, _fileName);
			if (File.Exists(fullPath))
				File.Delete(fullPath);

			if (Vibration.Default.IsSupported)
				Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(500));

#if ANDROID
			if (await LocalNotificationCenter.Current.AreNotificationsEnabled() == false)
				await LocalNotificationCenter.Current.RequestNotificationPermission();

			var request = new NotificationRequest
			{
				NotificationId = 101,
				Title = "Order Placed",
				Subtitle = "Order Confirmation",
				Description = $"Your order #{_order.OrderNo} has been successfully placed. {_order.Remarks}",
				Schedule = new NotificationRequestSchedule
				{
					NotifyTime = DateTime.Now.AddSeconds(5)
				}
			};

			await LocalNotificationCenter.Current.Show(request);
#endif

			var ms = await OrderA4Print.GenerateA4OrderDocument(_order.Id);
			var fileName = $"Order_Bill_{_order.OrderNo}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
			SaveService saveService = new();
			var filePath = saveService.SaveAndView(fileName, "application/pdf", ms);

			NavManager.NavigateTo("/Order/Confirmed", true);
		}
		catch (Exception ex)
		{
			_validationErrors.Add(new()
			{
				Field = "Exception",
				Message = $"An error occurred while saving the order: {ex.Message}"
			});
			_validationErrorDialogVisible = true;
			_orderConfirmationDialogVisible = false;
		}
		finally
		{
			_isSaving = false;
			StateHasChanged();
		}
	}
}