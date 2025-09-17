using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Notifications;

namespace PrimeOrders.Components.Pages.Admin;

public partial class UserPage
{
	[Inject] public NavigationManager NavManager { get; set; }
	[Inject] public IJSRuntime JS { get; set; }

	private UserModel _currentUser;
	private bool _isLoading = true;

	private UserModel _userModel = new()
	{
		Name = "",
		Passcode = 0,
		LocationId = 1,
		Sales = false,
		Order = false,
		Inventory = false,
		Admin = false,
		Status = true
	};

	private List<UserModel> _users = [];
	private List<LocationModel> _locations = [];

	private SfGrid<UserModel> _sfGrid;
	private SfToast _sfToast;
	private SfToast _sfUpdateToast;
	private SfToast _sfErrorToast;

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		_isLoading = true;

		if (!((_currentUser = (await AuthService.ValidateUser(JS, NavManager, UserRoles.Admin)).User) is not null))
			return;

		await LoadData();

		_isLoading = false;
		StateHasChanged();
	}

	private async Task LoadData()
	{
		_users = await CommonData.LoadTableData<UserModel>(TableNames.User);
		_locations = await CommonData.LoadTableData<LocationModel>(TableNames.Location);

		if (_currentUser.LocationId != 1)
			_users = [.. _users.Where(u => u.LocationId == _currentUser.LocationId)];

		_userModel.LocationId = _currentUser.LocationId;

		if (_sfGrid is not null)
			await _sfGrid.Refresh();

		StateHasChanged();
	}

	public async Task RowSelectHandler(RowSelectEventArgs<UserModel> args)
	{
		_userModel = args.Data;
		await _sfUpdateToast.ShowAsync();
		StateHasChanged();
	}

	private async Task<bool> ValidateForm()
	{
		if (_currentUser.LocationId != 1)
		{
			_userModel.LocationId = _currentUser.LocationId;
			_userModel.Accounts = false;
		}

		if (_userModel.LocationId != 1)
			_userModel.Accounts = false;

		if (string.IsNullOrWhiteSpace(_userModel.Name))
		{
			_sfErrorToast.Content = "Name is required.";
			await _sfErrorToast.ShowAsync();
			return false;
		}

		if (_userModel.Passcode <= 0)
		{
			_sfErrorToast.Content = "Please enter a valid passcode (PIN).";
			await _sfErrorToast.ShowAsync();
			return false;
		}

		if (_userModel.LocationId <= 0)
		{
			_sfErrorToast.Content = "Please select a location.";
			await _sfErrorToast.ShowAsync();
			return false;
		}

		if (_userModel.Passcode.ToString().Length != 4)
		{
			_sfErrorToast.Content = "Passcode must be a 4-digit number.";
			await _sfErrorToast.ShowAsync();
			return false;
		}

		if (_userModel.Id > 0)
		{
			var users = await CommonData.LoadTableData<UserModel>(TableNames.User);

			if (users.Any(u => u.Id != _userModel.Id && u.Passcode == _userModel.Passcode))
			{
				_sfErrorToast.Content = "Passcode Already Present. Try a Different one";
				await _sfErrorToast.ShowAsync();
				return false;
			}
		}

		else if (_userModel.Id <= 0)
			if (await UserData.LoadUserByPasscode(_userModel.Passcode) is not null)
			{
				_sfErrorToast.Content = "Passcode Already Present. Try a Different one";
				await _sfErrorToast.ShowAsync();
				return false;
			}

		return true;
	}

	private async Task OnSaveClick()
	{
		if (!await ValidateForm())
			return;

		await UserData.InsertUser(_userModel);
		await _sfToast.ShowAsync();
	}
}