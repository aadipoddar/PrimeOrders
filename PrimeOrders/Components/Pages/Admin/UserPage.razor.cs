using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Notifications;

namespace PrimeOrders.Components.Pages.Admin;

public partial class UserPage
{
	[Inject] public NavigationManager NavManager { get; set; }
	[Inject] public IJSRuntime JS { get; set; }

	private bool _isLoading = true;
	private UserModel _currentUser;
	private string _password = "";

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
		_isLoading = true;

		if (firstRender && !await ValidatePassword())
			NavManager.NavigateTo("/Login");

		_isLoading = false;
		StateHasChanged();

		if (firstRender)
			await LoadData();
	}

	private async Task<bool> ValidatePassword()
	{
		var userId = await JS.InvokeAsync<string>("getCookie", "UserId");
		var password = await JS.InvokeAsync<string>("getCookie", "Passcode");

		if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(password))
			return false;

		var user = await CommonData.LoadTableDataById<UserModel>(TableNames.User, int.Parse(userId));
		if (user is null || !BCrypt.Net.BCrypt.EnhancedVerify(user.Passcode.ToString(), password))
			return false;

		if (!user.Admin)
		{
			_sfErrorToast.Content = "You do not have permission to access this page.";
			await _sfErrorToast.ShowAsync();
			return false;
		}

		_currentUser = user;
		return true;
	}

	private async Task LoadData()
	{
		_users = await CommonData.LoadTableData<UserModel>(TableNames.User);
		_locations = await CommonData.LoadTableData<LocationModel>(TableNames.Location);

		if (_currentUser.LocationId != 1)
			_users = [.. _users.Where(u => u.LocationId == _currentUser.LocationId)];

		_userModel.LocationId = _currentUser.LocationId;

		await _sfGrid?.Refresh();
		StateHasChanged();
	}

	public async void RowSelectHandler(RowSelectEventArgs<UserModel> args)
	{
		_userModel = args.Data;
		await _sfUpdateToast.ShowAsync();
		StateHasChanged();
	}

	private async Task<bool> ValidateForm()
	{
		if (_currentUser.LocationId != 1)
			_userModel.LocationId = _currentUser.LocationId;

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

		if (await UserData.LoadUserByPasscode(_userModel.Passcode) is not null)
		{
			_sfErrorToast.Content = "Passcode Already Present. Try a Different one";
			await _sfErrorToast.ShowAsync();
			return false;
		}

		return true;
	}

	private async void OnSaveClick()
	{
		if (!await ValidateForm())
			return;

		await UserData.InsertUser(_userModel);
		await _sfToast.ShowAsync();
	}

	public void ClosedHandler(ToastCloseArgs args) =>
		NavManager.NavigateTo(NavManager.Uri, forceLoad: true);

	private void NavigateTo(string route) =>
		NavManager.NavigateTo(route);
}