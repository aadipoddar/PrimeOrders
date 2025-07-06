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

	private SfGrid<KitchenModel> _sfGrid;
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
		_kitchens = await CommonData.LoadTableData<KitchenModel>(TableNames.Kitchen);

		if (_sfGrid is not null)
			await _sfGrid.Refresh();

		StateHasChanged();
	}

	public async Task RowSelectHandler(RowSelectEventArgs<KitchenModel> args)
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

	private async Task OnSaveClick()
	{
		if (!await ValidateForm())
			return;

		await KitchenData.InsertKitchen(_kitchenModel);
		await _sfToast.ShowAsync();
	}
}