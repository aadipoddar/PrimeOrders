using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.Models.Accounts.Masters;

using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Notifications;

namespace PrimeOrders.Components.Pages.Accounts.Masters;

public partial class VoucherPage
{
	[Inject] public NavigationManager NavManager { get; set; }
	[Inject] public IJSRuntime JS { get; set; }

	private UserModel _user;
	private bool _isLoading = true;

	private VoucherModel _voucherModel = new()
	{
		Name = "",
		Remarks = "",
		Status = true
	};

	private List<VoucherModel> _vouchers = [];

	private SfGrid<VoucherModel> _sfGrid;
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
		_vouchers = await CommonData.LoadTableData<VoucherModel>(TableNames.Voucher);

		if (_sfGrid is not null)
			await _sfGrid.Refresh();

		StateHasChanged();
	}

	public async Task RowSelectHandler(RowSelectEventArgs<VoucherModel> args)
	{
		_voucherModel = args.Data;
		await _sfUpdateToast.ShowAsync();
		StateHasChanged();
	}

	private async Task<bool> ValidateForm()
	{
		_voucherModel.PrefixCode = _voucherModel.PrefixCode.ToUpper();

		if (string.IsNullOrWhiteSpace(_voucherModel.Name))
		{
			_sfErrorToast.Content = "Voucher name is required.";
			await _sfErrorToast.ShowAsync();
			return false;
		}

		if (string.IsNullOrWhiteSpace(_voucherModel.PrefixCode))
		{
			_sfErrorToast.Content = "Voucher Code is required.";
			await _sfErrorToast.ShowAsync();
			return false;
		}

		if (_voucherModel.Id > 0)
		{
			var existingVoucher = _vouchers.FirstOrDefault(_ => _.Id != _voucherModel.Id && _.PrefixCode.Equals(_voucherModel.PrefixCode, StringComparison.OrdinalIgnoreCase));
			if (existingVoucher is not null)
			{
				_sfErrorToast.Content = "Voucher with the same prefix code already exists.";
				await _sfErrorToast.ShowAsync();
				return false;
			}

			existingVoucher = _vouchers.FirstOrDefault(_ => _.Id != _voucherModel.Id && _.Name.Equals(_voucherModel.Name, StringComparison.OrdinalIgnoreCase));
			if (existingVoucher is not null)
			{
				_sfErrorToast.Content = "Voucher with the same name already exists.";
				await _sfErrorToast.ShowAsync();
				return false;
			}
		}

		else
		{
			var existingVoucher = _vouchers.FirstOrDefault(_ => _.PrefixCode.Equals(_voucherModel.PrefixCode, StringComparison.OrdinalIgnoreCase));
			if (existingVoucher is not null)
			{
				_sfErrorToast.Content = "Voucher with the same prefix code already exists.";
				await _sfErrorToast.ShowAsync();
				return false;
			}

			existingVoucher = _vouchers.FirstOrDefault(_ => _.Name.Equals(_voucherModel.Name, StringComparison.OrdinalIgnoreCase));
			if (existingVoucher is not null)
			{
				_sfErrorToast.Content = "Voucher with the same name already exists.";
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

		await VoucherData.InsertVoucher(_voucherModel);
		await _sfToast.ShowAsync();
	}
}