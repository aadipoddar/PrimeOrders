using Microsoft.AspNetCore.Components;

using PrimeBakes.Shared.Services;

using PrimeBakesLibrary.Data;
using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Inventory.Kitchen;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Exporting.Kitchen;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Inventory.Kitchen;

using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Popups;

namespace PrimeBakes.Shared.Pages.Inventory.Kitchen;

public partial class KitchenIssueCartPage
{
	private bool _isLoading = true;
	private bool _isSaving = false;

	private UserModel _user;

	private List<KitchenIssueRawMaterialCartModel> _cart = [];
	private List<KitchenModel> _kitchens = [];

	private KitchenIssueModel _kitchenIssue = new()
	{
		Id = 0,
		TransactionNo = "",
		KitchenId = 0,
		UserId = 0,
		IssueDate = DateTime.Now,
		CreatedAt = DateTime.Now,
		LocationId = 1,
		Status = true,
		Remarks = ""
	};
	private KitchenIssueRawMaterialCartModel _selectedRawMaterialForEdit;

	// Grid Reference
	private SfGrid<KitchenIssueRawMaterialCartModel> _sfGrid;

	// Dialog References and Visibility
	private SfDialog _sfIssueDetailsDialog;
	private SfDialog _sfIssueConfirmationDialog;
	private SfDialog _sfRawMaterialDetailsDialog;

	private bool _issueDetailsDialogVisible = false;
	private bool _issueConfirmationDialogVisible = false;
	private bool _rawMaterialDetailsDialogVisible = false;
	private bool _validationErrorDialogVisible = false;

	private readonly List<ValidationError> _validationErrors = [];
	public class ValidationError
	{
		public string Field { get; set; } = string.Empty;
		public string Message { get; set; } = string.Empty;
	}

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
		_kitchens = await CommonData.LoadTableDataByStatus<KitchenModel>(TableNames.Kitchen);

		_kitchenIssue.UserId = _user.Id;
		_kitchenIssue.KitchenId = _kitchens.FirstOrDefault()?.Id ?? 0;
		_kitchenIssue.IssueDate = DateTime.Now;
		_kitchenIssue.LocationId = 1;
		_kitchenIssue.TransactionNo = await GenerateCodes.GenerateKitchenIssueTransactionNo(_kitchenIssue);
		_kitchenIssue.CreatedAt = DateTime.Now;

		_cart.Clear();
		if (await DataStorageService.LocalExists(StorageFileNames.KitchenIssueCartDataFileName))
		{
			var items = System.Text.Json.JsonSerializer.Deserialize<List<KitchenIssueRawMaterialCartModel>>(await DataStorageService.LocalGetAsync(StorageFileNames.KitchenIssueCartDataFileName)) ?? [];
			foreach (var item in items)
				_cart.Add(item);
		}

		_cart.Sort((x, y) => string.Compare(x.RawMaterialName, y.RawMaterialName, StringComparison.Ordinal));

		if (await DataStorageService.LocalExists(StorageFileNames.KitchenIssueDataFileName))
			_kitchenIssue = System.Text.Json.JsonSerializer.Deserialize<KitchenIssueModel>(await DataStorageService.LocalGetAsync(StorageFileNames.KitchenIssueDataFileName)) ?? new()
			{
				Id = 0,
				TransactionNo = "",
				KitchenId = 0,
				UserId = 0,
				IssueDate = DateTime.Now,
				CreatedAt = DateTime.Now,
				LocationId = 1,
				Status = true,
				Remarks = ""
			};

		if (_sfGrid is not null)
			await _sfGrid.Refresh();

		StateHasChanged();
	}
	#endregion

	#region Raw Material
	private async Task UpdateQuantity(KitchenIssueRawMaterialCartModel item, decimal newQuantity)
	{
		if (item is null || _isSaving)
			return;

		item.Quantity = Math.Max(0, newQuantity);

		if (item.Quantity == 0)
			_cart.Remove(item);

		await SaveKitchenIssueFile();
	}

	private async Task ClearCart()
	{
		if (_isSaving)
			return;

		_cart.Clear();
		VibrationService.VibrateWithTime(500);
		await SaveKitchenIssueFile();
	}

	private void OpenRawMaterialDetailsDialog(KitchenIssueRawMaterialCartModel item)
	{
		_selectedRawMaterialForEdit = item;
		_rawMaterialDetailsDialogVisible = true;
	}

	private async Task OnBasicQuantityChanged(decimal quantity)
	{
		if (_selectedRawMaterialForEdit is not null)
			_selectedRawMaterialForEdit.Quantity = Math.Max(0, quantity);

		await SaveKitchenIssueFile();
	}

	private async Task OnMeasurementUnitChanged(string unit)
	{
		if (_selectedRawMaterialForEdit is not null)
			_selectedRawMaterialForEdit.MeasurementUnit = unit;

		await SaveKitchenIssueFile();
	}

	private async Task OnRateChanged(decimal rate)
	{
		if (_selectedRawMaterialForEdit is not null)
			_selectedRawMaterialForEdit.Rate = Math.Max(0, rate);

		await SaveKitchenIssueFile();
	}

	private async Task OnSaveBasicInfoClick()
	{
		_rawMaterialDetailsDialogVisible = false;
		await SaveKitchenIssueFile();
	}
	#endregion

	#region Saving
	private async Task SaveKitchenIssueFile()
	{
		foreach (var item in _cart)
			item.Total = item.Quantity * item.Rate;

		await DataStorageService.LocalSaveAsync(StorageFileNames.KitchenIssueDataFileName, System.Text.Json.JsonSerializer.Serialize(_kitchenIssue));

		if (!_cart.Any(x => x.Quantity > 0) && await DataStorageService.LocalExists(StorageFileNames.KitchenIssueCartDataFileName))
			await DataStorageService.LocalRemove(StorageFileNames.KitchenIssueCartDataFileName);
		else
			await DataStorageService.LocalSaveAsync(StorageFileNames.KitchenIssueCartDataFileName, System.Text.Json.JsonSerializer.Serialize(_cart.Where(_ => _.Quantity > 0)));

		VibrationService.VibrateHapticClick();
		await _sfGrid.Refresh();
		StateHasChanged();
	}

	private bool ValidateForm()
	{
		_validationErrors.Clear();

		_kitchenIssue.LocationId = 1;
		_kitchenIssue.UserId = _user.Id;

		if (_kitchenIssue.KitchenId <= 0)
		{
			_validationErrors.Add(new()
			{
				Field = "Kitchen",
				Message = "Please select a valid Kitchen for the Issue."
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

	private async Task SaveKitchenIssue()
	{
		if (_isSaving)
			return;

		_isSaving = true;
		StateHasChanged();

		try
		{
			await SaveKitchenIssueFile();

			if (!ValidateForm())
			{
				_validationErrorDialogVisible = true;
				_issueConfirmationDialogVisible = false;
				return;
			}

			_kitchenIssue.Id = await KitchenIssueData.SaveKitchenIssue(_kitchenIssue, _cart);
			await DeleteCart();
			await PrintInvoice();
			await SendLocalNotification();
			NavigationManager.NavigateTo("/Inventory/Kitchen-Issue/Confirmed", true);
		}
		catch (Exception ex)
		{
			_validationErrors.Add(new()
			{
				Field = "Exception",
				Message = $"An error occurred while saving the Issue: {ex.Message}"
			});
			_validationErrorDialogVisible = true;
			_issueConfirmationDialogVisible = false;
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
		await DataStorageService.LocalRemove(StorageFileNames.KitchenIssueCartDataFileName);
	}

	private async Task PrintInvoice()
	{
		var ms = await KitchenIssueA4Print.GenerateA4KitchenIssueBill(_kitchenIssue.Id);
		var fileName = $"Kitchen_Issue_{_kitchenIssue.TransactionNo}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
		await SaveAndViewService.SaveAndView(fileName, "application/pdf", ms);
	}

	private async Task SendLocalNotification()
	{
		var kitchenIssue = await KitchenIssueData.LoadKitchenIssueOverviewByKitchenIssueId(_kitchenIssue.Id);
		await NotificationService.ShowLocalNotification(
			kitchenIssue.KitchenId,
			"Kitchen Issue Placed",
			$"{kitchenIssue.TransactionNo}",
			$"Your kitchen issue #{kitchenIssue.TransactionNo} has been successfully placed | Total Items: {kitchenIssue.TotalProducts} | Total Qty: {kitchenIssue.TotalQuantity} | User: {kitchenIssue.UserName} | Date: {kitchenIssue.IssueDate:dd/MM/yy hh:mm tt}");
	}
	#endregion
}