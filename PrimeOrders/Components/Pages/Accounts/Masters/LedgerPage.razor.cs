using PrimeOrdersLibrary.Data.Accounts.Masters;
using PrimeOrdersLibrary.Models.Accounts.Masters;

using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Notifications;

namespace PrimeOrders.Components.Pages.Accounts.Masters;

public partial class LedgerPage
{
	[Inject] public NavigationManager NavManager { get; set; }
	[Inject] public IJSRuntime JS { get; set; }

	private UserModel _user;
	private bool _isLoading = true;

	private LedgerModel _ledgerModel = new()
	{
		Name = "",
		Code = "",
		GroupId = 0,
		AccountTypeId = 0,
		Phone = "",
		Address = "",
		GSTNo = "",
		Remarks = "",
		StateId = 0,
		LocationId = null,
		Status = true
	};

	private List<LedgerModel> _ledgers = [];
	private List<GroupModel> _groups = [];
	private List<AccountTypeModel> _accountTypes = [];
	private List<StateModel> _states = [];
	private List<LocationModel> _locations = [];

	private SfGrid<LedgerModel> _sfGrid;
	private SfToast _sfToast;
	private SfToast _sfUpdateToast;
	private SfToast _sfErrorToast;

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		_isLoading = true;

		if (!((_user = (await AuthService.ValidateUser(JS, NavManager, UserRoles.Accounts, true)).User) is not null))
			return;

		await LoadData();

		_isLoading = false;
		StateHasChanged();
	}

	private async Task LoadData()
	{
		_ledgers = await CommonData.LoadTableData<LedgerModel>(TableNames.Ledger);
		_groups = await CommonData.LoadTableData<GroupModel>(TableNames.Group);
		_accountTypes = await CommonData.LoadTableData<AccountTypeModel>(TableNames.AccountType);
		_states = await CommonData.LoadTableData<StateModel>(TableNames.State);
		_locations = await CommonData.LoadTableData<LocationModel>(TableNames.Location);

		_ledgerModel.StateId = _states.FirstOrDefault().Id;
		_ledgerModel.GroupId = _groups.FirstOrDefault().Id;
		_ledgerModel.AccountTypeId = _accountTypes.FirstOrDefault().Id;

		_ledgerModel.Code = GenerateCodes.GenerateLedgerCode(_ledgers.OrderBy(r => r.Code).LastOrDefault()?.Code);

		if (_sfGrid is not null)
			await _sfGrid.Refresh();

		StateHasChanged();
	}

	public async Task RowSelectHandler(RowSelectEventArgs<LedgerModel> args)
	{
		_ledgerModel = args.Data;
		await _sfUpdateToast.ShowAsync();
		StateHasChanged();
	}

	private async Task<bool> ValidateForm()
	{
		if (_ledgerModel.Id == 0)
			_ledgerModel.Code = GenerateCodes.GenerateLedgerCode(_ledgers.OrderBy(r => r.Code).LastOrDefault()?.Code);

		if (string.IsNullOrWhiteSpace(_ledgerModel.Name))
		{
			_sfErrorToast.Content = "Ledger name is required.";
			await _sfErrorToast.ShowAsync();
			return false;
		}

		if (_ledgerModel.GroupId <= 0)
		{
			_sfErrorToast.Content = "Please select an account group.";
			await _sfErrorToast.ShowAsync();
			return false;
		}

		if (_ledgerModel.AccountTypeId <= 0)
		{
			_sfErrorToast.Content = "Please select an account type.";
			await _sfErrorToast.ShowAsync();
			return false;
		}

		if (_ledgerModel.StateId <= 0)
		{
			_sfErrorToast.Content = "Please select a state.";
			await _sfErrorToast.ShowAsync();
			return false;
		}

		return true;
	}

	private async Task OnSaveClick()
	{
		if (!await ValidateForm())
			return;

		await LedgerData.InsertLedger(_ledgerModel);
		await _sfToast.ShowAsync();
	}
}