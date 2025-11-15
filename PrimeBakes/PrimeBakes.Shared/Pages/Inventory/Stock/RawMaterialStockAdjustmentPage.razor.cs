using Microsoft.JSInterop;

using PrimeBakes.Shared.Services;

using PrimeBakesLibrary.Data;
using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Inventory.Purchase;
using PrimeBakesLibrary.Data.Inventory.Stock;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Inventory;
using PrimeBakesLibrary.Models.Inventory.Stock;

using Syncfusion.Blazor.DropDowns;
using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Inputs;
using Syncfusion.Blazor.Notifications;

namespace PrimeBakes.Shared.Pages.Inventory.Stock;

public partial class RawMaterialStockAdjustmentPage
{
	private bool _isLoading = true;
	private bool _isProcessing = false;

	private DateTime _transactionDateTime = DateTime.Now;
	private string _transactionNo = string.Empty;

	private FinancialYearModel _selectedFinancialYear = new();
	private RawMaterialModel? _selectedRawMaterial = new();
	private RawMaterialStockAdjustmentCartModel _selectedCart = new();

	private List<RawMaterialModel> _rawMaterials = [];
	private List<RawMaterialStockAdjustmentCartModel> _cart = [];
	private List<RawMaterialStockSummaryModel> _stockSummary = [];

	private SfAutoComplete<RawMaterialModel?, RawMaterialModel> _sfItemAutoComplete;
	private SfGrid<RawMaterialStockAdjustmentCartModel> _sfCartGrid;

	private string _errorTitle = string.Empty;
	private string _errorMessage = string.Empty;

	private string _successTitle = string.Empty;
	private string _successMessage = string.Empty;

	private SfToast _sfSuccessToast;
	private SfToast _sfErrorToast;

	#region Load Data
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		await AuthService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, UserRoles.Inventory, true);
		await LoadData();
		_isLoading = false;
		StateHasChanged();
	}

	private async Task LoadData()
	{
		_transactionDateTime = await CommonData.LoadCurrentDateTime();
		_transactionNo = await GenerateCodes.GenerateRawMaterialStockAdjustmentTransactionNo(_transactionDateTime);
		await LoadStock();
		await LoadItems();
		await LoadExistingCart();
	}

	private async Task LoadStock()
	{
		try
		{
			_selectedFinancialYear = await FinancialYearData.LoadFinancialYearByDateTime(_transactionDateTime);
			_stockSummary = await RawMaterialStockData.LoadRawMaterialStockSummaryByDate(_transactionDateTime, _transactionDateTime);
		}
		catch (Exception ex)
		{
			await ShowToast("An Error Occurred While Loading Stock Data", ex.Message, "error");
		}
	}

	private async Task LoadItems()
	{
		try
		{
			_rawMaterials = await PurchaseData.LoadRawMaterialByPartyPurchaseDateTime(0, _transactionDateTime);

			_rawMaterials = [.. _rawMaterials.OrderBy(s => s.Name)];
			_rawMaterials.Add(new()
			{
				Id = 0,
				Name = "Create New Item ..."
			});
		}
		catch (Exception ex)
		{
			await ShowToast("An Error Occurred While Loading Items", ex.Message, "error");
		}
	}

	private async Task LoadExistingCart()
	{
		try
		{
			_cart.Clear();

			if (await DataStorageService.LocalExists(StorageFileNames.RawMaterialStockAdjustmentCartDataFileName))
				_cart = System.Text.Json.JsonSerializer.Deserialize<List<RawMaterialStockAdjustmentCartModel>>(await DataStorageService.LocalGetAsync(StorageFileNames.RawMaterialStockAdjustmentCartDataFileName));
		}
		catch (Exception ex)
		{
			await ShowToast("An Error Occurred While Loading Existing Cart", ex.Message, "error");
			await DeleteLocalFiles();
		}
		finally
		{
			await SaveTransactionFile();
		}
	}
	#endregion

	#region Change Events
	private async Task OnTransactionDateChanged(Syncfusion.Blazor.Calendars.ChangedEventArgs<DateTime> args)
	{
		_transactionDateTime = args.Value;
		await LoadStock();
		await LoadItems();
		await SaveTransactionFile();
	}
	#endregion

	#region Cart
	private async Task OnItemChanged(ChangeEventArgs<RawMaterialModel?, RawMaterialModel> args)
	{
		if (args.Value is null)
			return;

		if (args.Value.Id == 0)
		{
			if (FormFactor.GetFormFactor() == "Web")
				await JSRuntime.InvokeVoidAsync("open", PageRouteNames.AdminRawMaterial, "_blank");
			else
				NavigationManager.NavigateTo(PageRouteNames.AdminRawMaterial);

			return;
		}

		_selectedRawMaterial = args.Value;

		if (_selectedRawMaterial is null)
			_selectedCart = new()
			{
				RawMaterialId = 0,
				RawMaterialName = "",
				Stock = 0,
				Quantity = 1,
				Total = 0,
				Rate = 0,
			};

		else
		{
			_selectedCart.Stock = _stockSummary.FirstOrDefault(s => s.RawMaterialId == _selectedRawMaterial.Id)?.ClosingStock ?? 0;
			_selectedCart.Quantity = _stockSummary.FirstOrDefault(s => s.RawMaterialId == _selectedRawMaterial.Id)?.ClosingStock ?? 0;
			_selectedCart.Rate = _selectedRawMaterial.Rate;
			_selectedCart.Total = _selectedCart.Rate * _selectedCart.Quantity;
		}

		UpdateSelectedItemFinancialDetails();
	}

	private void OnItemQuantityChanged(ChangeEventArgs<decimal> args)
	{
		_selectedCart.Quantity = args.Value;
		UpdateSelectedItemFinancialDetails();
	}

	private void UpdateSelectedItemFinancialDetails()
	{
		if (_selectedRawMaterial is null)
			return;

		_selectedCart.RawMaterialId = _selectedRawMaterial.Id;
		_selectedCart.RawMaterialName = _selectedRawMaterial.Name;
		_selectedCart.Rate = _selectedRawMaterial.Rate;
		_selectedCart.Stock = _stockSummary.FirstOrDefault(s => s.RawMaterialId == _selectedRawMaterial.Id)?.ClosingStock ?? 0;
		_selectedCart.Total = _selectedCart.Quantity * _selectedCart.Rate;

		StateHasChanged();
	}

	private async Task AddItemToCart()
	{
		if (_selectedRawMaterial is null || _selectedRawMaterial.Id <= 0)
		{
			await ShowToast("Invalid Item Details", "Please ensure all item details are correctly filled before adding to the cart.", "error");
			return;
		}

		UpdateSelectedItemFinancialDetails();

		var existingItem = _cart.FirstOrDefault(s => s.RawMaterialId == _selectedCart.RawMaterialId);
		if (existingItem is not null)
		{
			existingItem.Quantity = _selectedCart.Quantity;
			existingItem.Rate = _selectedCart.Rate;
		}
		else
			_cart.Add(new()
			{
				RawMaterialId = _selectedCart.RawMaterialId,
				RawMaterialName = _selectedCart.RawMaterialName,
				Stock = _selectedCart.Stock,
				Quantity = _selectedCart.Quantity,
				Rate = _selectedCart.Rate,
				Total = _selectedCart.Total
			});

		_selectedRawMaterial = null;
		_selectedCart = new();

		await _sfItemAutoComplete.FocusAsync();
		await SaveTransactionFile();
	}

	private async Task EditCartItem(RawMaterialStockAdjustmentCartModel cartItem)
	{
		_selectedRawMaterial = _rawMaterials.FirstOrDefault(s => s.Id == cartItem.RawMaterialId);

		if (_selectedRawMaterial is null)
			return;

		_selectedCart = new()
		{
			RawMaterialId = cartItem.RawMaterialId,
			RawMaterialName = cartItem.RawMaterialName,
			Stock = _stockSummary.FirstOrDefault(s => s.RawMaterialId == cartItem.RawMaterialId)?.ClosingStock ?? 0,
			Quantity = cartItem.Quantity,
			Rate = cartItem.Rate,
			Total = cartItem.Total
		};

		await _sfItemAutoComplete.FocusAsync();
		UpdateSelectedItemFinancialDetails();
		await RemoveItemFromCart(cartItem);
	}

	private async Task RemoveItemFromCart(RawMaterialStockAdjustmentCartModel cartItem)
	{
		_cart.Remove(cartItem);
		await SaveTransactionFile();
	}
	#endregion

	#region Saving
	private async Task UpdateFinancialDetails()
	{
		foreach (var item in _cart)
		{
			item.Stock = _stockSummary.FirstOrDefault(s => s.RawMaterialId == item.RawMaterialId)?.ClosingStock ?? 0;
			item.Quantity = item.Quantity;
			item.Total = item.Rate * item.Quantity;
		}

		#region Financial Year
		_selectedFinancialYear = await FinancialYearData.LoadFinancialYearByDateTime(_transactionDateTime);
		if (_selectedFinancialYear is null || _selectedFinancialYear.Locked || _selectedFinancialYear.Status == false)
		{
			await ShowToast("Invalid Transaction Date", "The selected transaction date does not fall within an active financial year.", "error");
			_transactionDateTime = await CommonData.LoadCurrentDateTime();
			_selectedFinancialYear = await FinancialYearData.LoadFinancialYearByDateTime(_transactionDateTime);
			_stockSummary = await RawMaterialStockData.LoadRawMaterialStockSummaryByDate(_transactionDateTime, _transactionDateTime);
		}
		#endregion

		_transactionNo = await GenerateCodes.GenerateRawMaterialStockAdjustmentTransactionNo(_transactionDateTime);
	}

	private async Task SaveTransactionFile()
	{
		if (_isProcessing || _isLoading)
			return;

		try
		{
			_isProcessing = true;

			await UpdateFinancialDetails();

			await DataStorageService.LocalSaveAsync(StorageFileNames.RawMaterialStockAdjustmentCartDataFileName, System.Text.Json.JsonSerializer.Serialize(_cart));
		}
		catch (Exception ex)
		{
			await ShowToast("An Error Occurred While Saving Transaction Data", ex.Message, "error");
		}
		finally
		{
			if (_sfCartGrid is not null)
				await _sfCartGrid?.Refresh();

			_isProcessing = false;
			StateHasChanged();
		}
	}

	private async Task<bool> ValidateForm()
	{
		if (_cart.Count == 0)
		{
			await ShowToast("Cart is Empty", "Please add at least one item to the cart before saving the transaction.", "error");
			return false;
		}

		if (string.IsNullOrWhiteSpace(_transactionNo))
		{
			await ShowToast("Transaction Number Missing", "Transaction number is missing for the adjustment.", "error");
			return false;
		}

		if (_transactionDateTime == default)
		{
			await ShowToast("Transaction Date Missing", "Please select a valid transaction date for the adjustment.", "error");
			return false;
		}

		if (_selectedFinancialYear is null || _selectedFinancialYear.Id <= 0)
		{
			await ShowToast("Financial Year Not Found", "The transaction date does not fall within any financial year. Please check the date and try again.", "error");
			return false;
		}

		if (_selectedFinancialYear.Locked)
		{
			await ShowToast("Financial Year Locked", "The financial year for the selected transaction date is locked. Please select a different date.", "error");
			return false;
		}

		if (_selectedFinancialYear.Status == false)
		{
			await ShowToast("Financial Year Inactive", "The financial year for the selected transaction date is inactive. Please select a different date.", "error");
			return false;
		}

		return true;
	}

	private async Task SaveTransaction()
	{
		if (_isProcessing || _isLoading)
			return;

		try
		{
			_isProcessing = true;

			await SaveTransactionFile();

			if (!await ValidateForm())
			{
				_isProcessing = false;
				return;
			}

			await RawMaterialStockData.SaveRawMaterialStockAdjustment(_transactionDateTime, _cart);
			await DeleteLocalFiles();
			NavigationManager.NavigateTo(PageRouteNames.RawMaterialStockAdjustment, true);

			await ShowToast("Save Transaction", "Transaction saved successfully!", "success");
		}
		catch (Exception ex)
		{
			await ShowToast("An Error Occurred While Saving Transaction", ex.Message, "error");
		}
		finally
		{
			_isProcessing = false;
		}
	}

	private async Task DeleteLocalFiles() =>
		await DataStorageService.LocalRemove(StorageFileNames.RawMaterialStockAdjustmentCartDataFileName);
	#endregion

	#region Utilities
	private async Task ResetPage(Microsoft.AspNetCore.Components.Web.MouseEventArgs args)
	{
		await DeleteLocalFiles();
		NavigationManager.NavigateTo(PageRouteNames.RawMaterialStockAdjustment, true);
	}

	private async Task NavigateToRawMaterialStockReportPage()
	{
		if (FormFactor.GetFormFactor() == "Web")
			await JSRuntime.InvokeVoidAsync("open", PageRouteNames.ReportRawMaterialStock, "_blank");
		else
			NavigationManager.NavigateTo(PageRouteNames.ReportRawMaterialStock);
	}

	private async Task ShowToast(string title, string message, string type)
	{
		VibrationService.VibrateWithTime(200);

		if (type == "error")
		{
			_errorTitle = title;
			_errorMessage = message;
			await _sfErrorToast.ShowAsync(new()
			{
				Title = _errorTitle,
				Content = _errorMessage
			});
		}

		else if (type == "success")
		{
			_successTitle = title;
			_successMessage = message;
			await _sfSuccessToast.ShowAsync(new()
			{
				Title = _successTitle,
				Content = _successMessage
			});
		}
	}
	#endregion
}