using Microsoft.AspNetCore.Components;
using PrimeBakes.Shared.Services;
using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;
using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Notifications;

namespace PrimeBakes.Shared.Pages.Admin.Accounts;

public partial class AccountVoucherPage
{
    private UserModel _userModel;
    private bool _isLoading = true;
    private bool _isSubmitting = false;
    private string _successMessage = "Operation completed successfully!";
    private string _errorMessage = "An error occurred. Please try again.";

    private VoucherModel _voucherModel = new()
    {
        Name = "",
        PrefixCode = "",
        Remarks = "",
        Status = true
    };

    private List<VoucherModel> _vouchers = [];

    private SfGrid<VoucherModel> _sfGrid;
    private SfToast _sfToast;
    private SfToast _sfErrorToast;

    protected override async Task OnInitializedAsync()
    {
        var authResult = await AuthService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, UserRoles.Admin, true);
        _userModel = authResult.User;
        await LoadData();
        _isLoading = false;
    }

    private async Task LoadData()
    {
        _vouchers = await CommonData.LoadTableData<VoucherModel>(TableNames.Voucher);

        if (_sfGrid is not null)
            await _sfGrid.Refresh();

        StateHasChanged();
    }

    private void OnRowSelected(RowSelectEventArgs<VoucherModel> args)
    {
        _voucherModel = new VoucherModel
        {
            Id = args.Data.Id,
            Name = args.Data.Name,
            PrefixCode = args.Data.PrefixCode,
            Remarks = args.Data.Remarks ?? "",
            Status = args.Data.Status
        };

        StateHasChanged();
    }

    private async Task<bool> ValidateForm()
    {
        if (string.IsNullOrWhiteSpace(_voucherModel.Name))
        {
            _errorMessage = "Voucher name is required. Please enter a valid voucher name.";
            await ShowErrorToast();
            return false;
        }

        if (string.IsNullOrWhiteSpace(_voucherModel.PrefixCode))
        {
            _errorMessage = "Prefix code is required. Please enter a valid prefix code.";
            await ShowErrorToast();
            return false;
        }

        // Check for duplicate voucher names
        if (_voucherModel.Id <= 0)
        {
            var existingVoucher = _vouchers.FirstOrDefault(v =>
                v.Name.Equals(_voucherModel.Name, StringComparison.OrdinalIgnoreCase));

            if (existingVoucher != null)
            {
                _errorMessage = $"Voucher name '{_voucherModel.Name}' already exists. Please choose a different name.";
                await ShowErrorToast();
                return false;
            }

            var existingPrefixCode = _vouchers.FirstOrDefault(v =>
                v.PrefixCode.Equals(_voucherModel.PrefixCode, StringComparison.OrdinalIgnoreCase));

            if (existingPrefixCode != null)
            {
                _errorMessage = $"Prefix code '{_voucherModel.PrefixCode}' already exists. Please choose a different prefix code.";
                await ShowErrorToast();
                return false;
            }
        }
        else
        {
            var existingVoucher = _vouchers.FirstOrDefault(v =>
                v.Id != _voucherModel.Id &&
                v.Name.Equals(_voucherModel.Name, StringComparison.OrdinalIgnoreCase));

            if (existingVoucher != null)
            {
                _errorMessage = $"Voucher name '{_voucherModel.Name}' already exists. Please choose a different name.";
                await ShowErrorToast();
                return false;
            }

            var existingPrefixCode = _vouchers.FirstOrDefault(v =>
                v.Id != _voucherModel.Id &&
                v.PrefixCode.Equals(_voucherModel.PrefixCode, StringComparison.OrdinalIgnoreCase));

            if (existingPrefixCode != null)
            {
                _errorMessage = $"Prefix code '{_voucherModel.PrefixCode}' already exists. Please choose a different prefix code.";
                await ShowErrorToast();
                return false;
            }
        }

        return true;
    }

    private void ResetForm()
    {
        _voucherModel = new()
        {
            Name = "",
            PrefixCode = "",
            Remarks = "",
            Status = true
        };
    }

    private void OnEditVoucher(VoucherModel voucher)
    {
        _voucherModel = new VoucherModel
        {
            Id = voucher.Id,
            Name = voucher.Name,
            PrefixCode = voucher.PrefixCode,
            Remarks = voucher.Remarks ?? "",
            Status = voucher.Status
        };
        StateHasChanged();
    }

    private void OnAddVoucher()
    {
        ResetForm();
        StateHasChanged();
    }

    private async Task SaveVoucher()
    {
        if (!await ValidateForm())
            return;

        _isSubmitting = true;
        StateHasChanged();

        try
        {
            bool isNewVoucher = _voucherModel.Id <= 0;
            await VoucherData.InsertVoucher(_voucherModel);

            _successMessage = isNewVoucher
                ? $"Voucher '{_voucherModel.Name}' has been created successfully."
                : $"Voucher '{_voucherModel.Name}' has been updated successfully.";

            await ShowSuccessToast();
            await LoadData();
            ResetForm();
        }
        catch (Exception ex)
        {
            _errorMessage = $"Failed to save voucher: {ex.Message}";
            await ShowErrorToast();
            Console.WriteLine(ex.Message);
        }
        finally
        {
            _isSubmitting = false;
            StateHasChanged();
        }
    }

    private async Task ShowSuccessToast()
    {
        if (_sfToast != null)
        {
            var toastModel = new ToastModel
            {
                Title = "Success",
                Content = _successMessage,
                CssClass = "e-toast-success",
                Icon = "e-success toast-icons"
            };
            await _sfToast.ShowAsync(toastModel);
        }
    }

    private async Task ShowErrorToast()
    {
        if (_sfErrorToast != null)
        {
            var toastModel = new ToastModel
            {
                Title = "Error",
                Content = _errorMessage,
                CssClass = "e-toast-danger",
                Icon = "e-error toast-icons"
            };
            await _sfErrorToast.ShowAsync(toastModel);
        }
    }
}