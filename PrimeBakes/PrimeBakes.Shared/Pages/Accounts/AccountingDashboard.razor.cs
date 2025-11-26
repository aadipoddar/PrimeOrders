using Microsoft.JSInterop;

using PrimeBakes.Shared.Services;

using PrimeBakesLibrary.Models.Common;

namespace PrimeBakes.Shared.Pages.Accounts;

public partial class AccountingDashboard
{
    private bool _isLoading = true;

    protected override async Task OnInitializedAsync()
    {
        _isLoading = true;
        await AuthService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, UserRoles.Accounts, true);
        _isLoading = false;
    }

    #region Entry Pages Navigation
    private async Task NavigateToFinancialAccounting()
    {
        if (FormFactor.GetFormFactor() == "Web")
            await JSRuntime.InvokeVoidAsync("open", PageRouteNames.FinancialAccounting, "_blank");
        else
            NavigationManager.NavigateTo(PageRouteNames.FinancialAccounting, true);
    }
    #endregion

    #region Reports Navigation
    private async Task NavigateToFinancialAccountingReport()
    {
        if (FormFactor.GetFormFactor() == "Web")
            await JSRuntime.InvokeVoidAsync("open", PageRouteNames.ReportFinancialAccounting, "_blank");
        else
            NavigationManager.NavigateTo(PageRouteNames.ReportFinancialAccounting, true);
    }

    private async Task NavigateToLedgerReport()
    {
        if (FormFactor.GetFormFactor() == "Web")
            await JSRuntime.InvokeVoidAsync("open", PageRouteNames.ReportAccountingLedger, "_blank");
        else
            NavigationManager.NavigateTo(PageRouteNames.ReportAccountingLedger, true);
    }

    private async Task NavigateToTrialBalance()
    {
        if (FormFactor.GetFormFactor() == "Web")
            await JSRuntime.InvokeVoidAsync("open", PageRouteNames.ReportTrialBalance, "_blank");
        else
            NavigationManager.NavigateTo(PageRouteNames.ReportTrialBalance, true);
    }
    #endregion

    #region Master Pages Navigation
    private async Task NavigateToGroup()
    {
        if (FormFactor.GetFormFactor() == "Web")
            await JSRuntime.InvokeVoidAsync("open", PageRouteNames.AdminGroup, "_blank");
        else
            NavigationManager.NavigateTo(PageRouteNames.AdminGroup, true);
    }

    private async Task NavigateToAccountType()
    {
        if (FormFactor.GetFormFactor() == "Web")
            await JSRuntime.InvokeVoidAsync("open", PageRouteNames.AdminAccountType, "_blank");
        else
            NavigationManager.NavigateTo(PageRouteNames.AdminAccountType, true);
    }

    private async Task NavigateToLedger()
    {
        if (FormFactor.GetFormFactor() == "Web")
            await JSRuntime.InvokeVoidAsync("open", PageRouteNames.AdminLedger, "_blank");
        else
            NavigationManager.NavigateTo(PageRouteNames.AdminLedger, true);
    }

    private async Task NavigateToVoucher()
    {
        if (FormFactor.GetFormFactor() == "Web")
            await JSRuntime.InvokeVoidAsync("open", PageRouteNames.AdminVoucher, "_blank");
        else
            NavigationManager.NavigateTo(PageRouteNames.AdminVoucher, true);
    }

    private async Task NavigateToFinancialYear()
    {
        if (FormFactor.GetFormFactor() == "Web")
            await JSRuntime.InvokeVoidAsync("open", PageRouteNames.AdminFinancialYear, "_blank");
        else
            NavigationManager.NavigateTo(PageRouteNames.AdminFinancialYear, true);
    }

    private async Task NavigateToState()
    {
        if (FormFactor.GetFormFactor() == "Web")
            await JSRuntime.InvokeVoidAsync("open", PageRouteNames.AdminStateUT, "_blank");
        else
            NavigationManager.NavigateTo(PageRouteNames.AdminStateUT, true);
    }

    private async Task NavigateToCompany()
    {
        if (FormFactor.GetFormFactor() == "Web")
            await JSRuntime.InvokeVoidAsync("open", PageRouteNames.AdminCompany, "_blank");
        else
            NavigationManager.NavigateTo(PageRouteNames.AdminCompany, true);
    }
    #endregion

    private async Task Logout() =>
        await AuthService.Logout(DataStorageService, NavigationManager, NotificationService, VibrationService);
}