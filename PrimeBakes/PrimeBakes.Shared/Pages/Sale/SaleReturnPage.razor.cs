using PrimeBakes.Shared.Services;

using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Sale;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Exporting.Sale;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Product;
using PrimeBakesLibrary.Models.Sale;

using Syncfusion.Blazor.Calendars;
using Syncfusion.Blazor.DropDowns;
using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Popups;

namespace PrimeBakes.Shared.Pages.Sale;

public partial class SaleReturnPage
{
	private UserModel _user;
	private bool _isLoading = true;
	private bool _isSaving = false;

	// Dialog visibility flags
	private bool _returnDetailsDialogVisible = false;
	private bool _confirmReturnDialogVisible = false;
	private bool _validationErrorDialogVisible = false;

	// Validation errors
	private List<ValidationError> _validationErrors = new();

	public class ValidationError
	{
		public string Field { get; set; }
		public string Message { get; set; }
	}

	private DateOnly _saleDate = DateOnly.FromDateTime(DateTime.Now);
	private SaleReturnModel _saleReturn = new()
	{
		Id = 0,
		LocationId = 1,
		SaleId = 0,
		UserId = 0,
		TransactionNo = string.Empty,
		ReturnDateTime = DateTime.Now,
		Remarks = string.Empty,
		Status = true
	};

	private List<LocationModel> _locations = [];
	private List<SaleOverviewModel> _availableSales = [];
	private List<SaleReturnProductCartModel> _products = [];
	private List<SaleReturnProductCartModel> _availableSaleProducts = [];
	private List<SaleReturnProductCartModel> _saleReturnProductCart = [];

	private SfGrid<SaleReturnProductCartModel> _sfProductGrid;

	// Dialog references
	private SfDialog _sfValidationErrorDialog;
	private SfDialog _sfReturnDetailsDialog;
	private SfDialog _sfConfirmReturnDialog;

	#region Load Data
	protected override async Task OnInitializedAsync()
	{
		var authResult = await AuthService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, UserRoles.Sales);
		_user = authResult.User;
		await LoadData();
		_isLoading = false;
	}

	private async Task LoadData()
	{
		await LoadSaleReturn();
		await LoadLocations();
		await LoadAvailableSales();

		StateHasChanged();
	}

	private async Task LoadLocations()
	{
		_locations = await CommonData.LoadTableDataByStatus<LocationModel>(TableNames.Location);
		_saleReturn.LocationId = _user.LocationId;
	}

	private async Task LoadSaleReturn()
	{
		if (await DataStorageService.LocalExists(StorageFileNames.SaleReturnDataFileName))
			_saleReturn = System.Text.Json.JsonSerializer.Deserialize<SaleReturnModel>(
				await DataStorageService.LocalGetAsync(StorageFileNames.SaleReturnDataFileName)) ??
				new()
				{
					Id = 0,
					LocationId = _user.LocationId,
					SaleId = 0,
					UserId = _user.Id,
					TransactionNo = await GenerateCodes.GenerateSaleReturnTransactionNo(_saleReturn),
					ReturnDateTime = DateTime.Now,
					Remarks = string.Empty,
					Status = true
				};

		if (_saleReturn.SaleId > 0)
			await LoadSaleForReturn();

		_saleReturnProductCart.Clear();

		if (_saleReturn.Id != 0)
			await LoadExistingSaleReturn();

		StateHasChanged();
	}

	private async Task LoadExistingSaleReturn()
	{
		var saleReturnDetails = await SaleReturnData.LoadSaleReturnDetailBySaleReturn(_saleReturn.Id);
		foreach (var item in saleReturnDetails)
		{
			var product = await CommonData.LoadTableDataById<ProductModel>(TableNames.Product, item.ProductId);
			var cartItem = _availableSaleProducts.FirstOrDefault(p => p.ProductId == item.ProductId);

			_saleReturnProductCart.Add(new()
			{
				ProductId = product.Id,
				ProductName = product.Name,
				Quantity = item.Quantity,
				MaxQuantity = cartItem?.SoldQuantity ?? item.Quantity,
				SoldQuantity = cartItem?.SoldQuantity ?? item.Quantity,
				Rate = item.Rate,
				BaseTotal = item.BaseTotal,
				DiscPercent = item.DiscPercent,
				DiscAmount = item.DiscAmount,
				AfterDiscount = item.AfterDiscount,
				CGSTPercent = item.CGSTPercent,
				CGSTAmount = item.CGSTAmount,
				SGSTPercent = item.SGSTPercent,
				SGSTAmount = item.SGSTAmount,
				IGSTPercent = item.IGSTPercent,
				IGSTAmount = item.IGSTAmount,
				NetRate = item.NetRate,
				Total = item.Total,
			});
		}
	}

	private async Task LoadSaleForReturn()
	{
		if (_saleReturn.SaleId <= 0)
			return;

		var sale = await CommonData.LoadTableDataById<SaleModel>(TableNames.Sale, _saleReturn.SaleId);
		var saleDetails = await SaleData.LoadSaleDetailBySale(_saleReturn.SaleId);

		_saleDate = DateOnly.FromDateTime(sale.SaleDateTime);

		_availableSaleProducts.Clear();

		foreach (var detail in saleDetails)
		{
			var product = await CommonData.LoadTableDataById<ProductModel>(TableNames.Product, detail.ProductId);

			_availableSaleProducts.Add(new()
			{
				ProductId = product.Id,
				ProductName = product.Name,
				Quantity = 0,
				MaxQuantity = detail.Quantity,
				SoldQuantity = detail.Quantity,
				Rate = detail.Rate,
				BaseTotal = detail.BaseTotal,
				DiscPercent = detail.DiscPercent,
				DiscAmount = detail.DiscAmount,
				AfterDiscount = detail.AfterDiscount,
				CGSTPercent = detail.CGSTPercent,
				CGSTAmount = detail.CGSTAmount,
				SGSTPercent = detail.SGSTPercent,
				SGSTAmount = detail.SGSTAmount,
				IGSTPercent = detail.IGSTPercent,
				IGSTAmount = detail.IGSTAmount,
				NetRate = detail.NetRate,
				Total = detail.Total
			});
		}

		_products = [];
		foreach (var availableProduct in _availableSaleProducts)
			_products.Add(availableProduct);

		// Refresh the grid to show new products
		_sfProductGrid?.Refresh();
		StateHasChanged();
	}

	private async Task LoadAvailableSales()
	{
		_availableSales = await SaleData.LoadSaleDetailsByDateLocationId(
			_saleDate.ToDateTime(new TimeOnly(0, 0)),
			_saleDate.ToDateTime(new TimeOnly(23, 59)),
			_user.LocationId);

		StateHasChanged();
	}
	#endregion

	#region Fields Selection
	private async Task DateChanged(ChangedEventArgs<DateOnly> args)
	{
		_saleDate = args.Value;
		await LoadAvailableSales();
	}

	private async Task OnSaleSelected(ChangeEventArgs<int, SaleOverviewModel> args)
	{
		if (args.Value > 0)
			_saleReturn.SaleId = args.Value;
		else
			_saleReturn.SaleId = 0;

		// Clear the return cart when selecting a different sale
		_saleReturnProductCart.Clear();

		await LoadSaleForReturn();
		StateHasChanged();
	}

	private async Task OnLocationChanged(ChangeEventArgs<int, LocationModel> args)
	{
		_saleReturn.LocationId = args.Value;
		_saleReturn.SaleId = 0;

		// Clear products and return cart when location changes
		_availableSaleProducts.Clear();
		_saleReturnProductCart.Clear();

		await LoadAvailableSales();
		StateHasChanged();
	}
	#endregion

	#region Products
	private async Task AddProductToReturn(SaleReturnProductCartModel product)
	{
		if (product.MaxQuantity <= 0)
		{
			await NotificationService.ShowLocalNotification(999, "Warning", "No quantity available", "No quantity available for return");
			return;
		}

		var existingItem = _saleReturnProductCart.FirstOrDefault(c => c.ProductId == product.ProductId);
		if (existingItem != null)
		{
			if (existingItem.Quantity < existingItem.MaxQuantity)
			{
				existingItem.Quantity += 1;
				UpdateFinancialDetails();
			}
			else
			{
				await NotificationService.ShowLocalNotification(999, "Warning", "Maximum quantity reached", "Cannot exceed maximum available quantity");
			}
		}
		else
		{
			_saleReturnProductCart.Add(new SaleReturnProductCartModel
			{
				ProductId = product.ProductId,
				ProductName = product.ProductName,
				Quantity = 1,
				MaxQuantity = product.MaxQuantity,
				SoldQuantity = product.SoldQuantity,
				Rate = product.Rate,
				DiscPercent = product.DiscPercent,
				CGSTPercent = product.CGSTPercent,
				SGSTPercent = product.SGSTPercent,
				IGSTPercent = product.IGSTPercent
			});
		}

		UpdateFinancialDetails();
		StateHasChanged();
	}

	private async Task UpdateReturnQuantity(SaleReturnProductCartModel item, decimal newQuantity)
	{
		if (newQuantity < 0)
		{
			await NotificationService.ShowLocalNotification(999, "Warning", "Invalid quantity", "Quantity cannot be negative");
			return;
		}

		if (newQuantity > item.MaxQuantity)
		{
			await NotificationService.ShowLocalNotification(999, "Warning", "Maximum exceeded", $"Cannot exceed maximum available quantity ({item.MaxQuantity})");
			return;
		}

		if (newQuantity == 0)
		{
			RemoveFromReturn(item);
			return;
		}

		item.Quantity = newQuantity;
		UpdateFinancialDetails();
		StateHasChanged();
	}

	private async Task HandleQuantityChange(SaleReturnProductCartModel product, decimal newQuantity)
	{
		if (newQuantity < 0)
		{
			await NotificationService.ShowLocalNotification(999, "Warning", "Invalid quantity", "Quantity cannot be negative");
			return;
		}

		if (newQuantity > product.MaxQuantity)
		{
			await NotificationService.ShowLocalNotification(999, "Warning", "Maximum exceeded", $"Cannot exceed maximum available quantity ({product.MaxQuantity})");
			return;
		}

		var existingItem = _saleReturnProductCart.FirstOrDefault(c => c.ProductId == product.ProductId);

		if (newQuantity == 0)
		{
			if (existingItem != null)
			{
				RemoveFromReturn(existingItem);
			}
			return;
		}

		if (existingItem != null)
		{
			// Update existing item
			existingItem.Quantity = newQuantity;
		}
		else
		{
			// Add new item to cart
			_saleReturnProductCart.Add(new SaleReturnProductCartModel
			{
				ProductId = product.ProductId,
				ProductName = product.ProductName,
				Quantity = newQuantity,
				MaxQuantity = product.MaxQuantity,
				SoldQuantity = product.SoldQuantity,
				Rate = product.Rate,
				DiscPercent = product.DiscPercent,
				CGSTPercent = product.CGSTPercent,
				SGSTPercent = product.SGSTPercent,
				IGSTPercent = product.IGSTPercent
			});
		}

		UpdateFinancialDetails();
		StateHasChanged();
	}

	private void RemoveFromReturn(SaleReturnProductCartModel item)
	{
		_saleReturnProductCart.Remove(item);
		UpdateFinancialDetails();
		StateHasChanged();
	}

	private void ClearReturnCart()
	{
		_saleReturnProductCart.Clear();
		UpdateFinancialDetails();
		StateHasChanged();
	}

	private async void UpdateFinancialDetails()
	{
		foreach (var item in _saleReturnProductCart)
		{
			item.BaseTotal = item.Rate * item.Quantity;
			item.DiscAmount = item.BaseTotal * (item.DiscPercent / 100);
			item.AfterDiscount = item.BaseTotal - item.DiscAmount;
			item.CGSTAmount = item.AfterDiscount * (item.CGSTPercent / 100);
			item.SGSTAmount = item.AfterDiscount * (item.SGSTPercent / 100);
			item.IGSTAmount = item.AfterDiscount * (item.IGSTPercent / 100);
			item.Total = item.AfterDiscount + item.CGSTAmount + item.SGSTAmount + item.IGSTAmount;
			item.NetRate = item.Quantity > 0 ? item.Total / item.Quantity : 0;
		}

		_sfProductGrid?.Refresh();
		StateHasChanged();

		// Save to storage after financial calculations
		await DataStorageService.LocalSaveAsync(StorageFileNames.SaleReturnDataFileName, System.Text.Json.JsonSerializer.Serialize(_saleReturn));
	}
	#endregion

	#region Saving
	private async Task<bool> ValidateForm()
	{
		_validationErrors.Clear();

		// Basic validations
		if (_saleReturn.SaleId <= 0)
		{
			_validationErrors.Add(new ValidationError { Field = "Sale Selection", Message = "Please select a sale to process returns" });
		}

		if (_saleReturnProductCart.Count == 0)
		{
			_validationErrors.Add(new ValidationError { Field = "Return Cart", Message = "Please add at least one product to the return cart" });
		}

		if (_saleReturn.LocationId <= 0)
		{
			_validationErrors.Add(new ValidationError { Field = "Location", Message = "Please select a valid location" });
		}

		// Validate each product in cart
		foreach (var item in _saleReturnProductCart)
		{
			if (item.Quantity <= 0)
			{
				_validationErrors.Add(new ValidationError { Field = $"Product: {item.ProductName}", Message = "Return quantity must be greater than zero" });
			}
			else if (item.Quantity > item.MaxQuantity)
			{
				_validationErrors.Add(new ValidationError { Field = $"Product: {item.ProductName}", Message = $"Return quantity cannot exceed maximum available ({item.MaxQuantity})" });
			}
		}

		// Show validation errors if any
		if (_validationErrors.Count != 0)
		{
			_validationErrorDialogVisible = true;
			StateHasChanged();
			return false;
		}

		// Set required fields if validation passes
		_saleReturn.UserId = _user.Id;
		return true;
	}

	private async Task ConfirmSaleReturn()
	{
		// Validate form first
		if (!await ValidateForm())
			return;

		// Close confirmation dialog and start saving
		_confirmReturnDialogVisible = false;
		_isSaving = true;
		StateHasChanged();

		try
		{
			// Save the return
			_saleReturn.Id = await SaleReturnData.SaveSaleReturn(_saleReturn, _saleReturnProductCart);

			// Print invoice
			await PrintInvoice();

			// Send notifications
			await SendLocalNotification();

			// Clear local storage
			await DataStorageService.LocalRemove(StorageFileNames.SaleReturnDataFileName);

			// Navigate to success page
			NavigationManager.NavigateTo("/SaleReturn/Confirmed", true);
		}
		catch (Exception ex)
		{
			_validationErrors.Add(new() { Field = "Save Error", Message = $"An error occurred while saving the sale return: {ex.Message}" });
			_validationErrorDialogVisible = true;
			StateHasChanged();
		}
		finally
		{
			_isSaving = false;
			StateHasChanged();
		}
	}

	private async Task PrintInvoice()
	{
		var memoryStream = await SaleReturnA4Print.GenerateA4SaleReturnBill(_saleReturn.Id);
		var fileName = $"Sale_Return_Bill_{_saleReturn.TransactionNo}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
		await SaveAndViewService.SaveAndView(fileName, "application/pdf", memoryStream);
	}

	private async Task SendLocalNotification()
	{
		var saleReturn = await SaleReturnData.LoadSaleReturnOverviewBySaleReturnId(_saleReturn.Id);

		await NotificationService.ShowLocalNotification(
			saleReturn.SaleReturnId,
			"Sale Return Placed",
			$"{saleReturn.TransactionNo}",
			$"Your sale return #{saleReturn.TransactionNo} has been successfully placed for sale #{saleReturn.SaleId} | Total Items: {saleReturn.TotalProducts} | Total Qty: {saleReturn.TotalQuantity} | Location: {saleReturn.LocationName} | User: {saleReturn.UserName} | Date: {saleReturn.ReturnDateTime:dd/MM/yy hh:mm tt} | Remarks: {saleReturn.Remarks}");
	}
	#endregion
}