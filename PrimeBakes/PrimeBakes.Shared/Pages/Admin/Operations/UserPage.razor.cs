using PrimeBakes.Shared.Components;

using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Exporting.Operations;
using PrimeBakesLibrary.Models.Common;

using Syncfusion.Blazor.Grids;

namespace PrimeBakes.Shared.Pages.Admin.Operations;

public partial class UserPage : IAsyncDisposable
{
	private HotKeysContext _hotKeysContext;
	private bool _isLoading = true;
	private bool _isProcessing = false;
	private bool _showDeleted = false;

	private UserModel _user = new();
	private LocationModel _location = new();

	private List<UserModel> _users = [];
	private List<LocationModel> _locations = [];

	private SfGrid<UserModel> _sfGrid;
	private DeleteConfirmationDialog _deleteConfirmationDialog;
	private RecoverConfirmationDialog _recoverConfirmationDialog;

	private int _deleteUserId = 0;
	private string _deleteUserName = string.Empty;

	private int _recoverUserId = 0;
	private string _recoverUserName = string.Empty;

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
			.Add(ModCode.Ctrl, Code.S, SaveUser, "Save", Exclude.None)
			.Add(ModCode.Ctrl, Code.N, () => NavigationManager.NavigateTo(PageRouteNames.AdminUser, true), "New", Exclude.None)
			.Add(ModCode.Ctrl, Code.E, ExportExcel, "Export Excel", Exclude.None)
			.Add(ModCode.Ctrl, Code.P, ExportPdf, "Export PDF", Exclude.None)
			.Add(ModCode.Ctrl, Code.D, () => NavigationManager.NavigateTo(PageRouteNames.Dashboard), "Dashboard", Exclude.None)
			.Add(ModCode.Ctrl, Code.B, () => NavigationManager.NavigateTo(PageRouteNames.AdminDashboard), "Back", Exclude.None)
			.Add(Code.Insert, EditSelectedItem, "Edit selected", Exclude.None)
			.Add(Code.Delete, DeleteSelectedItem, "Delete selected", Exclude.None);

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

	private async Task ShowDeleteConfirmation(int id, string name)
	{
		_deleteUserId = id;
		_deleteUserName = name;
		await _deleteConfirmationDialog.ShowAsync();
	}

	private async Task CancelDelete()
	{
		_deleteUserId = 0;
		_deleteUserName = string.Empty;
		await _deleteConfirmationDialog.HideAsync();
	}

	private async Task ConfirmDelete()
	{
		try
		{
			_isProcessing = true;
			await _deleteConfirmationDialog.HideAsync();

			var user = _users.FirstOrDefault(u => u.Id == _deleteUserId);
			if (user == null)
			{
				await _toastNotification.ShowAsync("Error", "User not found.", ToastType.Error);
				return;
			}

			user.Status = false;
			await UserData.InsertUser(user);

			await _toastNotification.ShowAsync("Deleted", $"User '{user.Name}' removed successfully.", ToastType.Success);
			NavigationManager.NavigateTo(PageRouteNames.AdminUser, true);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"Failed to delete user: {ex.Message}", ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
			_deleteUserId = 0;
			_deleteUserName = string.Empty;
		}
	}

	private async Task ShowRecoverConfirmation(int id, string name)
	{
		_recoverUserId = id;
		_recoverUserName = name;
		await _recoverConfirmationDialog.ShowAsync();
	}

	private async Task CancelRecover()
	{
		_recoverUserId = 0;
		_recoverUserName = string.Empty;
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

			var user = _users.FirstOrDefault(u => u.Id == _recoverUserId);
			if (user == null)
			{
				await _toastNotification.ShowAsync("Error", "User not found.", ToastType.Error);
				return;
			}

			user.Status = true;
			await UserData.InsertUser(user);

			await _toastNotification.ShowAsync("Recovered", $"User '{user.Name}' restored successfully.", ToastType.Success);
			NavigationManager.NavigateTo(PageRouteNames.AdminUser, true);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"Failed to recover user: {ex.Message}", ToastType.Error);
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
			await _toastNotification.ShowAsync("Validation", "User name is required.", ToastType.Warning);
			return false;
		}

		if (_user.Passcode.ToString().Length != 4)
		{
			await _toastNotification.ShowAsync("Validation", "Passcode must be a 4-digit number.", ToastType.Warning);
			return false;
		}

		if (_user.LocationId <= 0)
		{
			await _toastNotification.ShowAsync("Validation", "Please select a location.", ToastType.Warning);
			return false;
		}

		if (_user.Admin == false && _user.Sales == false && _user.Inventory == false && _user.Accounts == false && _user.Order == false)
		{
			await _toastNotification.ShowAsync("Validation", "At least one role must be assigned.", ToastType.Warning);
			return false;
		}

		if (_location is null || _location.Id <= 0)
		{
			await _toastNotification.ShowAsync("Validation", "Please select a valid location.", ToastType.Warning);
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
				await _toastNotification.ShowAsync("Validation", $"Passcode '{_user.Passcode}' already exists.", ToastType.Warning);
				return false;
			}

			existingUser = _users.FirstOrDefault(_ => _.Id != _user.Id && _.Name.Equals(_user.Name, StringComparison.OrdinalIgnoreCase));
			if (existingUser is not null)
			{
				await _toastNotification.ShowAsync("Validation", $"User name '{_user.Name}' already exists.", ToastType.Warning);
				return false;
			}
		}
		else
		{
			var existingUser = _users.FirstOrDefault(_ => _.Passcode == _user.Passcode);
			if (existingUser is not null)
			{
				await _toastNotification.ShowAsync("Validation", $"Passcode '{_user.Passcode}' already exists.", ToastType.Warning);
				return false;
			}

			existingUser = _users.FirstOrDefault(_ => _.Name.Equals(_user.Name, StringComparison.OrdinalIgnoreCase));
			if (existingUser is not null)
			{
				await _toastNotification.ShowAsync("Validation", $"User name '{_user.Name}' already exists.", ToastType.Warning);
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
			StateHasChanged();

			if (!await ValidateForm())
			{
				_isProcessing = false;
				return;
			}

			await _toastNotification.ShowAsync("Saving", "Processing user...", ToastType.Info);

			await UserData.InsertUser(_user);

			await _toastNotification.ShowAsync("Saved", $"User '{_user.Name}' saved successfully.", ToastType.Success);
			NavigationManager.NavigateTo(PageRouteNames.AdminUser, true);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"Failed to save user: {ex.Message}", ToastType.Error);
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
			await _toastNotification.ShowAsync("Exporting", "Generating Excel file...", ToastType.Info);

			// Call the Excel export utility
			var stream = await UserExcelExport.ExportUser(_users);

			// Generate file name
			string fileName = "USER_MASTER.xlsx";

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
			var stream = await UserPDFExport.ExportUser(_users);

			// Generate file name
			string fileName = "USER_MASTER.pdf";

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

	#region Utilities
	private void ResetPage()
	{
		NavigationManager.NavigateTo(PageRouteNames.AdminUser, true);
	}
	#endregion

	private async Task EditSelectedItem()
	{
		var selectedRecords = await _sfGrid.GetSelectedRecordsAsync();
		if (selectedRecords.Count > 0)
			OnEditUser(selectedRecords[0]);
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