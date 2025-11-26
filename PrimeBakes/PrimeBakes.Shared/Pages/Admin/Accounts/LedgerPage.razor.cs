using PrimeBakes.Shared.Services;

using PrimeBakesLibrary.Data;
using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;

using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Notifications;

namespace PrimeBakes.Shared.Pages.Admin.Accounts;

public partial class LedgerPage
{
    private bool _isLoading = true;
    private bool _isSubmitting = false;

    private LedgerModel _ledgerModel = new()
    {
        Id = 0,
        Name = "",
        Alias = "",
        Code = "",
        GroupId = 0,
        AccountTypeId = 0,
        Phone = "",
        Email = "",
        Address = "",
        GSTNo = "",
        Remarks = "",
        StateUTId = 0,
        LocationId = null,
        Status = true
    };

    private List<LedgerModel> _ledgers = [];
    private List<GroupModel> _groups = [];
    private List<AccountTypeModel> _accountTypes = [];
    private List<StateUTModel> _states = [];
    private List<LocationModel> _locations = [];

    private SfGrid<LedgerModel> _sfGrid;
    private SfToast _sfToast;
    private SfToast _sfErrorToast;

    // Toast message properties
    private string _successMessage = "Operation completed successfully!";
    private string _errorMessage = "An error occurred. Please try again.";

    protected override async Task OnInitializedAsync()
    {
        var authResult = await AuthService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, UserRoles.Admin, true);
        await LoadLedgers();

        _isLoading = false;
        StateHasChanged();
    }

    private async Task LoadLedgers()
    {
        try
        {
            _ledgers = await CommonData.LoadTableData<LedgerModel>(TableNames.Ledger);
            _groups = await CommonData.LoadTableData<GroupModel>(TableNames.Group);
            _accountTypes = await CommonData.LoadTableData<AccountTypeModel>(TableNames.AccountType);
            _states = await CommonData.LoadTableData<StateUTModel>(TableNames.StateUT);
            _locations = await CommonData.LoadTableData<LocationModel>(TableNames.Location);

            _ledgerModel.Code = await GenerateCodes.GenerateLedgerCode();

            if (_sfGrid is not null)
                await _sfGrid.Refresh();
            StateHasChanged();
        }
        catch (Exception ex)
        {
            _errorMessage = $"Failed to load ledger data: {ex.Message}";
            await ShowErrorToast();
        }
    }

    private async Task OnAddLedger()
    {
        _ledgerModel = new()
        {
            Id = 0,
            Name = "",
            Alias = "",
            Code = await GenerateCodes.GenerateLedgerCode(),
            GroupId = 0,
            AccountTypeId = 0,
            Phone = "",
            Email = "",
            Address = "",
            GSTNo = "",
            Remarks = "",
            StateUTId = 0,
            LocationId = null,
            Status = true
        };
        StateHasChanged();
    }

    private void OnEditLedger(LedgerModel ledger)
    {
        _ledgerModel = new()
        {
            Id = ledger.Id,
            Name = ledger.Name,
            Alias = ledger.Alias,
            Code = ledger.Code,
            GroupId = ledger.GroupId,
            AccountTypeId = ledger.AccountTypeId,
            Phone = ledger.Phone,
            Email = ledger.Email,
            Address = ledger.Address,
            GSTNo = ledger.GSTNo,
            Remarks = ledger.Remarks,
            StateUTId = ledger.StateUTId,
            LocationId = ledger.LocationId,
            Status = ledger.Status
        };
        StateHasChanged();
    }

    private async Task ToggleLedgerStatus(LedgerModel ledger)
    {
        try
        {
            ledger.Status = !ledger.Status;
            await LedgerData.InsertLedger(ledger);
            await LoadLedgers();

            _successMessage = $"Ledger '{ledger.Name}' has been {(ledger.Status ? "activated" : "deactivated")} successfully.";
            await ShowSuccessToast();

            OnAddLedger();
        }
        catch (Exception ex)
        {
            _errorMessage = $"Failed to update ledger status: {ex.Message}";
            await ShowErrorToast();
        }
    }

    private async Task<bool> ValidateForm()
    {
        if (string.IsNullOrWhiteSpace(_ledgerModel.Name))
        {
            _errorMessage = "Ledger name is required. Please enter a valid name.";
            await ShowErrorToast();
            return false;
        }

        if (string.IsNullOrWhiteSpace(_ledgerModel.Code))
        {
            _errorMessage = "Ledger code is required. Please enter a valid code.";
            await ShowErrorToast();
            return false;
        }

        if (_ledgerModel.GroupId <= 0)
        {
            _errorMessage = "Group selection is required. Please select a valid group.";
            await ShowErrorToast();
            return false;
        }

        if (_ledgerModel.AccountTypeId <= 0)
        {
            _errorMessage = "Account type selection is required. Please select a valid account type.";
            await ShowErrorToast();
            return false;
        }

        if (_ledgerModel.StateUTId <= 0)
        {
            _errorMessage = "State selection is required. Please select a valid state.";
            await ShowErrorToast();
            return false;
        }

        // Check for duplicate names and codes
        if (_ledgerModel.Id > 0)
        {
            var existingLedger = _ledgers.FirstOrDefault(_ => _.Id != _ledgerModel.Id && _.Name.Equals(_ledgerModel.Name, StringComparison.OrdinalIgnoreCase));
            if (existingLedger is not null)
            {
                _errorMessage = $"Ledger name '{_ledgerModel.Name}' already exists. Please choose a different name.";
                await ShowErrorToast();
                return false;
            }

            existingLedger = _ledgers.FirstOrDefault(_ => _.Id != _ledgerModel.Id && _.Code.Equals(_ledgerModel.Code, StringComparison.OrdinalIgnoreCase));
            if (existingLedger is not null)
            {
                _errorMessage = $"Ledger code '{_ledgerModel.Code}' is already used by '{existingLedger.Name}'. Please choose a different code.";
                await ShowErrorToast();
                return false;
            }
        }
        else
        {
            var existingLedger = _ledgers.FirstOrDefault(_ => _.Name.Equals(_ledgerModel.Name, StringComparison.OrdinalIgnoreCase));
            if (existingLedger is not null)
            {
                _errorMessage = $"Ledger name '{_ledgerModel.Name}' already exists. Please choose a different name.";
                await ShowErrorToast();
                return false;
            }

            existingLedger = _ledgers.FirstOrDefault(_ => _.Code.Equals(_ledgerModel.Code, StringComparison.OrdinalIgnoreCase));
            if (existingLedger is not null)
            {
                _errorMessage = $"Ledger code '{_ledgerModel.Code}' is already used by '{existingLedger.Name}'. Please choose a different code.";
                await ShowErrorToast();
                return false;
            }
        }

        // Validate location business rule: not more than one ledger can be connected to the same location
        if (_ledgerModel.LocationId.HasValue && _ledgerModel.LocationId > 0)
        {
            var existingLedger = _ledgers.FirstOrDefault(_ => _.Id != _ledgerModel.Id && _.LocationId == _ledgerModel.LocationId);
            if (existingLedger is not null)
            {
                var locationName = _locations.FirstOrDefault(l => l.Id == _ledgerModel.LocationId)?.Name ?? "Unknown";
                _errorMessage = $"Location '{locationName}' is already connected to ledger '{existingLedger.Name}'. Only one ledger can be connected to each location.";
                await ShowErrorToast();
                return false;
            }
        }

        return true;
    }

    private async Task SaveLedger()
    {
        try
        {
            if (_isSubmitting || !await ValidateForm())
                return;

            _isSubmitting = true;
            StateHasChanged();

            var isNewLedger = _ledgerModel.Id == 0;
            var ledgerName = _ledgerModel.Name;

            await LedgerData.InsertLedger(_ledgerModel);
            await LoadLedgers();

            // Reset form
            OnAddLedger();

            _successMessage = isNewLedger
                ? $"Ledger '{ledgerName}' has been created successfully!"
                : $"Ledger '{ledgerName}' has been updated successfully!";
            await ShowSuccessToast();
        }
        catch (Exception ex)
        {
            _errorMessage = $"Failed to save ledger: {ex.Message}";
            await ShowErrorToast();
        }
        finally
        {
            _isSubmitting = false;
            StateHasChanged();
        }
    }

    public void RowSelectHandler(RowSelectEventArgs<LedgerModel> args) =>
        OnEditLedger(args.Data);

    // Helper methods for showing toasts with dynamic content
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

    // Helper methods to get display names
    private string GetGroupName(int groupId)
    {
        return _groups.FirstOrDefault(g => g.Id == groupId)?.Name ?? "Unknown";
    }

    private string GetAccountTypeName(int accountTypeId)
    {
        return _accountTypes.FirstOrDefault(at => at.Id == accountTypeId)?.Name ?? "Unknown";
    }

    private string GetStateName(int stateId)
    {
        return _states.FirstOrDefault(s => s.Id == stateId)?.Name ?? "Unknown";
    }

    private string GetLocationName(int? locationId)
    {
        if (!locationId.HasValue || locationId == 0)
            return "Not Assigned";
        return _locations.FirstOrDefault(l => l.Id == locationId)?.Name ?? "Unknown";
    }
}