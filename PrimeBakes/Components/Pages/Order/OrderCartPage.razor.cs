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

	private bool _orderDetailsDialogVisible = false;
	private bool _orderConfirmationDialogVisible = false;
	private bool _validationErrorDialogVisible = false;

	private List<LocationModel> _locations = [];
	private readonly List<OrderProductCartModel> _cart = [];

	private readonly OrderModel _order = new() { OrderDateTime = DateTime.Now, Id = 0, SaleId = null, Status = true, Remarks = "", CreatedAt = DateTime.Now };
	private readonly List<ValidationError> _validationErrors = [];

	public class ValidationError
	{
		public string Field { get; set; } = string.Empty;
		public string Message { get; set; } = string.Empty;
	}

	private SfGrid<OrderProductCartModel> _sfGrid;
	private SfDialog _sfOrderDetailsDialog;
	private SfDialog _sfOrderConfirmationDialog;
	private SfDialog _sfValidationErrorDialog;

	#region Load Data
	protected override async Task OnInitializedAsync()
	{
		_user = await AuthService.AuthenticateCurrentUser(NavManager);

		await LoadData();
		_isLoading = false;
	}

	private async Task LoadData()
	{
		_locations = await CommonData.LoadTableDataByStatus<LocationModel>(TableNames.Location);
		_locations.RemoveAll(c => c.Id == 1);

		_order.LocationId = _user.LocationId == 1 ? _locations.FirstOrDefault().Id : _user.LocationId;
		_order.UserId = _user.Id;
		_order.OrderNo = await GenerateCodes.GenerateOrderBillNo(_order);

		_cart.Clear();
		var fullPath = Path.Combine(FileSystem.Current.AppDataDirectory, StorageFileNames.OrderCart);
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
	#endregion

	#region Products
	private async Task UpdateQuantity(OrderProductCartModel item, decimal newQuantity)
	{
		if (item is null || _isSaving)
			return;

		item.Quantity = Math.Max(0, newQuantity);

		if (item.Quantity == 0)
			_cart.Remove(item);

		await SaveOrderFile();
	}

	private async Task ClearCart()
	{
		if (_isSaving)
			return;

		_cart.Clear();

		if (Vibration.Default.IsSupported)
			Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(500));

		await SaveOrderFile();
	}
	#endregion

	#region Saving
	private async Task SaveOrderFile()
	{
		if (!_cart.Any(x => x.Quantity > 0) && File.Exists(Path.Combine(FileSystem.Current.AppDataDirectory, StorageFileNames.OrderCart)))
			File.Delete(Path.Combine(FileSystem.Current.AppDataDirectory, StorageFileNames.OrderCart));
		else
			await File.WriteAllTextAsync(Path.Combine(FileSystem.Current.AppDataDirectory, StorageFileNames.OrderCart), System.Text.Json.JsonSerializer.Serialize(_cart.Where(_ => _.Quantity > 0)));

		HapticFeedback.Default.Perform(HapticFeedbackType.Click);

		await _sfGrid.Refresh();
		StateHasChanged();
	}

	private bool ValidateForm()
	{
		_validationErrors.Clear();

		if (_order.LocationId <= 1)
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

	private async Task SaveOrder()
	{
		if (_isSaving)
			return;

		_isSaving = true;
		StateHasChanged();

		try
		{
			await SaveOrderFile();

			if (!ValidateForm())
			{
				_validationErrorDialogVisible = true;
				_orderConfirmationDialogVisible = false;
				return;
			}

			_order.Id = await OrderData.SaveOrder(_order, _cart);
			DeleteCart();
			await PrintInvoice();
#if ANDROID
			await SendLocalNotification();
#endif

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

	private void DeleteCart()
	{
		_cart.Clear();

		var fullPath = Path.Combine(FileSystem.Current.AppDataDirectory, StorageFileNames.OrderCart);
		if (File.Exists(fullPath))
			File.Delete(fullPath);
	}

	private async Task PrintInvoice()
	{
		var ms = await OrderA4Print.GenerateA4OrderDocument(_order.Id);
		var fileName = $"Order_Bill_{_order.OrderNo}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
		SaveService saveService = new();
		saveService.SaveAndView(fileName, "application/pdf", ms);
	}

#if ANDROID
	private async Task SendLocalNotification()
	{
		if (await LocalNotificationCenter.Current.AreNotificationsEnabled() == false)
			await LocalNotificationCenter.Current.RequestNotificationPermission();

		var order = await OrderData.LoadOrderOverviewByOrderId(_order.Id);

		var request = new NotificationRequest
		{
			NotificationId = _order.Id,
			Title = "Order Placed",
			Subtitle = $"{order.OrderNo}",
			Description = $"Your order #{order.OrderNo} has been successfully placed | Total Items: {order.TotalProducts} | Total Qty: {order.TotalQuantity} | Location: {order.LocationName} | User: {order.UserName} | Date: {order.OrderDateTime:dd/MM/yy hh:mm tt} | Remarks: {order.Remarks}",
			Schedule = new NotificationRequestSchedule
			{
				NotifyTime = DateTime.Now.AddSeconds(5)
			}
		};

		await LocalNotificationCenter.Current.Show(request);
	}
#endif
	#endregion
}