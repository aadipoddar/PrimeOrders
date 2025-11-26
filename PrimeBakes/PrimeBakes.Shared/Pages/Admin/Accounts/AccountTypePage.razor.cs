using PrimeBakes.Shared.Services;

using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;

using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Notifications;

namespace PrimeBakes.Shared.Pages.Admin.Accounts;

public partial class AccountTypePage
{
    private UserModel _userModel;
    private bool _isLoading = true;
    private bool _isSubmitting = false;
    private string _successMessage = "Operation completed successfully!";
    private string _errorMessage = "An error occurred. Please try again.";

    private AccountTypeModel _accountTypeModel = new()
    {
        Name = "",
        Remarks = "",
        Status = true
    };

    private List<AccountTypeModel> _accountTypes = [];

    private SfGrid<AccountTypeModel> _sfGrid;
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
        _accountTypes = await CommonData.LoadTableData<AccountTypeModel>(TableNames.AccountType);

        if (_sfGrid is not null)
            await _sfGrid.Refresh();

        StateHasChanged();
    }

    private void OnRowSelected(RowSelectEventArgs<AccountTypeModel> args)
    {
        _accountTypeModel = new AccountTypeModel
        {
            Id = args.Data.Id,
            Name = args.Data.Name,
            Remarks = args.Data.Remarks ?? "",
            Status = args.Data.Status
        };

        StateHasChanged();
    }

    private async Task<bool> ValidateForm()
    {
        if (string.IsNullOrWhiteSpace(_accountTypeModel.Name))
        {
            _errorMessage = "Account type name is required. Please enter a valid account type name.";
            await ShowErrorToast();
            return false;
        }

        // Check for duplicate account type names
        if (_accountTypeModel.Id <= 0)
        {
            var existingAccountType = _accountTypes.FirstOrDefault(at =>
                at.Name.Equals(_accountTypeModel.Name, StringComparison.OrdinalIgnoreCase));

            if (existingAccountType != null)
            {
                _errorMessage = $"Account type name '{_accountTypeModel.Name}' already exists. Please choose a different name.";
                await ShowErrorToast();
                return false;
            }
        }
        else
        {
            var existingAccountType = _accountTypes.FirstOrDefault(at =>
                at.Id != _accountTypeModel.Id &&
                at.Name.Equals(_accountTypeModel.Name, StringComparison.OrdinalIgnoreCase));

            if (existingAccountType != null)
            {
                _errorMessage = $"Account type name '{_accountTypeModel.Name}' already exists. Please choose a different name.";
                await ShowErrorToast();
                return false;
            }
        }

        return true;
    }

    private void ResetForm()
    {
        _accountTypeModel = new()
        {
            Name = "",
            Remarks = "",
            Status = true
        };
    }

    private void OnEditAccountType(AccountTypeModel accountType)
    {
        _accountTypeModel = new AccountTypeModel
        {
            Id = accountType.Id,
            Name = accountType.Name,
            Remarks = accountType.Remarks ?? "",
            Status = accountType.Status
        };
        StateHasChanged();
    }

    private void OnAddAccountType()
    {
        ResetForm();
        StateHasChanged();
    }

    private async Task SaveAccountType()
    {
        if (!await ValidateForm())
            return;

        _isSubmitting = true;
        StateHasChanged();

        try
        {
            bool isNewAccountType = _accountTypeModel.Id <= 0;
            await AccountTypeData.InsertAccountType(_accountTypeModel);

            _successMessage = isNewAccountType
                ? $"Account type '{_accountTypeModel.Name}' has been created successfully."
                : $"Account type '{_accountTypeModel.Name}' has been updated successfully.";

            await ShowSuccessToast();
            await LoadData();
            ResetForm();
        }
        catch (Exception ex)
        {
            _errorMessage = $"Failed to save account type: {ex.Message}";
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