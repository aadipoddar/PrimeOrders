using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Notifications;

namespace PrimeOrders.Components.Pages.Admin;

public partial class KitchenPage
{
	[Inject] public NavigationManager NavManager { get; set; }
	[Inject] public IJSRuntime JS { get; set; }

	private bool _isLoading = true;

	private KitchenModel _kitchenModel = new()
	{
		Name = "",
		Status = true
	};

	private List<KitchenModel> _kitchens = [];
	private List<LocationModel> _locations = [];

	private SfGrid<KitchenModel> _sfGrid;
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
		_kitchens = await CommonData.LoadTableData<KitchenModel>(TableNames.Kitchen);

		await _sfGrid?.Refresh();
		StateHasChanged();
	}

	public async void RowSelectHandler(RowSelectEventArgs<KitchenModel> args)
	{
		_kitchenModel = args.Data;
		await _sfUpdateToast.ShowAsync();
		StateHasChanged();
	}

	private async Task<bool> ValidateForm()
	{
		if (string.IsNullOrWhiteSpace(_kitchenModel.Name))
		{
			_sfErrorToast.Content = "Kitchen name is required.";
			await _sfErrorToast.ShowAsync();
			return false;
		}

		return true;
	}

	private async void OnSaveClick()
	{
		if (!await ValidateForm())
			return;

		await KitchenData.InsertKitchen(_kitchenModel);
		await _sfToast.ShowAsync();
	}

	public void ClosedHandler(ToastCloseArgs args) =>
		NavManager.NavigateTo(NavManager.Uri, forceLoad: true);

	private void NavigateTo(string route) =>
		NavManager.NavigateTo(route);
}