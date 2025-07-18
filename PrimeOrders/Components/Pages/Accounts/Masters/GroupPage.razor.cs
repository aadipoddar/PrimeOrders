using PrimeOrdersLibrary.Data.Accounts;
using PrimeOrdersLibrary.Models.Accounts;

using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Notifications;

namespace PrimeOrders.Components.Pages.Accounts.Masters;

public partial class GroupPage
{
	[Inject] public NavigationManager NavManager { get; set; }
	[Inject] public IJSRuntime JS { get; set; }

	private UserModel _user;
	private bool _isLoading = true;

	private GroupModel _groupModel = new()
	{
		Name = "",
		Remarks = "",
		Status = true
	};

	private List<GroupModel> _groups = [];

	private SfGrid<GroupModel> _sfGrid;
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
		_groups = await CommonData.LoadTableData<GroupModel>(TableNames.Group);

		if (_sfGrid is not null)
			await _sfGrid.Refresh();

		StateHasChanged();
	}

	public async Task RowSelectHandler(RowSelectEventArgs<GroupModel> args)
	{
		_groupModel = args.Data;
		await _sfUpdateToast.ShowAsync();
		StateHasChanged();
	}

	private async Task<bool> ValidateForm()
	{
		if (string.IsNullOrWhiteSpace(_groupModel.Name))
		{
			_sfErrorToast.Content = "Group name is required.";
			await _sfErrorToast.ShowAsync();
			return false;
		}

		// Check for duplicate group names
		if (_groupModel.Id <= 0)
		{
			var existingGroup = _groups.FirstOrDefault(g =>
				g.Name.Equals(_groupModel.Name, StringComparison.OrdinalIgnoreCase));

			if (existingGroup != null)
			{
				_sfErrorToast.Content = "A group with this name already exists.";
				await _sfErrorToast.ShowAsync();
				return false;
			}
		}
		else
		{
			var existingGroup = _groups.FirstOrDefault(g =>
				g.Id != _groupModel.Id &&
				g.Name.Equals(_groupModel.Name, StringComparison.OrdinalIgnoreCase));

			if (existingGroup != null)
			{
				_sfErrorToast.Content = "A group with this name already exists.";
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

		await GroupData.InsertGroup(_groupModel);
		await _sfToast.ShowAsync();
	}
}