using PrimeOrdersLibrary.Data.Accounts.Masters;
using PrimeOrdersLibrary.Models.Accounts.Masters;

using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Notifications;

namespace PrimeOrders.Components.Pages.Accounts.Masters;

public partial class AccountTypePage
{
	[Inject] public NavigationManager NavManager { get; set; }
	[Inject] public IJSRuntime JS { get; set; }

	private UserModel _user;
	private bool _isLoading = true;

	private AccountTypeModel _accountTypeModel = new()
	{
		Name = "",
		Remarks = "",
		Status = true
	};

	private List<AccountTypeModel> _accountTypes = [];

	private SfGrid<AccountTypeModel> _sfGrid;
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
		_accountTypes = await CommonData.LoadTableData<AccountTypeModel>(TableNames.AccountType);

		if (_sfGrid is not null)
			await _sfGrid.Refresh();

		StateHasChanged();
	}

	public async Task RowSelectHandler(RowSelectEventArgs<AccountTypeModel> args)
	{
		_accountTypeModel = args.Data;
		await _sfUpdateToast.ShowAsync();
		StateHasChanged();
	}

	private async Task<bool> ValidateForm()
	{
		if (string.IsNullOrWhiteSpace(_accountTypeModel.Name))
		{
			_sfErrorToast.Content = "Account type name is required.";
			await _sfErrorToast.ShowAsync();
			return false;
		}

		// Check for duplicate account type names
		if (_accountTypeModel.Id <= 0)
		{
			var existingAccountType = _accountTypes.FirstOrDefault(at =>
				at.Name.Equals(_accountTypeModel.Name, StringComparison.OrdinalIgnoreCase));

			if (existingAccountType is not null)
			{
				_sfErrorToast.Content = "An account type with this name already exists.";
				await _sfErrorToast.ShowAsync();
				return false;
			}
		}
		else
		{
			var existingAccountType = _accountTypes.FirstOrDefault(at =>
				at.Id != _accountTypeModel.Id &&
				at.Name.Equals(_accountTypeModel.Name, StringComparison.OrdinalIgnoreCase));

			if (existingAccountType is not null)
			{
				_sfErrorToast.Content = "An account type with this name already exists.";
				await _sfErrorToast.ShowAsync();
				return false;
			}
		}

		return true;
	}

	private async Task OnSaveClick()
	{
		if (!await ValidateForm())
			return;

		await AccountTypeData.InsertAccountType(_accountTypeModel);
		await _sfToast.ShowAsync();
	}
}