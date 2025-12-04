using PrimeBakes.Shared.Components;

using PrimeBakesLibrary.Data;
using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Exporting.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Accounts.Masters;

using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Popups;

namespace PrimeBakes.Shared.Pages.Admin.Accounts;

public partial class LedgerPage : IAsyncDisposable
{
	private HotKeysContext _hotKeysContext;
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
			.Add(ModCode.Ctrl, Code.S, SaveLedger, "Save", Exclude.None)
			.Add(ModCode.Ctrl, Code.N, () => NavigationManager.NavigateTo(PageRouteNames.AdminLedger, true), "New", Exclude.None)
			.Add(ModCode.Ctrl, Code.E, ExportExcel, "Export Excel", Exclude.None)
			.Add(ModCode.Ctrl, Code.P, ExportPdf, "Export PDF", Exclude.None)
			.Add(ModCode.Ctrl, Code.D, () => NavigationManager.NavigateTo(PageRouteNames.Dashboard), "Dashboard", Exclude.None)
			.Add(ModCode.Ctrl, Code.B, () => NavigationManager.NavigateTo(PageRouteNames.AdminDashboard), "Back", Exclude.None)
			.Add(Code.Insert, EditSelectedItem, "Edit selected", Exclude.None)
			.Add(Code.Delete, DeleteSelectedItem, "Delete selected", Exclude.None);

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
				await _toastNotification.ShowAsync("Error", "Ledger not found.", ToastType.Error);
				return;
			}

			ledger.Status = false;
			await LedgerData.InsertLedger(ledger);

			await _toastNotification.ShowAsync("Success", $"Ledger '{ledger.Name}' has been deleted successfully.", ToastType.Success);
			NavigationManager.NavigateTo(PageRouteNames.AdminLedger, true);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"Failed to delete Ledger: {ex.Message}", ToastType.Error);
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
				await _toastNotification.ShowAsync("Error", "Ledger not found.", ToastType.Error);
				return;
			}

			ledger.Status = true;
			await LedgerData.InsertLedger(ledger);

			await _toastNotification.ShowAsync("Success", $"Ledger '{ledger.Name}' has been recovered successfully.", ToastType.Success);
			NavigationManager.NavigateTo(PageRouteNames.AdminLedger, true);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"Failed to recover Ledger: {ex.Message}", ToastType.Error);
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
			await _toastNotification.ShowAsync("Error", "Ledger name is required. Please enter a valid ledger name.", ToastType.Error);
			return false;
		}

		if (_ledger.GroupId <= 0)
		{
			await _toastNotification.ShowAsync("Error", "Group is required. Please select a valid group.", ToastType.Error);
			return false;
		}

		if (_ledger.AccountTypeId <= 0)
		{
			await _toastNotification.ShowAsync("Error", "Account Type is required. Please select a valid account type.", ToastType.Error);
			return false;
		}

		if (_ledger.StateUTId <= 0)
		{
			await _toastNotification.ShowAsync("Error", "State/UT is required. Please select a valid State/UT.", ToastType.Error);
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
					await _toastNotification.ShowAsync("Error", $"Location '{locationName}' is already tagged to another ledger '{existingLedgerWithLocation.Name}'. Each location can only be tagged to one ledger.", ToastType.Error);
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
					await _toastNotification.ShowAsync("Error", $"Location '{locationName}' is already tagged to ledger '{existingLedgerWithLocation.Name}'. Each location can only be tagged to one ledger.", ToastType.Error);
					return false;
				}
			}
		}

		if (_ledger.Id > 0)
		{
			var existingLedger = _ledgers.FirstOrDefault(_ => _.Id != _ledger.Id && _.Name.Equals(_ledger.Name, StringComparison.OrdinalIgnoreCase));
			if (existingLedger is not null)
			{
				await _toastNotification.ShowAsync("Error", $"Ledger name '{_ledger.Name}' already exists. Please choose a different name.", ToastType.Error);
				return false;
			}
		}
		else
		{
			var existingLedger = _ledgers.FirstOrDefault(_ => _.Name.Equals(_ledger.Name, StringComparison.OrdinalIgnoreCase));
			if (existingLedger is not null)
			{
				await _toastNotification.ShowAsync("Error", $"Ledger name '{_ledger.Name}' already exists. Please choose a different name.", ToastType.Error);
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
			StateHasChanged();

			if (!await ValidateForm())
			{
				_isProcessing = false;
				return;
			}

			await _toastNotification.ShowAsync("Processing Transaction", "Please wait while the transaction is being saved...", ToastType.Info);

			if (_ledger.Id == 0)
				_ledger.Code = await GenerateCodes.GenerateLedgerCode();

			await LedgerData.InsertLedger(_ledger);

			await _toastNotification.ShowAsync("Success", $"Ledger '{_ledger.Name}' has been saved successfully.", ToastType.Success);
			NavigationManager.NavigateTo(PageRouteNames.AdminLedger, true);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"Failed to save Ledger: {ex.Message}", ToastType.Error);
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
			await _toastNotification.ShowAsync("Processing", "Exporting to Excel...", ToastType.Info);

			// Call the Excel export utility
			var stream = await LedgerExcelExport.ExportLedger(_ledgers);

			// Generate file name
			string fileName = "LEDGER_MASTER.xlsx";

			// Save and view the Excel file
			await SaveAndViewService.SaveAndView(fileName, stream);

			await _toastNotification.ShowAsync("Success", "Ledger data exported to Excel successfully.", ToastType.Success);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"An error occurred while exporting to Excel: {ex.Message}", ToastType.Error);
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
			await _toastNotification.ShowAsync("Processing", "Exporting to PDF...", ToastType.Info);

			// Call the PDF export utility
			var stream = await LedgerPDFExport.ExportLedger(_ledgers);

			// Generate file name
			string fileName = "LEDGER_MASTER.pdf";

			// Save and view the PDF file
			await SaveAndViewService.SaveAndView(fileName, stream);

			await _toastNotification.ShowAsync("Success", "Ledger data exported to PDF successfully.", ToastType.Success);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"An error occurred while exporting to PDF: {ex.Message}", ToastType.Error);
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
			OnEditLedger(selectedRecords[0]);
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