using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Exporting.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Accounts.Masters;

using Syncfusion.Blazor.Grids;
using PrimeBakes.Shared.Components.Dialog;

namespace PrimeBakes.Shared.Pages.Admin.Accounts;

public partial class AccountTypePage : IAsyncDisposable
{
	private HotKeysContext _hotKeysContext;
	private bool _isLoading = true;
	private bool _isProcessing = false;
	private bool _showDeleted = false;

	private AccountTypeModel _accountType = new();

	private List<AccountTypeModel> _accountTypes = [];

	private SfGrid<AccountTypeModel> _sfGrid;
	private DeleteConfirmationDialog _deleteConfirmationDialog;
	private RecoverConfirmationDialog _recoverConfirmationDialog;

	private int _deleteAccountTypeId = 0;
	private string _deleteAccountTypeName = string.Empty;

	private int _recoverAccountTypeId = 0;
	private string _recoverAccountTypeName = string.Empty;

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
			.Add(ModCode.Ctrl, Code.S, SaveAccountType, "Save", Exclude.None)
			.Add(ModCode.Ctrl, Code.N, () => NavigationManager.NavigateTo(PageRouteNames.AdminAccountType, true), "New", Exclude.None)
			.Add(ModCode.Ctrl, Code.E, ExportExcel, "Export Excel", Exclude.None)
			.Add(ModCode.Ctrl, Code.P, ExportPdf, "Export PDF", Exclude.None)
			.Add(ModCode.Ctrl, Code.D, () => NavigationManager.NavigateTo(PageRouteNames.Dashboard), "Dashboard", Exclude.None)
			.Add(ModCode.Ctrl, Code.B, () => NavigationManager.NavigateTo(PageRouteNames.AdminDashboard), "Back", Exclude.None)
			.Add(Code.Insert, EditSelectedItem, "Edit selected", Exclude.None)
			.Add(Code.Delete, DeleteSelectedItem, "Delete selected", Exclude.None);

		_accountTypes = await CommonData.LoadTableData<AccountTypeModel>(TableNames.AccountType);

		if (!_showDeleted)
			_accountTypes = [.. _accountTypes.Where(at => at.Status)];

		if (_sfGrid is not null)
			await _sfGrid.Refresh();
	}
	#endregion

	#region Actions
	private void OnEditAccountType(AccountTypeModel accountType)
	{
		_accountType = new()
		{
			Id = accountType.Id,
			Name = accountType.Name,
			Remarks = accountType.Remarks,
			Status = accountType.Status
		};

		StateHasChanged();
	}

	private async Task ShowDeleteConfirmation(int id, string name)
	{
		_deleteAccountTypeId = id;
		_deleteAccountTypeName = name;
		await _deleteConfirmationDialog.ShowAsync();
	}

	private async Task CancelDelete()
	{
		_deleteAccountTypeId = 0;
		_deleteAccountTypeName = string.Empty;
		await _deleteConfirmationDialog.HideAsync();
	}

	private async Task ConfirmDelete()
	{
		try
		{
			_isProcessing = true;
			await _deleteConfirmationDialog.HideAsync();

			var accountType = _accountTypes.FirstOrDefault(at => at.Id == _deleteAccountTypeId);
			if (accountType == null)
			{
				await _toastNotification.ShowAsync("Error", "Account Type not found.", ToastType.Error);
				return;
			}

			accountType.Status = false;
			await AccountTypeData.InsertAccountType(accountType);

			await _toastNotification.ShowAsync("Success", $"Account Type '{accountType.Name}' has been deleted successfully.", ToastType.Success);
			NavigationManager.NavigateTo(PageRouteNames.AdminAccountType, true);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"Failed to delete Account Type: {ex.Message}", ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
			_deleteAccountTypeId = 0;
			_deleteAccountTypeName = string.Empty;
		}
	}

	private async Task ShowRecoverConfirmation(int id, string name)
	{
		_recoverAccountTypeId = id;
		_recoverAccountTypeName = name;
		await _recoverConfirmationDialog.ShowAsync();
	}

	private async Task CancelRecover()
	{
		_recoverAccountTypeId = 0;
		_recoverAccountTypeName = string.Empty;
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

			var accountType = _accountTypes.FirstOrDefault(at => at.Id == _recoverAccountTypeId);
			if (accountType == null)
			{
				await _toastNotification.ShowAsync("Error", "Account Type not found.", ToastType.Error);
				return;
			}

			accountType.Status = true;
			await AccountTypeData.InsertAccountType(accountType);

			await _toastNotification.ShowAsync("Success", $"Account Type '{accountType.Name}' has been recovered successfully.", ToastType.Success);
			NavigationManager.NavigateTo(PageRouteNames.AdminAccountType, true);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"Failed to recover Account Type: {ex.Message}", ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
			_recoverAccountTypeId = 0;
			_recoverAccountTypeName = string.Empty;
		}
	}
	#endregion

	#region Saving
	private async Task<bool> ValidateForm()
	{
		_accountType.Name = _accountType.Name?.Trim() ?? "";
		_accountType.Name = _accountType.Name?.ToUpper() ?? "";

		_accountType.Remarks = _accountType.Remarks?.Trim() ?? "";
		_accountType.Status = true;

		if (string.IsNullOrWhiteSpace(_accountType.Name))
		{
			await _toastNotification.ShowAsync("Error", "Account Type name is required. Please enter a valid account type name.", ToastType.Error);
			return false;
		}

		if (string.IsNullOrWhiteSpace(_accountType.Remarks))
			_accountType.Remarks = null;

		if (_accountType.Id > 0)
		{
			var existingAccountType = _accountTypes.FirstOrDefault(_ => _.Id != _accountType.Id && _.Name.Equals(_accountType.Name, StringComparison.OrdinalIgnoreCase));
			if (existingAccountType is not null)
			{
				await _toastNotification.ShowAsync("Error", $"Account Type name '{_accountType.Name}' already exists. Please choose a different name.", ToastType.Error);
				return false;
			}
		}
		else
		{
			var existingAccountType = _accountTypes.FirstOrDefault(_ => _.Name.Equals(_accountType.Name, StringComparison.OrdinalIgnoreCase));
			if (existingAccountType is not null)
			{
				await _toastNotification.ShowAsync("Error", $"Account Type name '{_accountType.Name}' already exists. Please choose a different name.", ToastType.Error);
				return false;
			}
		}

		return true;
	}

	private async Task SaveAccountType()
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

			await AccountTypeData.InsertAccountType(_accountType);

			await _toastNotification.ShowAsync("Success", $"Account Type '{_accountType.Name}' has been saved successfully.", ToastType.Success);
			NavigationManager.NavigateTo(PageRouteNames.AdminAccountType, true);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"Failed to save Account Type: {ex.Message}", ToastType.Error);
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
			var stream = await AccountTypeExcelExport.ExportAccountType(_accountTypes);

			// Generate file name
			string fileName = "ACCOUNT_TYPE_MASTER.xlsx";

			// Save and view the Excel file
			await SaveAndViewService.SaveAndView(fileName, stream);

			await _toastNotification.ShowAsync("Success", "Account Type data exported to Excel successfully.", ToastType.Success);
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
			var stream = await AccountTypePDFExport.ExportAccountType(_accountTypes);

			// Generate file name
			string fileName = "ACCOUNT_TYPE_MASTER.pdf";

			// Save and view the PDF file
			await SaveAndViewService.SaveAndView(fileName, stream);

			await _toastNotification.ShowAsync("Success", "Account Type data exported to PDF successfully.", ToastType.Success);
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
			OnEditAccountType(selectedRecords[0]);
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