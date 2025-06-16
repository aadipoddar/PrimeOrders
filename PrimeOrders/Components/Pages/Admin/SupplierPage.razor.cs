using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Notifications;

namespace PrimeOrders.Components.Pages.Admin;

public partial class SupplierPage
{
	[Inject] public NavigationManager NavManager { get; set; }
	[Inject] public IJSRuntime JS { get; set; }

	private bool _isLoading = true;

	private SupplierModel _supplierModel = new()
	{
		Address = "",
		Email = "",
		Phone = "",
		GSTNo = "",
		Code = "",
		Status = true
	};

	private List<SupplierModel> _suppliers;
	private List<StateModel> _states;
	private List<LocationModel> _locations;

	private SfGrid<SupplierModel> _sfGrid;
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
			await LoadComboBox();
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

	private async Task LoadComboBox()
	{
		_suppliers = await CommonData.LoadTableData<SupplierModel>(TableNames.Supplier);
		_states = await CommonData.LoadTableData<StateModel>(TableNames.State);
		_locations = await CommonData.LoadTableData<LocationModel>(TableNames.Location);
		_supplierModel.StateId = _states.FirstOrDefault()?.Id ?? 0;
		_sfGrid?.Refresh();
		StateHasChanged();
	}

	public async void RowSelectHandler(RowSelectEventArgs<SupplierModel> args)
	{
		_supplierModel = args.Data;
		await _sfUpdateToast.ShowAsync();
		StateHasChanged();
	}

	private async Task<bool> ValidateForm()
	{
		if (string.IsNullOrWhiteSpace(_supplierModel.Name))
		{
			_sfErrorToast.Content = "Supplier name is required.";
			await _sfErrorToast.ShowAsync();
			return false;
		}

		if (string.IsNullOrWhiteSpace(_supplierModel.Code))
		{
			_sfErrorToast.Content = "Supplier code is required.";
			await _sfErrorToast.ShowAsync();
			return false;
		}

		if (string.IsNullOrWhiteSpace(_supplierModel.GSTNo))
		{
			_sfErrorToast.Content = "GST Number is required.";
			await _sfErrorToast.ShowAsync();
			return false;
		}

		if (_supplierModel.StateId <= 0)
		{
			_sfErrorToast.Content = "Please select a state.";
			await _sfErrorToast.ShowAsync();
			return false;
		}

		return true;
	}

	private async void OnSaveClick()
	{
		if (!await ValidateForm())
			return;

		await SupplierData.InsertSupplier(_supplierModel);
		await _sfToast.ShowAsync();
	}

	public void ClosedHandler(ToastCloseArgs args) =>
		NavManager.NavigateTo(NavManager.Uri, forceLoad: true);

	private void NavigateTo(string route) =>
		NavManager.NavigateTo(route);
}