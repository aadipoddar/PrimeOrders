using PrimeBakes.Shared.Services;

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

public partial class AccountTypePage
{
	private bool _isLoading = true;
	private bool _isProcessing = false;
	private bool _showDeleted = false;

	private AccountTypeModel _accountType = new();

	private List<AccountTypeModel> _accountTypes = [];

	private SfGrid<AccountTypeModel> _sfGrid;
	private SfDialog _deleteConfirmationDialog;
	private SfDialog _recoverConfirmationDialog;

	private int _deleteAccountTypeId = 0;
	private string _deleteAccountTypeName = string.Empty;
	private bool _isDeleteDialogVisible = false;

	private int _recoverAccountTypeId = 0;
	private string _recoverAccountTypeName = string.Empty;
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

	private void ShowDeleteConfirmation(int id, string name)
	{
		_deleteAccountTypeId = id;
		_deleteAccountTypeName = name;
		_isDeleteDialogVisible = true;
		StateHasChanged();
	}

	private void CancelDelete()
	{
		_deleteAccountTypeId = 0;
		_deleteAccountTypeName = string.Empty;
		_isDeleteDialogVisible = false;
		StateHasChanged();
	}

	private async Task ConfirmDelete()
	{
		try
		{
			_isProcessing = true;
			_isDeleteDialogVisible = false;

			var accountType = _accountTypes.FirstOrDefault(at => at.Id == _deleteAccountTypeId);
			if (accountType == null)
			{
				await ShowToast("Error", "Account Type not found.", "error");
				return;
			}

			accountType.Status = false;
			await AccountTypeData.InsertAccountType(accountType);

			await ShowToast("Success", $"Account Type '{accountType.Name}' has been deleted successfully.", "success");
			NavigationManager.NavigateTo(PageRouteNames.AdminAccountType, true);
		}
		catch (Exception ex)
		{
			await ShowToast("Error", $"Failed to delete Account Type: {ex.Message}", "error");
		}
		finally
		{
			_isProcessing = false;
			_deleteAccountTypeId = 0;
			_deleteAccountTypeName = string.Empty;
		}
	}

	private void ShowRecoverConfirmation(int id, string name)
	{
		_recoverAccountTypeId = id;
		_recoverAccountTypeName = name;
		_isRecoverDialogVisible = true;
		StateHasChanged();
	}

	private void CancelRecover()
	{
		_recoverAccountTypeId = 0;
		_recoverAccountTypeName = string.Empty;
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

			var accountType = _accountTypes.FirstOrDefault(at => at.Id == _recoverAccountTypeId);
			if (accountType == null)
			{
				await ShowToast("Error", "Account Type not found.", "error");
				return;
			}

			accountType.Status = true;
			await AccountTypeData.InsertAccountType(accountType);

			await ShowToast("Success", $"Account Type '{accountType.Name}' has been recovered successfully.", "success");
			NavigationManager.NavigateTo(PageRouteNames.AdminAccountType, true);
		}
		catch (Exception ex)
		{
			await ShowToast("Error", $"Failed to recover Account Type: {ex.Message}", "error");
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
			await ShowToast("Error", "Account Type name is required. Please enter a valid account type name.", "error");
			return false;
		}

		if (string.IsNullOrWhiteSpace(_accountType.Remarks))
			_accountType.Remarks = null;

		if (_accountType.Id > 0)
		{
			var existingAccountType = _accountTypes.FirstOrDefault(_ => _.Id != _accountType.Id && _.Name.Equals(_accountType.Name, StringComparison.OrdinalIgnoreCase));
			if (existingAccountType is not null)
			{
				await ShowToast("Error", $"Account Type name '{_accountType.Name}' already exists. Please choose a different name.", "error");
				return false;
			}
		}
		else
		{
			var existingAccountType = _accountTypes.FirstOrDefault(_ => _.Name.Equals(_accountType.Name, StringComparison.OrdinalIgnoreCase));
			if (existingAccountType is not null)
			{
				await ShowToast("Error", $"Account Type name '{_accountType.Name}' already exists. Please choose a different name.", "error");
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

			if (!await ValidateForm())
			{
				_isProcessing = false;
				return;
			}

			await AccountTypeData.InsertAccountType(_accountType);

			await ShowToast("Success", $"Account Type '{_accountType.Name}' has been saved successfully.", "success");
			NavigationManager.NavigateTo(PageRouteNames.AdminAccountType, true);
		}
		catch (Exception ex)
		{
			await ShowToast("Error", $"Failed to save Account Type: {ex.Message}", "error");
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
			var stream = await Task.Run(() => AccountTypeExcelExport.ExportAccountType(_accountTypes));

			// Generate file name
			string fileName = "ACCOUNT_TYPE_MASTER.xlsx";

			// Save and view the Excel file
			await SaveAndViewService.SaveAndView(fileName, stream);

			await ShowToast("Success", "Account Type data exported to Excel successfully.", "success");
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
			var stream = await Task.Run(() => AccountTypePDFExport.ExportAccountType(_accountTypes));

			// Generate file name
			string fileName = "ACCOUNT_TYPE_MASTER.pdf";

			// Save and view the PDF file
			await SaveAndViewService.SaveAndView(fileName, stream);

			await ShowToast("Success", "Account Type data exported to PDF successfully.", "success");
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