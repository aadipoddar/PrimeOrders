using PrimeBakes.Shared.Components;

using PrimeBakesLibrary.Data;
using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;

using Syncfusion.Blazor.Grids;
using PrimeBakesLibrary.Exporting.Operations;
using PrimeBakesLibrary.Models.Sales.Product;
using PrimeBakesLibrary.Data.Sales.Product;

namespace PrimeBakes.Shared.Pages.Admin.Operations;

public partial class LocationPage : IAsyncDisposable
{
	private HotKeysContext _hotKeysContext;
	private bool _isLoading = true;
	private bool _isProcessing = false;
	private bool _showDeleted = false;

	private LocationModel _location = new();
	private LocationModel _copyLocation;

	private List<LocationModel> _locations = [];

	private SfGrid<LocationModel> _sfGrid;
	private DeleteConfirmationDialog _deleteConfirmationDialog;
	private RecoverConfirmationDialog _recoverConfirmationDialog;

	private int _deleteLocationId = 0;
	private string _deleteLocationName = string.Empty;

	private int _recoverLocationId = 0;
	private string _recoverLocationName = string.Empty;

	private ToastNotification _toastNotification;

	#region Load Data
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, UserRoles.Admin, true);
		await LoadData();
		_isLoading = false;
		StateHasChanged();
	}

	private async Task LoadData()
	{
		_hotKeysContext = HotKeys.CreateContext()
			.Add(ModCode.Ctrl, Code.S, SaveLocation, "Save", Exclude.None)
			.Add(ModCode.Ctrl, Code.N, () => NavigationManager.NavigateTo(PageRouteNames.AdminLocation, true), "New", Exclude.None)
			.Add(ModCode.Ctrl, Code.E, ExportExcel, "Export Excel", Exclude.None)
			.Add(ModCode.Ctrl, Code.P, ExportPdf, "Export PDF", Exclude.None)
			.Add(ModCode.Ctrl, Code.D, () => NavigationManager.NavigateTo(PageRouteNames.Dashboard), "Dashboard", Exclude.None)
			.Add(ModCode.Ctrl, Code.B, () => NavigationManager.NavigateTo(PageRouteNames.AdminDashboard), "Back", Exclude.None)
			.Add(Code.Insert, EditSelectedItem, "Edit selected", Exclude.None)
			.Add(Code.Delete, DeleteSelectedItem, "Delete selected", Exclude.None);

		_locations = await CommonData.LoadTableData<LocationModel>(TableNames.Location);

		if (!_showDeleted)
			_locations = [.. _locations.Where(l => l.Status)];

		if (_sfGrid is not null)
			await _sfGrid.Refresh();
	}
	#endregion

	#region Actions
	private void OnEditLocation(LocationModel location)
	{
		_location = new()
		{
			Id = location.Id,
			Name = location.Name,
			PrefixCode = location.PrefixCode,
			Discount = location.Discount,
			Remarks = location.Remarks,
			Status = location.Status
		};

		StateHasChanged();
	}

	private async Task ShowDeleteConfirmation(int id, string name)
	{
		_deleteLocationId = id;
		_deleteLocationName = name;
		await _deleteConfirmationDialog.ShowAsync();
	}

	private async Task CancelDelete()
	{
		_deleteLocationId = 0;
		_deleteLocationName = string.Empty;
		await _deleteConfirmationDialog.HideAsync();
	}

	private async Task ConfirmDelete()
	{
		try
		{
			_isProcessing = true;
			await _deleteConfirmationDialog.HideAsync();

			var location = _locations.FirstOrDefault(l => l.Id == _deleteLocationId);
			if (location == null)
			{
				await _toastNotification.ShowAsync("Error", "Location not found.", ToastType.Error);
				return;
			}

			if (location.Id == 1 && location.Status)
			{
				await _toastNotification.ShowAsync("Error", "Cannot delete main location.", ToastType.Error);
				return;
			}

			location.Status = false;
			await LocationData.InsertLocation(location);

			await _toastNotification.ShowAsync("Deleted", $"Location '{location.Name}' removed successfully.", ToastType.Success);
			NavigationManager.NavigateTo(PageRouteNames.AdminLocation, true);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"Failed to delete location: {ex.Message}", ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
			_deleteLocationId = 0;
			_deleteLocationName = string.Empty;
		}
	}

	private async Task ShowRecoverConfirmation(int id, string name)
	{
		_recoverLocationId = id;
		_recoverLocationName = name;
		await _recoverConfirmationDialog.ShowAsync();
	}

	private async Task CancelRecover()
	{
		_recoverLocationId = 0;
		_recoverLocationName = string.Empty;
		await _recoverConfirmationDialog.HideAsync();
	}

	private async Task ToggleDeleted()
	{
		_showDeleted = !_showDeleted;
		await LoadData();
		StateHasChanged();
	}

	private async Task ConfirmRecover()
	{
		try
		{
			_isProcessing = true;
			await _recoverConfirmationDialog.HideAsync();

			var location = _locations.FirstOrDefault(l => l.Id == _recoverLocationId);
			if (location == null)
			{
				await _toastNotification.ShowAsync("Error", "Location not found.", ToastType.Error);
				return;
			}

			location.Status = true;
			await LocationData.InsertLocation(location);

			await _toastNotification.ShowAsync("Recovered", $"Location '{location.Name}' restored successfully.", ToastType.Success);
			NavigationManager.NavigateTo(PageRouteNames.AdminLocation, true);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"Failed to recover location: {ex.Message}", ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
			_recoverLocationId = 0;
			_recoverLocationName = string.Empty;
		}
	}
	#endregion

	#region Saving
	private async Task<bool> ValidateForm()
	{
		_location.Name = _location.Name?.Trim() ?? "";
		_location.PrefixCode = _location.PrefixCode?.Trim() ?? "";

		_location.Name = _location.Name?.ToUpper() ?? "";
		_location.PrefixCode = _location.PrefixCode?.ToUpper() ?? "";

		_location.Remarks = _location.Remarks?.Trim() ?? "";
		_location.Status = true;

		if (string.IsNullOrWhiteSpace(_location.Name))
		{
			await _toastNotification.ShowAsync("Validation", "Location name is required.", ToastType.Warning);
			return false;
		}

		if (string.IsNullOrWhiteSpace(_location.PrefixCode))
		{
			await _toastNotification.ShowAsync("Validation", "Prefix code is required.", ToastType.Warning);
			return false;
		}

		if (string.IsNullOrWhiteSpace(_location.Remarks))
			_location.Remarks = null;

		if (_location.Id > 0)
		{
			var existingLocation = _locations.FirstOrDefault(_ => _.Id != _location.Id && _.PrefixCode.Equals(_location.PrefixCode, StringComparison.OrdinalIgnoreCase));
			if (existingLocation is not null)
			{
				await _toastNotification.ShowAsync("Validation", $"Prefix code '{_location.PrefixCode}' already exists.", ToastType.Warning);
				return false;
			}

			existingLocation = _locations.FirstOrDefault(_ => _.Id != _location.Id && _.Name.Equals(_location.Name, StringComparison.OrdinalIgnoreCase));
			if (existingLocation is not null)
			{
				await _toastNotification.ShowAsync("Validation", $"Location name '{_location.Name}' already exists.", ToastType.Warning);
				return false;
			}
		}
		else
		{
			var existingLocation = _locations.FirstOrDefault(_ => _.PrefixCode.Equals(_location.PrefixCode, StringComparison.OrdinalIgnoreCase));
			if (existingLocation is not null)
			{
				await _toastNotification.ShowAsync("Validation", $"Prefix code '{_location.PrefixCode}' already exists.", ToastType.Warning);
				return false;
			}

			existingLocation = _locations.FirstOrDefault(_ => _.Name.Equals(_location.Name, StringComparison.OrdinalIgnoreCase));
			if (existingLocation is not null)
			{
				await _toastNotification.ShowAsync("Validation", $"Location name '{_location.Name}' already exists.", ToastType.Warning);
				return false;
			}
		}

		if (_location.Discount < 0 || _location.Discount > 100)
		{
			await _toastNotification.ShowAsync("Validation", "Discount must be between 0% and 100%.", ToastType.Warning);
			return false;
		}

		return true;
	}

	private async Task SaveLocation()
	{
		if (_isProcessing)
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();

			if (!await ValidateForm())
			{
				_isProcessing = false;
				return;
			}

			await _toastNotification.ShowAsync("Saving", "Processing location...", ToastType.Info);

			var isNewLocation = _location.Id == 0;
			_location.Id = await LocationData.InsertLocation(_location);
			await InsertLedger();
			await InsertProducts(isNewLocation);

			await _toastNotification.ShowAsync("Saved", $"Location '{_location.Name}' saved successfully.", ToastType.Success);
			NavigationManager.NavigateTo(PageRouteNames.AdminLocation, true);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"Failed to save location: {ex.Message}", ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
		}
	}

	private async Task InsertLedger()
	{
		try
		{
			var ledgers = await CommonData.LoadTableData<LedgerModel>(TableNames.Ledger);
			var ledger = ledgers.FirstOrDefault(_ => _.LocationId == _location.Id);
			var primaryCompany = await SettingsData.LoadSettingsByKey(SettingsKeys.PrimaryCompanyLinkingId);
			var company = await CommonData.LoadTableDataById<CompanyModel>(TableNames.Company, int.Parse(primaryCompany.Value));

			if (ledger is not null && ledger.Id > 0)
			{
				ledger.Name = _location.Name;
				ledger.Remarks = _location.Remarks;
				ledger.StateUTId = company.StateUTId;
				await LedgerData.InsertLedger(ledger);
			}

			else
				await LedgerData.InsertLedger(new()
				{
					Id = ledger?.Id ?? 0,
					Name = _location.Name,
					LocationId = _location.Id,
					Code = ledger?.Code ?? await GenerateCodes.GenerateLedgerCode(),
					AccountTypeId = 3,
					GroupId = 1,
					Remarks = _location.Remarks,
					StateUTId = company.StateUTId,
					Status = true
				});
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"Failed to create ledger: {ex.Message}", ToastType.Error);
		}
	}

	private async Task InsertProducts(bool isNewLocation)
	{
		try
		{
			if (_copyLocation is not null && _copyLocation.Id > 0)
			{
				if (_copyLocation.Id == _location.Id)
				{
					await _toastNotification.ShowAsync("Warning", "Cannot copy products from the same location.", ToastType.Warning);
					return;
				}

				var existingProductLocations = await ProductData.LoadProductByLocation(_location.Id);
				foreach (var existingProductLocation in existingProductLocations)
					await ProductData.InsertProductLocation(new()
					{
						Id = existingProductLocation.Id,
						ProductId = existingProductLocation.ProductId,
						LocationId = _location.Id,
						Rate = existingProductLocation.Rate,
						Status = false
					});

				var productLocations = await ProductData.LoadProductByLocation(_copyLocation.Id);
				foreach (var productLocation in productLocations)
					await ProductData.InsertProductLocation(new()
					{
						Id = 0,
						ProductId = productLocation.ProductId,
						LocationId = _location.Id,
						Rate = productLocation.Rate,
						Status = true
					});
			}

			else if (isNewLocation)
			{
				var existingProductLocations = await ProductData.LoadProductByLocation(_location.Id);
				foreach (var existingProductLocation in existingProductLocations)
					await ProductData.InsertProductLocation(new()
					{
						Id = existingProductLocation.Id,
						ProductId = existingProductLocation.ProductId,
						LocationId = _location.Id,
						Rate = existingProductLocation.Rate,
						Status = false
					});

				var products = await CommonData.LoadTableDataByStatus<ProductModel>(TableNames.Product);
				foreach (var product in products)
					await ProductData.InsertProductLocation(new()
					{
						Id = 0,
						ProductId = product.Id,
						LocationId = _location.Id,
						Rate = product.Rate,
						Status = true
					});
			}
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"Failed to copy products: {ex.Message}", ToastType.Error);
		}
	}
	#endregion

	#region Exporting
	private async Task ExportExcel()
	{
		if (_isProcessing)
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();
			await _toastNotification.ShowAsync("Exporting", "Generating Excel file...", ToastType.Info);

			// Call the Excel export utility
			var stream = await LocationExcelExport.ExportLocation(_locations);

			// Generate file name
			string fileName = "LOCATION_MASTER.xlsx";

			// Save and view the Excel file
			await SaveAndViewService.SaveAndView(fileName, stream);

			await _toastNotification.ShowAsync("Exported", "Excel file downloaded successfully.", ToastType.Success);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"Excel export failed: {ex.Message}", ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
			StateHasChanged();
		}
	}

	private async Task ExportPdf()
	{
		if (_isProcessing)
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();
			await _toastNotification.ShowAsync("Exporting", "Generating PDF file...", ToastType.Info);

			// Call the PDF export utility
			var stream = await LocationPDFExport.ExportLocation(_locations);

			// Generate file name
			string fileName = "LOCATION_MASTER.pdf";

			// Save and view the PDF file
			await SaveAndViewService.SaveAndView(fileName, stream);

			await _toastNotification.ShowAsync("Exported", "PDF file downloaded successfully.", ToastType.Success);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"PDF export failed: {ex.Message}", ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
			StateHasChanged();
		}
	}
	#endregion

	private async Task EditSelectedItem()
	{
		var selectedRecords = await _sfGrid.GetSelectedRecordsAsync();
		if (selectedRecords.Count > 0)
			OnEditLocation(selectedRecords[0]);
	}

	private async Task DeleteSelectedItem()
	{
		var selectedRecords = await _sfGrid.GetSelectedRecordsAsync();
		if (selectedRecords.Count > 0)
		{
			if (selectedRecords[0].Status)
				ShowDeleteConfirmation(selectedRecords[0].Id, selectedRecords[0].Name);
			else
				ShowRecoverConfirmation(selectedRecords[0].Id, selectedRecords[0].Name);
		}
	}

	public async ValueTask DisposeAsync()
	{
		await _hotKeysContext.DisposeAsync();
	}
}