using PrimeBakes.Shared.Services;

using PrimeBakesLibrary.Data;
using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Order;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Exporting.Order;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Order;

using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Popups;

namespace PrimeBakes.Shared.Pages.Order;

public partial class OrderCartPage
{
	private UserModel _user;
	private bool _isLoading = true;
	private bool _isSaving = false;

	private bool _orderDetailsDialogVisible = false;
	private bool _orderConfirmationDialogVisible = false;
	private bool _validationErrorDialogVisible = false;

	private List<LocationModel> _locations = [];
	private readonly List<OrderProductCartModel> _cart = [];

	private OrderModel _order = new() { OrderDateTime = DateTime.Now, Id = 0, SaleId = null, Status = true, Remarks = "", CreatedAt = DateTime.Now };
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
		var authResult = await AuthService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, UserRoles.Order);
		_user = authResult.User;
		await LoadData();
		_isLoading = false;
	}

	private async Task LoadData()
	{
		_locations = await CommonData.LoadTableDataByStatus<LocationModel>(TableNames.Location);
		_locations.RemoveAll(c => c.Id == 1);

		_order.LocationId = _user.LocationId == 1 ? _locations.FirstOrDefault().Id : _user.LocationId;
		_order.OrderNo = await GenerateCodes.GenerateOrderBillNo(_order);

		_cart.Clear();
		if (await DataStorageService.LocalExists(StorageFileNames.OrderCartDataFileName))
		{
			var items = System.Text.Json.JsonSerializer.Deserialize<List<OrderProductCartModel>>(await DataStorageService.LocalGetAsync(StorageFileNames.OrderCartDataFileName)) ?? [];
			foreach (var item in items)
				_cart.Add(item);
		}

		_cart.Sort((x, y) => string.Compare(x.ProductName, y.ProductName, StringComparison.Ordinal));

		if (await DataStorageService.LocalExists(StorageFileNames.OrderDataFileName))
			_order = System.Text.Json.JsonSerializer.Deserialize<OrderModel>(await DataStorageService.LocalGetAsync(StorageFileNames.OrderDataFileName)) ??
				new() { OrderDateTime = DateTime.Now, Id = 0, SaleId = null, Status = true, Remarks = "", CreatedAt = DateTime.Now };

		_order.UserId = _user.Id;

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
		VibrationService.VibrateWithTime(500);
		await SaveOrderFile();
	}
	#endregion

	#region Saving
	private async Task SaveOrderFile()
	{
		await DataStorageService.LocalSaveAsync(StorageFileNames.OrderDataFileName, System.Text.Json.JsonSerializer.Serialize(_order));

		if (!_cart.Any(x => x.Quantity > 0) && await DataStorageService.LocalExists(StorageFileNames.OrderCartDataFileName))
			await DataStorageService.LocalRemove(StorageFileNames.OrderCartDataFileName);
		else
			await DataStorageService.LocalSaveAsync(StorageFileNames.OrderCartDataFileName, System.Text.Json.JsonSerializer.Serialize(_cart.Where(_ => _.Quantity > 0)));

		VibrationService.VibrateHapticClick();
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
			await DeleteCart();
			await PrintInvoice();
			await SendLocalNotification();
			NavigationManager.NavigateTo("/Order/Confirmed", true);
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

	private async Task DeleteCart()
	{
		_cart.Clear();
		await DataStorageService.LocalRemove(StorageFileNames.OrderDataFileName);
		await DataStorageService.LocalRemove(StorageFileNames.OrderCartDataFileName);
	}

	private async Task PrintInvoice()
	{
		var ms = await OrderA4Print.GenerateA4OrderDocument(_order.Id);
		var fileName = $"Order_Bill_{_order.OrderNo}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
		await SaveAndViewService.SaveAndView(fileName, "application/pdf", ms);
	}

	private async Task SendLocalNotification()
	{
		var order = await OrderData.LoadOrderOverviewByOrderId(_order.Id);
		await NotificationService.ShowLocalNotification(
			_order.Id,
			"Order Placed",
			$"{order.OrderNo}",
			$"Your order #{order.OrderNo} has been successfully placed | Total Items: {order.TotalProducts} | Total Qty: {order.TotalQuantity} | Location: {order.LocationName} | User: {order.UserName} | Date: {order.OrderDateTime:dd/MM/yy hh:mm tt} | Remarks: {order.Remarks}");
	}
	#endregion
}