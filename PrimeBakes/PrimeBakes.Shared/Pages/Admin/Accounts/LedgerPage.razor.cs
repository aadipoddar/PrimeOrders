using PrimeBakes.Shared.Services;

using PrimeBakesLibrary.Data;
using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Exporting.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Accounts.Masters;

using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Notifications;
using Syncfusion.Blazor.Popups;

namespace PrimeBakes.Shared.Pages.Admin.Accounts;

public partial class LedgerPage
{
	private bool _isLoading = true;
	private bool _isProcessing = false;
	private bool _showDeleted = false;

	private LedgerModel _ledger = new();

	private List<LedgerModel> _ledgers = [];
	private List<GroupModel> _groups = [];
	private List<AccountTypeModel> _accountTypes = [];
	private List<StateUTModel> _stateUTs = [];
	private List<LocationModel> _locations = [];

	private SfGrid<LedgerModel> _sfGrid;
	private SfDialog _deleteConfirmationDialog;
	private SfDialog _recoverConfirmationDialog;

	private int _deleteLedgerId = 0;
	private string _deleteLedgerName = string.Empty;
	private bool _isDeleteDialogVisible = false;

	private int _recoverLedgerId = 0;
	private string _recoverLedgerName = string.Empty;
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

		await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, UserRoles.Admin, true);
		await LoadData();
		_isLoading = false;
		StateHasChanged();
	}

	private async Task LoadData()
	{
		_ledgers = await CommonData.LoadTableData<LedgerModel>(TableNames.Ledger);
		_groups = await CommonData.LoadTableData<GroupModel>(TableNames.Group);
		_accountTypes = await CommonData.LoadTableData<AccountTypeModel>(TableNames.AccountType);
		_stateUTs = await CommonData.LoadTableData<StateUTModel>(TableNames.StateUT);
		_locations = await CommonData.LoadTableData<LocationModel>(TableNames.Location);

		if (!_showDeleted)
			_ledgers = [.. _ledgers.Where(l => l.Status)];

		if (_sfGrid is not null)
			await _sfGrid.Refresh();
	}
	#endregion

	#region Actions
	private void OnEditLedger(LedgerModel ledger)
	{
		_ledger = new()
		{
			Id = ledger.Id,
			Name = ledger.Name,
			Code = ledger.Code,
			GroupId = ledger.GroupId,
			AccountTypeId = ledger.AccountTypeId,
			StateUTId = ledger.StateUTId,
			LocationId = ledger.LocationId,
			GSTNo = ledger.GSTNo,
			PANNo = ledger.PANNo,
			CINNo = ledger.CINNo,
			Alias = ledger.Alias,
			Phone = ledger.Phone,
			Email = ledger.Email,
			Address = ledger.Address,
			Remarks = ledger.Remarks,
			Status = ledger.Status
		};

		StateHasChanged();
	}

	private void ShowDeleteConfirmation(int id, string name)
	{
		_deleteLedgerId = id;
		_deleteLedgerName = name;
		_isDeleteDialogVisible = true;
		StateHasChanged();
	}

	private void CancelDelete()
	{
		_deleteLedgerId = 0;
		_deleteLedgerName = string.Empty;
		_isDeleteDialogVisible = false;
		StateHasChanged();
	}

	private async Task ConfirmDelete()
	{
		try
		{
			_isProcessing = true;
			_isDeleteDialogVisible = false;

			var ledger = _ledgers.FirstOrDefault(l => l.Id == _deleteLedgerId);
			if (ledger == null)
			{
				await ShowToast("Error", "Ledger not found.", "error");
				return;
			}

			ledger.Status = false;
			await LedgerData.InsertLedger(ledger);

			await ShowToast("Success", $"Ledger '{ledger.Name}' has been deleted successfully.", "success");
			NavigationManager.NavigateTo(PageRouteNames.AdminLedger, true);
		}
		catch (Exception ex)
		{
			await ShowToast("Error", $"Failed to delete Ledger: {ex.Message}", "error");
		}
		finally
		{
			_isProcessing = false;
			_deleteLedgerId = 0;
			_deleteLedgerName = string.Empty;
		}
	}

	private void ShowRecoverConfirmation(int id, string name)
	{
		_recoverLedgerId = id;
		_recoverLedgerName = name;
		_isRecoverDialogVisible = true;
		StateHasChanged();
	}

	private void CancelRecover()
	{
		_recoverLedgerId = 0;
		_recoverLedgerName = string.Empty;
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

			var ledger = _ledgers.FirstOrDefault(l => l.Id == _recoverLedgerId);
			if (ledger == null)
			{
				await ShowToast("Error", "Ledger not found.", "error");
				return;
			}

			ledger.Status = true;
			await LedgerData.InsertLedger(ledger);

			await ShowToast("Success", $"Ledger '{ledger.Name}' has been recovered successfully.", "success");
			NavigationManager.NavigateTo(PageRouteNames.AdminLedger, true);
		}
		catch (Exception ex)
		{
			await ShowToast("Error", $"Failed to recover Ledger: {ex.Message}", "error");
		}
		finally
		{
			_isProcessing = false;
			_recoverLedgerId = 0;
			_recoverLedgerName = string.Empty;
		}
	}
	#endregion

	#region Saving
	private async Task<bool> ValidateForm()
	{
		_ledger.Name = _ledger.Name?.Trim() ?? "";
		_ledger.Name = _ledger.Name?.ToUpper() ?? "";

		_ledger.GSTNo = _ledger.GSTNo?.Trim() ?? "";
		_ledger.GSTNo = _ledger.GSTNo?.ToUpper() ?? "";

		_ledger.PANNo = _ledger.PANNo?.Trim() ?? "";
		_ledger.PANNo = _ledger.PANNo?.ToUpper() ?? "";

		_ledger.CINNo = _ledger.CINNo?.Trim() ?? "";
		_ledger.CINNo = _ledger.CINNo?.ToUpper() ?? "";

		_ledger.Alias = _ledger.Alias?.Trim() ?? "";
		_ledger.Alias = _ledger.Alias?.ToUpper() ?? "";

		_ledger.Phone = _ledger.Phone?.Trim() ?? "";
		_ledger.Email = _ledger.Email?.Trim() ?? "";
		_ledger.Address = _ledger.Address?.Trim() ?? "";

		_ledger.Remarks = _ledger.Remarks?.Trim() ?? "";
		_ledger.Status = true;

		if (string.IsNullOrWhiteSpace(_ledger.Name))
		{
			await ShowToast("Error", "Ledger name is required. Please enter a valid ledger name.", "error");
			return false;
		}

		if (_ledger.GroupId <= 0)
		{
			await ShowToast("Error", "Group is required. Please select a valid group.", "error");
			return false;
		}

		if (_ledger.AccountTypeId <= 0)
		{
			await ShowToast("Error", "Account Type is required. Please select a valid account type.", "error");
			return false;
		}

		if (_ledger.StateUTId <= 0)
		{
			await ShowToast("Error", "State/UT is required. Please select a valid State/UT.", "error");
			return false;
		}

		if (string.IsNullOrWhiteSpace(_ledger.GSTNo)) _ledger.GSTNo = null;
		if (string.IsNullOrWhiteSpace(_ledger.PANNo)) _ledger.PANNo = null;
		if (string.IsNullOrWhiteSpace(_ledger.CINNo)) _ledger.CINNo = null;
		if (string.IsNullOrWhiteSpace(_ledger.Alias)) _ledger.Alias = null;
		if (string.IsNullOrWhiteSpace(_ledger.Phone)) _ledger.Phone = null;
		if (string.IsNullOrWhiteSpace(_ledger.Email)) _ledger.Email = null;
		if (string.IsNullOrWhiteSpace(_ledger.Address)) _ledger.Address = null;
		if (string.IsNullOrWhiteSpace(_ledger.Remarks)) _ledger.Remarks = null;

		// Validate location uniqueness if location is selected
		if (_ledger.LocationId.HasValue && _ledger.LocationId.Value > 0)
		{
			if (_ledger.Id > 0)
			{
				// Editing existing ledger - check if another ledger has this location
				var existingLedgerWithLocation = _ledgers.FirstOrDefault(_ => _.Id != _ledger.Id && _.LocationId == _ledger.LocationId);
				if (existingLedgerWithLocation is not null)
				{
					var locationName = _locations.FirstOrDefault(l => l.Id == _ledger.LocationId)?.Name ?? "Unknown";
					await ShowToast("Error", $"Location '{locationName}' is already tagged to another ledger '{existingLedgerWithLocation.Name}'. Each location can only be tagged to one ledger.", "error");
					return false;
				}
			}
			else
			{
				// Creating new ledger - check if any ledger has this location
				var existingLedgerWithLocation = _ledgers.FirstOrDefault(_ => _.LocationId == _ledger.LocationId);
				if (existingLedgerWithLocation is not null)
				{
					var locationName = _locations.FirstOrDefault(l => l.Id == _ledger.LocationId)?.Name ?? "Unknown";
					await ShowToast("Error", $"Location '{locationName}' is already tagged to ledger '{existingLedgerWithLocation.Name}'. Each location can only be tagged to one ledger.", "error");
					return false;
				}
			}
		}

		if (_ledger.Id > 0)
		{
			var existingLedger = _ledgers.FirstOrDefault(_ => _.Id != _ledger.Id && _.Name.Equals(_ledger.Name, StringComparison.OrdinalIgnoreCase));
			if (existingLedger is not null)
			{
				await ShowToast("Error", $"Ledger name '{_ledger.Name}' already exists. Please choose a different name.", "error");
				return false;
			}
		}
		else
		{
			var existingLedger = _ledgers.FirstOrDefault(_ => _.Name.Equals(_ledger.Name, StringComparison.OrdinalIgnoreCase));
			if (existingLedger is not null)
			{
				await ShowToast("Error", $"Ledger name '{_ledger.Name}' already exists. Please choose a different name.", "error");
				return false;
			}
		}

		return true;
	}

	private async Task SaveLedger()
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

			if (_ledger.Id == 0)
				_ledger.Code = await GenerateCodes.GenerateLedgerCode();

			await LedgerData.InsertLedger(_ledger);

			await ShowToast("Success", $"Ledger '{_ledger.Name}' has been saved successfully.", "success");
			NavigationManager.NavigateTo(PageRouteNames.AdminLedger, true);
		}
		catch (Exception ex)
		{
			await ShowToast("Error", $"Failed to save Ledger: {ex.Message}", "error");
		}
		finally
		{
			_isProcessing = false;
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
			var stream = await LedgerExcelExport.ExportLedger(_ledgers);

			// Generate file name
			string fileName = "LEDGER_MASTER.xlsx";

			// Save and view the Excel file
			await SaveAndViewService.SaveAndView(fileName, stream);

			await ShowToast("Success", "Ledger data exported to Excel successfully.", "success");
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
			var stream = await LedgerPDFExport.ExportLedger(_ledgers);

			// Generate file name
			string fileName = "LEDGER_MASTER.pdf";

			// Save and view the PDF file
			await SaveAndViewService.SaveAndView(fileName, stream);

			await ShowToast("Success", "Ledger data exported to PDF successfully.", "success");
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