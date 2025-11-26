using PrimeBakes.Shared.Services;

using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Exporting.Operations;
using PrimeBakesLibrary.Models.Common;

using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Notifications;
using Syncfusion.Blazor.Popups;

namespace PrimeBakes.Shared.Pages.Admin.Operations;

public partial class UserPage
{
	private bool _isLoading = true;
	private bool _isProcessing = false;
	private bool _showDeleted = false;

	private UserModel _user = new();
	private LocationModel _location = new();

	private List<UserModel> _users = [];
	private List<LocationModel> _locations = [];

	private SfGrid<UserModel> _sfGrid;
	private SfDialog _deleteConfirmationDialog;
	private SfDialog _recoverConfirmationDialog;

	private int _deleteUserId = 0;
	private string _deleteUserName = string.Empty;
	private bool _isDeleteDialogVisible = false;

	private int _recoverUserId = 0;
	private string _recoverUserName = string.Empty;
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
		_users = await CommonData.LoadTableData<UserModel>(TableNames.User);

		if (!_showDeleted)
			_users = [.. _users.Where(u => u.Status)];

		if (_sfGrid is not null)
			await _sfGrid.Refresh();
	}
	#endregion

	#region Actions
	private void OnEditUser(UserModel user)
	{
		_user = new()
		{
			Id = user.Id,
			Name = user.Name,
			LocationId = user.LocationId,
			Passcode = user.Passcode,
			Order = user.Order,
			Sales = user.Sales,
			Inventory = user.Inventory,
			Accounts = user.Accounts,
			Admin = user.Admin,
			Status = user.Status
		};

		_location = _locations.FirstOrDefault(l => l.Id == user.LocationId) ?? new LocationModel();

		StateHasChanged();
	}

	private void ShowDeleteConfirmation(int id, string name)
	{
		_deleteUserId = id;
		_deleteUserName = name;
		_isDeleteDialogVisible = true;
		StateHasChanged();
	}

	private void CancelDelete()
	{
		_deleteUserId = 0;
		_deleteUserName = string.Empty;
		_isDeleteDialogVisible = false;
		StateHasChanged();
	}

	private async Task ConfirmDelete()
	{
		try
		{
			_isProcessing = true;
			_isDeleteDialogVisible = false;

			var user = _users.FirstOrDefault(u => u.Id == _deleteUserId);
			if (user == null)
			{
				await ShowToast("Error", "User not found.", "error");
				return;
			}

			user.Status = false;
			await UserData.InsertUser(user);

			await ShowToast("Success", $"User '{user.Name}' has been deleted successfully.", "success");
			NavigationManager.NavigateTo(PageRouteNames.AdminUser, true);
		}
		catch (Exception ex)
		{
			await ShowToast("Error", $"Failed to delete user: {ex.Message}", "error");
		}
		finally
		{
			_isProcessing = false;
			_deleteUserId = 0;
			_deleteUserName = string.Empty;
		}
	}

	private void ShowRecoverConfirmation(int id, string name)
	{
		_recoverUserId = id;
		_recoverUserName = name;
		_isRecoverDialogVisible = true;
		StateHasChanged();
	}

	private void CancelRecover()
	{
		_recoverUserId = 0;
		_recoverUserName = string.Empty;
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

			var user = _users.FirstOrDefault(u => u.Id == _recoverUserId);
			if (user == null)
			{
				await ShowToast("Error", "User not found.", "error");
				return;
			}

			user.Status = true;
			await UserData.InsertUser(user);

			await ShowToast("Success", $"User '{user.Name}' has been recovered successfully.", "success");
			NavigationManager.NavigateTo(PageRouteNames.AdminUser, true);
		}
		catch (Exception ex)
		{
			await ShowToast("Error", $"Failed to recover user: {ex.Message}", "error");
		}
		finally
		{
			_isProcessing = false;
			_recoverUserId = 0;
			_recoverUserName = string.Empty;
		}
	}
	#endregion

	#region Saving
	private async Task<bool> ValidateForm()
	{
		_user.Name = _user.Name?.Trim() ?? "";
		_user.Remarks = _user.Remarks?.Trim() ?? "";

		_user.Name = _user.Name?.ToUpper() ?? "";
		_user.Status = true;

		if (string.IsNullOrWhiteSpace(_user.Name))
		{
			await ShowToast("Error", "User name is required. Please enter a valid user name.", "error");
			return false;
		}

		if (_user.Passcode.ToString().Length != 4)
		{
			await ShowToast("Error", "Passcode must be a 4-digit number. Please enter a valid passcode.", "error");
			return false;
		}

		if (_user.LocationId <= 0)
		{
			await ShowToast("Error", "Please select a valid location for the user.", "error");
			return false;
		}

		if (_user.Admin == false && _user.Sales == false && _user.Inventory == false && _user.Accounts == false && _user.Order == false)
		{
			await ShowToast("Error", "At least one role (Sales, Inventory, Accounts, Order, or Admin) must be assigned to the user.", "error");
			return false;
		}

		if (_location is null || _location.Id <= 0)
		{
			await ShowToast("Error", "Please select a valid location for the user.", "error");
			return false;
		}
		_user.LocationId = _location.Id;

		if (_user.Admin)
		{
			_user.Order = true;
			_user.Sales = true;
			_user.Inventory = true;
			_user.Accounts = true;
		}

		if (string.IsNullOrWhiteSpace(_user.Remarks))
			_user.Remarks = null;

		if (_user.Id > 0)
		{
			var existingUser = _users.FirstOrDefault(_ => _.Id != _user.Id && _.Passcode == _user.Passcode);
			if (existingUser is not null)
			{
				await ShowToast("Error", $"Passcode '{_user.Passcode}' is already used by user '{existingUser.Name}'. Please choose a different passcode.", "error");
				return false;
			}

			existingUser = _users.FirstOrDefault(_ => _.Id != _user.Id && _.Name.Equals(_user.Name, StringComparison.OrdinalIgnoreCase));
			if (existingUser is not null)
			{
				await ShowToast("Error", $"User name '{_user.Name}' already exists. Please choose a different name.", "error");
				return false;
			}
		}
		else
		{
			var existingUser = _users.FirstOrDefault(_ => _.Passcode == _user.Passcode);
			if (existingUser is not null)
			{
				await ShowToast("Error", $"Passcode '{_user.Passcode}' is already used by user '{existingUser.Name}'. Please choose a different passcode.", "error");
				return false;
			}

			existingUser = _users.FirstOrDefault(_ => _.Name.Equals(_user.Name, StringComparison.OrdinalIgnoreCase));
			if (existingUser is not null)
			{
				await ShowToast("Error", $"User name '{_user.Name}' already exists. Please choose a different name.", "error");
				return false;
			}
		}

		return true;
	}

	private async Task SaveUser()
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

			await UserData.InsertUser(_user);

			await ShowToast("Success", $"User '{_user.Name}' has been saved successfully.", "success");
			NavigationManager.NavigateTo(PageRouteNames.AdminUser, true);
		}
		catch (Exception ex)
		{
			await ShowToast("Error", $"Failed to save user: {ex.Message}", "error");
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
			var stream = await Task.Run(() => UserExcelExport.ExportUser(_users));

			// Generate file name
			string fileName = "USER_MASTER.xlsx";

			// Save and view the Excel file
			await SaveAndViewService.SaveAndView(fileName, stream);

			await ShowToast("Success", "User data exported to Excel successfully.", "success");
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
			var stream = await Task.Run(() => UserPDFExport.ExportUser(_users));

			// Generate file name
			string fileName = "USER_MASTER.pdf";

			// Save and view the PDF file
			await SaveAndViewService.SaveAndView(fileName, stream);

			await ShowToast("Success", "User data exported to PDF successfully.", "success");
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
	private void ResetPage()
	{
		NavigationManager.NavigateTo(PageRouteNames.AdminUser, true);
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