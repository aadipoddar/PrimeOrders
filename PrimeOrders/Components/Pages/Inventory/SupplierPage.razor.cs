using PrimeOrdersLibrary.Data.Inventory;

using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Notifications;

namespace PrimeOrders.Components.Pages.Inventory;

public partial class SupplierPage
{
	[Inject] public NavigationManager NavManager { get; set; }
	[Inject] public IJSRuntime JS { get; set; }

	private bool IsLoading { get; set; } = true;

	private SupplierModel _supplierModel = new() { Status = true };

	private List<SupplierModel> _suppliers;

	private SfGrid<SupplierModel> _sfGrid;
	private SfToast _sfToast;
	private SfToast _sfUpdateToast;

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		IsLoading = true;

		if (firstRender && !await ValidatePassword())
			NavManager.NavigateTo("/Login");

		IsLoading = false;
		StateHasChanged();

		if (firstRender)
			await LoadSuppliers();
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

	private async Task LoadSuppliers()
	{
		_suppliers = await CommonData.LoadTableData<SupplierModel>(TableNames.Supplier);
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
		if (string.IsNullOrWhiteSpace(_supplierModel.Name) || string.IsNullOrWhiteSpace(_supplierModel.Code))
		{
			await _sfToast.ShowAsync(new()
			{
				Title = "Validation Error",
				Content = "Name and Code are required.",
			});
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