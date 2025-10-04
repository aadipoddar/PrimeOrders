using PrimeBakes.Shared.Services;

using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;

using Syncfusion.Blazor.Grids;

namespace PrimeBakes.Shared.Pages.Admin;

public partial class LedgerPage
{
	private bool _isLoading = true;

	private LedgerModel _ledgerModel = new()
	{
		Id = 0,
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

	protected override async Task OnInitializedAsync()
	{
		var authResult = await AuthService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, UserRoles.Admin, true);
		await LoadLedgers();

		_isLoading = false;
		StateHasChanged();
	}

	private async Task LoadLedgers()
	{
		_ledgers = await CommonData.LoadTableData<LedgerModel>(TableNames.Ledger);
		_groups = await CommonData.LoadTableData<GroupModel>(TableNames.Group);
		_accountTypes = await CommonData.LoadTableData<AccountTypeModel>(TableNames.AccountType);
		_states = await CommonData.LoadTableData<StateModel>(TableNames.State);
		_locations = await CommonData.LoadTableData<LocationModel>(TableNames.Location);

		if (_sfGrid is not null)
			await _sfGrid.Refresh();
		StateHasChanged();
	}
}