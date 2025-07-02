using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Notifications;

namespace PrimeOrders.Components.Pages.Admin;

public partial class LocationPage
{
	[Inject] public NavigationManager NavManager { get; set; }
	[Inject] public IJSRuntime JS { get; set; }

	private bool _isLoading = true;

	private LocationModel _locationModel = new()
	{
		Name = "",
		Discount = 0,
		Status = true
	};

	private List<LocationModel> _locations = [];

	private SfGrid<LocationModel> _sfGrid;
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

		return true;
	}

	private async Task LoadData()
	{
		_locations = await CommonData.LoadTableData<LocationModel>(TableNames.Location);

		await _sfGrid?.Refresh();
		StateHasChanged();
	}

	public async void RowSelectHandler(RowSelectEventArgs<LocationModel> args)
	{
		_locationModel = args.Data;
		await _sfUpdateToast.ShowAsync();
		StateHasChanged();
	}

	private async Task<bool> ValidateForm()
	{
		if (string.IsNullOrWhiteSpace(_locationModel.Name))
		{
			_sfErrorToast.Content = "Location name is required.";
			await _sfErrorToast.ShowAsync();
			return false;
		}

		if (_locationModel.Discount < 0 || _locationModel.Discount > 100)
		{
			_sfErrorToast.Content = "Discount must be between 0 and 100%.";
			await _sfErrorToast.ShowAsync();
			return false;
		}

		return true;
	}

	private async void OnSaveClick()
	{
		if (!await ValidateForm())
			return;

		await LocationData.InsertLocation(_locationModel);
		await _sfToast.ShowAsync();
	}

	public void ClosedHandler(ToastCloseArgs args) =>
		NavManager.NavigateTo(NavManager.Uri, forceLoad: true);

	private void NavigateTo(string route) =>
		NavManager.NavigateTo(route);
}