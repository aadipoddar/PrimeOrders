using PrimeBakes.Shared.Services;

using PrimeBakesLibrary.Data;
using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;

using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Notifications;
using Syncfusion.Blazor.Popups;
using PrimeBakesLibrary.Exporting.Operations;
using PrimeBakesLibrary.Models.Sales.Product;
using PrimeBakesLibrary.Data.Sales.Product;

namespace PrimeBakes.Shared.Pages.Admin.Operations;

public partial class LocationPage
{
	private bool _isLoading = true;
	private bool _isProcessing = false;
	private bool _showDeleted = false;

	private LocationModel _location = new();
	private LocationModel _copyLocation;

	private List<LocationModel> _locations = [];

	private SfGrid<LocationModel> _sfGrid;
	private SfDialog _deleteConfirmationDialog;
	private SfDialog _recoverConfirmationDialog;

	private int _deleteLocationId = 0;
	private string _deleteLocationName = string.Empty;
	private bool _isDeleteDialogVisible = false;

	private int _recoverLocationId = 0;
	private string _recoverLocationName = string.Empty;
	private bool _isRecoverDialogVisible = false;

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

		await AuthService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, UserRoles.Admin, true);
		await LoadData();
		_isLoading = false;
		StateHasChanged();
	}

	private async Task LoadData()
	{
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

	private void ShowDeleteConfirmation(int id, string name)
	{
		_deleteLocationId = id;
		_deleteLocationName = name;
		_isDeleteDialogVisible = true;
		StateHasChanged();
	}

	private void CancelDelete()
	{
		_deleteLocationId = 0;
		_deleteLocationName = string.Empty;
		_isDeleteDialogVisible = false;
		StateHasChanged();
	}

	private async Task ConfirmDelete()
	{
		try
		{
			_isProcessing = true;
			_isDeleteDialogVisible = false;

			var location = _locations.FirstOrDefault(l => l.Id == _deleteLocationId);
			if (location == null)
			{
				await ShowToast("Error", "Location not found.", "error");
				return;
			}

			if (location.Id == 1 && location.Status)
			{
				await ShowToast("Error", "Cannot delete the main location. It must remain active for system operations.", "error");
				return;
			}

			location.Status = false;
			await LocationData.InsertLocation(location);

			await ShowToast("Success", $"Location '{location.Name}' has been deleted successfully.", "success");
			NavigationManager.NavigateTo(PageRouteNames.AdminLocation, true);
		}
		catch (Exception ex)
		{
			await ShowToast("Error", $"Failed to delete location: {ex.Message}", "error");
		}
		finally
		{
			_isProcessing = false;
			_deleteLocationId = 0;
			_deleteLocationName = string.Empty;
		}
	}

	private void ShowRecoverConfirmation(int id, string name)
	{
		_recoverLocationId = id;
		_recoverLocationName = name;
		_isRecoverDialogVisible = true;
		StateHasChanged();
	}

	private void CancelRecover()
	{
		_recoverLocationId = 0;
		_recoverLocationName = string.Empty;
		_isRecoverDialogVisible = false;
		StateHasChanged();
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
			_isRecoverDialogVisible = false;

			var location = _locations.FirstOrDefault(l => l.Id == _recoverLocationId);
			if (location == null)
			{
				await ShowToast("Error", "Location not found.", "error");
				return;
			}

			location.Status = true;
			await LocationData.InsertLocation(location);

			await ShowToast("Success", $"Location '{location.Name}' has been recovered successfully.", "success");
			NavigationManager.NavigateTo(PageRouteNames.AdminLocation, true);
		}
		catch (Exception ex)
		{
			await ShowToast("Error", $"Failed to recover location: {ex.Message}", "error");
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
			await ShowToast("Error", "Location name is required. Please enter a valid location name.", "error");
			return false;
		}

		if (string.IsNullOrWhiteSpace(_location.PrefixCode))
		{
			await ShowToast("Error", "Prefix code is required. Please enter a valid prefix code for the location.", "error");
			return false;
		}

		if (string.IsNullOrWhiteSpace(_location.Remarks))
			_location.Remarks = null;

		if (_location.Id > 0)
		{
			var existingLocation = _locations.FirstOrDefault(_ => _.Id != _location.Id && _.PrefixCode.Equals(_location.PrefixCode, StringComparison.OrdinalIgnoreCase));
			if (existingLocation is not null)
			{
				await ShowToast("Error", $"Prefix code '{_location.PrefixCode}' is already used by location '{existingLocation.Name}'. Please choose a different prefix code.", "error");
				return false;
			}

			existingLocation = _locations.FirstOrDefault(_ => _.Id != _location.Id && _.Name.Equals(_location.Name, StringComparison.OrdinalIgnoreCase));
			if (existingLocation is not null)
			{
				await ShowToast("Error", $"Location name '{_location.Name}' already exists. Please choose a different name.", "error");
				return false;
			}
		}
		else
		{
			var existingLocation = _locations.FirstOrDefault(_ => _.PrefixCode.Equals(_location.PrefixCode, StringComparison.OrdinalIgnoreCase));
			if (existingLocation is not null)
			{
				await ShowToast("Error", $"Prefix code '{_location.PrefixCode}' is already used by location '{existingLocation.Name}'. Please choose a different prefix code.", "error");
				return false;
			}

			existingLocation = _locations.FirstOrDefault(_ => _.Name.Equals(_location.Name, StringComparison.OrdinalIgnoreCase));
			if (existingLocation is not null)
			{
				await ShowToast("Error", $"Location name '{_location.Name}' already exists. Please choose a different name.", "error");
				return false;
			}
		}

		if (_location.Discount < 0 || _location.Discount > 100)
		{
			await ShowToast("Error", $"Discount must be between 0% and 100%. Current value: {_location.Discount}%", "error");
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

			if (!await ValidateForm())
			{
				_isProcessing = false;
				return;
			}

			var isNewLocation = _location.Id == 0;
			_location.Id = await LocationData.InsertLocation(_location);
			await InsertLedger();
			await InsertProducts(isNewLocation);

			await ShowToast("Success", $"Location '{_location.Name}' has been saved successfully.", "success");
			NavigationManager.NavigateTo(PageRouteNames.AdminLocation, true);
		}
		catch (Exception ex)
		{
			await ShowToast("Error", $"Failed to save location: {ex.Message}", "error");
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
			await ShowToast("Error", $"Failed to create or update ledger for location: {ex.Message}", "error");
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
					await ShowToast("Error", "Cannot copy products from the same location. Please select a different location to copy products from.", "error");
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
			await ShowToast("Error", $"Failed to copy products to new location: {ex.Message}", "error");
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

			// Call the Excel export utility
			var stream = await Task.Run(() => LocationExcelExport.ExportLocation(_locations));

			// Generate file name
			string fileName = "LOCATION_MASTER.xlsx";

			// Save and view the Excel file
			await SaveAndViewService.SaveAndView(fileName, stream);

			await ShowToast("Success", "Location data exported to Excel successfully.", "success");
		}
		catch (Exception ex)
		{
			await ShowToast("Error", $"An error occurred while exporting to Excel: {ex.Message}", "error");
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

			// Call the PDF export utility
			var stream = await Task.Run(() => LocationPDFExport.ExportLocation(_locations));

			// Generate file name
			string fileName = "LOCATION_MASTER.pdf";

			// Save and view the PDF file
			await SaveAndViewService.SaveAndView(fileName, stream);

			await ShowToast("Success", "Location data exported to PDF successfully.", "success");
		}
		catch (Exception ex)
		{
			await ShowToast("Error", $"An error occurred while exporting to PDF: {ex.Message}", "error");
		}
		finally
		{
			_isProcessing = false;
			StateHasChanged();
		}
	}
	#endregion

	#region Utilities
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