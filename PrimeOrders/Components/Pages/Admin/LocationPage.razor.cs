using PrimeOrdersLibrary.Data.Inventory.Purchase;

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
		MainLocation = false,
		Status = true
	};

	private List<LocationModel> _locations = [];

	private SfGrid<LocationModel> _sfGrid;
	private SfToast _sfToast;
	private SfToast _sfUpdateToast;
	private SfToast _sfErrorToast;

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		_isLoading = true;

		if (!((await AuthService.ValidateUser(JS, NavManager, UserRoles.Admin, true)).User is not null))
			return;

		await LoadData();

		_isLoading = false;
		StateHasChanged();
	}

	private async Task LoadData()
	{
		_locations = await CommonData.LoadTableData<LocationModel>(TableNames.Location);

		if (_sfGrid is not null)
			await _sfGrid.Refresh();

		StateHasChanged();
	}

	public async Task RowSelectHandler(RowSelectEventArgs<LocationModel> args)
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

	private async Task OnSaveClick()
	{
		if (!await ValidateForm())
			return;

		await LocationData.InsertLocation(_locationModel);
		await InsertSupplier();

		await _sfToast.ShowAsync();
	}

	private async Task InsertSupplier()
	{
		SupplierModel supplier = null;
		if (_locationModel.Id > 0)
			supplier = await SupplierData.LoadSupplierByLocation(_locationModel.Id);

		await SupplierData.InsertSupplier(new()
		{
			Id = supplier?.Id ?? 0,
			Name = _locationModel.Name,
			LocationId = _locationModel.Id,
			Code = await GenerateBillNo.GetLocationPrefix(_locationModel.Id),
			Address = "",
			Email = "",
			GSTNo = "",
			Phone = "",
			StateId = 2,
			Status = true
		});
	}
}