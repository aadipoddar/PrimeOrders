using PrimeBakes.Shared.Services;

using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Inventory.Stock;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Inventory;
using PrimeBakesLibrary.Models.Inventory.Stock;

using Syncfusion.Blazor.DropDowns;
using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Popups;

namespace PrimeBakes.Shared.Pages.Inventory.Stock;

public partial class RawMaterialStockAdjustmentPage
{
	private UserModel _user;
	private bool _isLoading = true;
	private bool _isSaving = false;

	private int _selectedCategoryId = 0;
	private bool _adjustmentConfirmationDialogVisible = false;
	private bool _validationErrorDialogVisible = false;

	private List<RawMaterialCategoryModel> _rawMaterialCategories = [];
	private List<RawMaterialStockAdjustmentCartModel> _cart = [];
	private readonly List<ValidationError> _validationErrors = [];

	public class ValidationError
	{
		public string Field { get; set; } = string.Empty;
		public string Message { get; set; } = string.Empty;
	}

	private SfGrid<RawMaterialStockAdjustmentCartModel> _sfGrid;

	private SfDialog _sfAdjustmentConfirmationDialog;
	private SfDialog _sfValidationErrorDialog;

	#region Load Data
	protected override async Task OnInitializedAsync()
	{
		var authResult = await AuthService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, UserRoles.Inventory, true);
		_user = authResult.User;
		await LoadData();
		_isLoading = false;
	}

	private async Task LoadData()
	{
		_rawMaterialCategories = await CommonData.LoadTableDataByStatus<RawMaterialCategoryModel>(TableNames.RawMaterialCategory);
		_rawMaterialCategories.Add(new()
		{
			Id = 0,
			Name = "All Categories"
		});
		_rawMaterialCategories.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.Ordinal));
		_selectedCategoryId = 0;

		var allProducts = await CommonData.LoadTableDataByStatus<RawMaterialModel>(TableNames.RawMaterial);

		foreach (var product in allProducts)
			_cart.Add(new()
			{
				RawMaterialCategoryId = product.RawMaterialCategoryId,
				RawMaterialId = product.Id,
				RawMaterialName = product.Name,
				Quantity = 0,
				Rate = product.Rate,
				Total = 0
			});

		_cart.Sort((x, y) => string.Compare(x.RawMaterialName, y.RawMaterialName, StringComparison.Ordinal));

		var stockSummary = await RawMaterialStockData.LoadRawMaterialStockSummaryByDateLocationId(
			DateTime.Now.AddDays(-1),
			DateTime.Now.AddDays(1),
			1);

		foreach (var item in stockSummary)
			if (_cart.Any(c => c.RawMaterialId == item.RawMaterialId))
				_cart.FirstOrDefault(c => c.RawMaterialId == item.RawMaterialId).Quantity = item.ClosingStock;

		if (_sfGrid is not null)
			await _sfGrid.Refresh();

		StateHasChanged();
	}
	#endregion

	#region Raw Material
	private async Task OnRawMaterialCategoryChanged(ChangeEventArgs<int, RawMaterialCategoryModel> args)
	{
		if (args is null || args.Value <= 0)
			_selectedCategoryId = 0;
		else
			_selectedCategoryId = args.Value;

		await SaveAdjustmentFile();
	}

	private async Task UpdateQuantity(RawMaterialStockAdjustmentCartModel item, decimal newQuantity)
	{
		if (item is null)
			return;

		item.Quantity = newQuantity;
		await SaveAdjustmentFile();
	}
	#endregion

	#region Dialog Methods
	private void CloseConfirmationDialog()
	{
		_adjustmentConfirmationDialogVisible = false;
		StateHasChanged();
	}
	#endregion

	#region Saving
	private async Task SaveAdjustmentFile()
	{
		VibrationService.VibrateHapticClick();
		await _sfGrid.Refresh();
		StateHasChanged();
	}

	private bool ValidateForm()
	{
		_validationErrors.Clear();

		// Check for extremely negative quantities that might be unintentional
		var extremeNegativeItems = _cart.Where(x => x.Quantity < -1000).ToList();
		if (extremeNegativeItems.Count != 0)
		{
			_validationErrors.Add(new()
			{
				Field = "Large Negative Quantities",
				Message = $"Some items have very large negative quantities. Please verify: {string.Join(", ", extremeNegativeItems.Select(x => $"{x.RawMaterialName} ({x.Quantity})"))}"
			});
		}

		return _validationErrors.Count == 0;
	}

	private async Task ConfirmAdjustment()
	{
		if (_isSaving)
			return;

		_isSaving = true;
		StateHasChanged();

		try
		{
			await SaveAdjustmentFile();

			if (!ValidateForm())
			{
				_validationErrorDialogVisible = true;
				_adjustmentConfirmationDialogVisible = false;
				return;
			}

			await RawMaterialStockData.SaveRawMaterialStockAdjustment(_cart);
			_cart.Clear();
			await SendLocalNotification();
			NavigationManager.NavigateTo("/Inventory/RawMaterialStockAdjustment/Confirmed", true);
		}
		catch (Exception ex)
		{
			_validationErrors.Add(new()
			{
				Field = "System Error",
				Message = $"An error occurred while saving the adjustment: {ex.Message}"
			});
			_validationErrorDialogVisible = true;
			_adjustmentConfirmationDialogVisible = false;
		}
		finally
		{
			_isSaving = false;
			StateHasChanged();
		}
	}

	private async Task SendLocalNotification()
	{
		await NotificationService.ShowLocalNotification(
			100,
			 "Raw Material Stock Adjustment Saved",
			 "Stock Adjusted.",
			   $"Raw Material Stock Adjustment has been successfully saved on {DateTime.Now:dd/MM/yy hh:mm tt}. Please check the Stock Adjustment report for details.");
	}
	#endregion
}