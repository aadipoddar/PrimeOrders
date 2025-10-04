using PrimeBakes.Shared.Services;
using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Models.Common;
using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Notifications;

namespace PrimeBakes.Shared.Pages.Admin;

public partial class UserPage
{
	private bool _isLoading = true;
	private bool _isSubmitting = false;

	private UserModel _currentUser;
	private UserModel _userModel = new()
	{
		Id = 0,
		Name = "",
		Passcode = 0,
		LocationId = 1,
		Sales = false,
		Order = false,
		Inventory = false,
		Accounts = false,
		Admin = false,
		Status = true
	};

	private List<UserModel> _users = [];
	private List<LocationModel> _locations = [];

	private SfGrid<UserModel> _sfGrid;
	private SfToast _sfToast;
	private SfToast _sfErrorToast;

	// Toast message properties
	private string _successMessage = "User saved successfully!";
	private string _errorMessage = "An error occurred. Please try again.";

	protected override async Task OnInitializedAsync()
	{
		var authResult = await AuthService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, UserRoles.Admin, true);
		_currentUser = authResult.User;
		
		await LoadUsers();

		_isLoading = false;
		StateHasChanged();
	}

	private async Task LoadUsers()
	{
		try
		{
			_users = await CommonData.LoadTableData<UserModel>(TableNames.User);
			_locations = await CommonData.LoadTableData<LocationModel>(TableNames.Location);

			// If current user is not from primary location, filter users
			if (_currentUser.LocationId != 1)
			{
				_users = _users.Where(u => u.LocationId == _currentUser.LocationId).ToList();
				_userModel.LocationId = _currentUser.LocationId;
			}

			if (_sfGrid is not null)
				await _sfGrid.Refresh();

			StateHasChanged();
		}
		catch (Exception ex)
		{
			_errorMessage = $"Failed to load user data: {ex.Message}";
			await ShowErrorToast();
		}
	}

	private void OnAddUser()
	{
		_userModel = new()
		{
			Id = 0,
			Name = "",
			Passcode = 0,
			LocationId = _currentUser.LocationId,
			Sales = false,
			Order = false,
			Inventory = false,
			Accounts = false,
			Admin = false,
			Status = true
		};
		StateHasChanged();
	}

	private void OnEditUser(UserModel user)
	{
		_userModel = new()
		{
			Id = user.Id,
			Name = user.Name,
			Passcode = user.Passcode,
			LocationId = user.LocationId,
			Sales = user.Sales,
			Order = user.Order,
			Inventory = user.Inventory,
			Accounts = user.Accounts,
			Admin = user.Admin,
			Status = user.Status
		};
		StateHasChanged();
	}

	private async Task ToggleUserStatus(UserModel user)
	{
		try
		{
			user.Status = !user.Status;
			await UserData.InsertUser(user);
			await LoadUsers();

			_successMessage = $"User '{user.Name}' has been {(user.Status ? "activated" : "deactivated")} successfully.";
			await ShowSuccessToast();

			OnAddUser();
		}
		catch (Exception ex)
		{
			_errorMessage = $"Failed to update user status: {ex.Message}";
			await ShowErrorToast();
		}
	}

	// Handle Admin role selection - automatically grant all permissions
	private void OnAdminChanged()
	{
		if (_userModel.Admin)
		{
			_userModel.Sales = true;
			_userModel.Order = true;
			_userModel.Inventory = true;
			_userModel.Accounts = true;
		}
		StateHasChanged();
	}

	// Prevent removing admin privileges if other permissions are unchecked
	private void OnPermissionChanged()
	{
		// If admin is checked, keep all permissions checked
		if (_userModel.Admin)
		{
			_userModel.Sales = true;
			_userModel.Order = true;
			_userModel.Inventory = true;
			_userModel.Accounts = true;
		}
		// If not admin but all permissions are checked, don't auto-check admin
		StateHasChanged();
	}

	private async Task<bool> ValidateForm()
	{
		// Reset accounts permission if not primary location
		if (_currentUser.LocationId != 1)
		{
			_userModel.LocationId = _currentUser.LocationId;
			_userModel.Accounts = false;
		}

		if (_userModel.LocationId != 1)
			_userModel.Accounts = false;

		if (string.IsNullOrWhiteSpace(_userModel.Name))
		{
			_errorMessage = "User name is required. Please enter a valid name.";
			await ShowErrorToast();
			return false;
		}

		if (_userModel.Passcode <= 0)
		{
			_errorMessage = "Please enter a valid passcode (PIN).";
			await ShowErrorToast();
			return false;
		}

		if (_userModel.LocationId <= 0)
		{
			_errorMessage = "Please select a location.";
			await ShowErrorToast();
			return false;
		}

		if (_userModel.Passcode.ToString().Length != 4)
		{
			_errorMessage = "Passcode must be exactly 4 digits.";
			await ShowErrorToast();
			return false;
		}

		// Check for duplicate passcode
		if (_userModel.Id > 0)
		{
			var existingUser = _users.FirstOrDefault(u => u.Id != _userModel.Id && u.Passcode == _userModel.Passcode);
			if (existingUser is not null)
			{
				_errorMessage = $"Passcode {_userModel.Passcode} is already used by '{existingUser.Name}'. Please choose a different passcode.";
				await ShowErrorToast();
				return false;
			}
		}
		else
		{
			var existingUser = _users.FirstOrDefault(u => u.Passcode == _userModel.Passcode);
			if (existingUser is not null)
			{
				_errorMessage = $"Passcode {_userModel.Passcode} is already used by '{existingUser.Name}'. Please choose a different passcode.";
				await ShowErrorToast();
				return false;
			}
		}

		// Validate at least one permission is selected
		if (!_userModel.Sales && !_userModel.Order && !_userModel.Inventory && !_userModel.Accounts && !_userModel.Admin)
		{
			_errorMessage = "Please select at least one permission for the user.";
			await ShowErrorToast();
			return false;
		}

		return true;
	}

	private async Task SaveUser()
	{
		try
		{
			if (_isSubmitting || !await ValidateForm())
				return;

			_isSubmitting = true;
			StateHasChanged();

			var isNewUser = _userModel.Id == 0;
			var userName = _userModel.Name;

			await UserData.InsertUser(_userModel);
			await LoadUsers();

			// Reset form
			OnAddUser();

			_successMessage = isNewUser
				? $"User '{userName}' has been created successfully!"
				: $"User '{userName}' has been updated successfully!";
			await ShowSuccessToast();
		}
		catch (Exception ex)
		{
			_errorMessage = $"Failed to save user: {ex.Message}";
			await ShowErrorToast();
		}
		finally
		{
			_isSubmitting = false;
			StateHasChanged();
		}
	}

	public void RowSelectHandler(RowSelectEventArgs<UserModel> args) =>
		OnEditUser(args.Data);

	// Helper methods for showing toasts with dynamic content
	private async Task ShowSuccessToast()
	{
		if (_sfToast != null)
		{
			var toastModel = new ToastModel
			{
				Title = "Success",
				Content = _successMessage,
				CssClass = "e-toast-success",
				Icon = "e-success toast-icons"
			};
			await _sfToast.ShowAsync(toastModel);
		}
	}

	private async Task ShowErrorToast()
	{
		if (_sfErrorToast != null)
		{
			var toastModel = new ToastModel
			{
				Title = "Error",
				Content = _errorMessage,
				CssClass = "e-toast-danger",
				Icon = "e-error toast-icons"
			};
			await _sfErrorToast.ShowAsync(toastModel);
		}
	}

	// Helper method to get location name
	private string GetLocationName(int locationId)
	{
		return _locations.FirstOrDefault(l => l.Id == locationId)?.Name ?? "Unknown";
	}
}