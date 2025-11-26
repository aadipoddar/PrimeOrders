using Microsoft.JSInterop;

using PrimeBakes.Shared.Services;

using PrimeBakesLibrary.Models.Common;

namespace PrimeBakes.Shared.Pages.Reports;

public partial class ReportsDashboard
{
    private bool _isLoading = true;
    private UserModel _user;

    protected override async Task OnInitializedAsync()
    {
        _isLoading = true;
        var authResult = await AuthService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService);
        _user = authResult.User;
        _isLoading = false;
    }

    #region Sales Reports Navigation
    private async Task NavigateToSaleReport()
    {
        if (FormFactor.GetFormFactor() == "Web")
            await JSRuntime.InvokeVoidAsync("open", PageRouteNames.ReportSale, "_blank");
        else
            NavigationManager.NavigateTo(PageRouteNames.ReportSale, true);
    }

    private async Task NavigateToSaleReturnReport()
    {
        if (FormFactor.GetFormFactor() == "Web")
            await JSRuntime.InvokeVoidAsync("open", PageRouteNames.ReportSaleReturn, "_blank");
        else
            NavigationManager.NavigateTo(PageRouteNames.ReportSaleReturn, true);
    }

    private async Task NavigateToSaleItemReport()
    {
        if (FormFactor.GetFormFactor() == "Web")
            await JSRuntime.InvokeVoidAsync("open", PageRouteNames.ReportSaleItem, "_blank");
        else
            NavigationManager.NavigateTo(PageRouteNames.ReportSaleItem, true);
    }

    private async Task NavigateToSaleReturnItemReport()
    {
        if (FormFactor.GetFormFactor() == "Web")
            await JSRuntime.InvokeVoidAsync("open", PageRouteNames.ReportSaleReturnItem, "_blank");
        else
            NavigationManager.NavigateTo(PageRouteNames.ReportSaleReturnItem, true);
    }

    private async Task NavigateToOrderReport()
    {
        if (FormFactor.GetFormFactor() == "Web")
            await JSRuntime.InvokeVoidAsync("open", PageRouteNames.ReportOrder, "_blank");
        else
            NavigationManager.NavigateTo(PageRouteNames.ReportOrder, true);
    }

    private async Task NavigateToOrderItemReport()
    {
        if (FormFactor.GetFormFactor() == "Web")
            await JSRuntime.InvokeVoidAsync("open", PageRouteNames.ReportOrderItem, "_blank");
        else
            NavigationManager.NavigateTo(PageRouteNames.ReportOrderItem, true);
    }
    #endregion

    #region Inventory Reports Navigation
    private async Task NavigateToPurchaseReport()
    {
        if (FormFactor.GetFormFactor() == "Web")
            await JSRuntime.InvokeVoidAsync("open", PageRouteNames.ReportPurchase, "_blank");
        else
            NavigationManager.NavigateTo(PageRouteNames.ReportPurchase, true);
    }

    private async Task NavigateToPurchaseReturnReport()
    {
        if (FormFactor.GetFormFactor() == "Web")
            await JSRuntime.InvokeVoidAsync("open", PageRouteNames.ReportPurchaseReturn, "_blank");
        else
            NavigationManager.NavigateTo(PageRouteNames.ReportPurchaseReturn, true);
    }

    private async Task NavigateToPurchaseItemReport()
    {
        if (FormFactor.GetFormFactor() == "Web")
            await JSRuntime.InvokeVoidAsync("open", PageRouteNames.ReportPurchaseItem, "_blank");
        else
            NavigationManager.NavigateTo(PageRouteNames.ReportPurchaseItem, true);
    }

    private async Task NavigateToPurchaseReturnItemReport()
    {
        if (FormFactor.GetFormFactor() == "Web")
            await JSRuntime.InvokeVoidAsync("open", PageRouteNames.ReportPurchaseReturnItem, "_blank");
        else
            NavigationManager.NavigateTo(PageRouteNames.ReportPurchaseReturnItem, true);
    }

    private async Task NavigateToKitchenIssueReport()
    {
        if (FormFactor.GetFormFactor() == "Web")
            await JSRuntime.InvokeVoidAsync("open", PageRouteNames.ReportKitchenIssue, "_blank");
        else
            NavigationManager.NavigateTo(PageRouteNames.ReportKitchenIssue, true);
    }

    private async Task NavigateToKitchenProductionReport()
    {
        if (FormFactor.GetFormFactor() == "Web")
            await JSRuntime.InvokeVoidAsync("open", PageRouteNames.ReportKitchenProduction, "_blank");
        else
            NavigationManager.NavigateTo(PageRouteNames.ReportKitchenProduction, true);
    }

    private async Task NavigateToKitchenIssueItemReport()
    {
        if (FormFactor.GetFormFactor() == "Web")
            await JSRuntime.InvokeVoidAsync("open", PageRouteNames.ReportKitchenIssueItem, "_blank");
        else
            NavigationManager.NavigateTo(PageRouteNames.ReportKitchenIssueItem, true);
    }

    private async Task NavigateToKitchenProductionItemReport()
    {
        if (FormFactor.GetFormFactor() == "Web")
            await JSRuntime.InvokeVoidAsync("open", PageRouteNames.ReportKitchenProductionItem, "_blank");
        else
            NavigationManager.NavigateTo(PageRouteNames.ReportKitchenProductionItem, true);
    }

    private async Task NavigateToRawMaterialStockReport()
    {
        if (FormFactor.GetFormFactor() == "Web")
            await JSRuntime.InvokeVoidAsync("open", PageRouteNames.ReportRawMaterialStock, "_blank");
        else
            NavigationManager.NavigateTo(PageRouteNames.ReportRawMaterialStock, true);
    }

    private async Task NavigateToProductStockReport()
    {
        if (FormFactor.GetFormFactor() == "Web")
            await JSRuntime.InvokeVoidAsync("open", PageRouteNames.ReportProductStock, "_blank");
        else
            NavigationManager.NavigateTo(PageRouteNames.ReportProductStock, true);
    }
    #endregion

    #region Accounting Reports Navigation
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

    private async Task NavigateToTrialBalanceReport()
    {
        if (FormFactor.GetFormFactor() == "Web")
            await JSRuntime.InvokeVoidAsync("open", PageRouteNames.ReportTrialBalance, "_blank");
        else
            NavigationManager.NavigateTo(PageRouteNames.ReportTrialBalance, true);
    }
    #endregion

    private async Task Logout() =>
        await AuthService.Logout(DataStorageService, NavigationManager, NotificationService, VibrationService);
}