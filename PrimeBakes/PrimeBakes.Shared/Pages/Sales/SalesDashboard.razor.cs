using Microsoft.JSInterop;

using PrimeBakes.Shared.Services;

using PrimeBakesLibrary.Models.Common;

namespace PrimeBakes.Shared.Pages.Sales;

public partial class SalesDashboard
{
    private bool _isLoading = true;

    protected override async Task OnInitializedAsync()
    {
        _isLoading = true;
        await AuthService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, UserRoles.Sales, true);
        _isLoading = false;
    }

    #region Sales Operations Navigation
    private async Task NavigateToOrder()
    {
        if (FormFactor.GetFormFactor() == "Web")
            await JSRuntime.InvokeVoidAsync("open", PageRouteNames.Order, "_blank");
        else
            NavigationManager.NavigateTo(PageRouteNames.Order, true);
    }

    private async Task NavigateToSale()
    {
        if (FormFactor.GetFormFactor() == "Web")
            await JSRuntime.InvokeVoidAsync("open", PageRouteNames.Sale, "_blank");
        else
            NavigationManager.NavigateTo(PageRouteNames.Sale, true);
    }

    private async Task NavigateToSaleReturn()
    {
        if (FormFactor.GetFormFactor() == "Web")
            await JSRuntime.InvokeVoidAsync("open", PageRouteNames.SaleReturn, "_blank");
        else
            NavigationManager.NavigateTo(PageRouteNames.SaleReturn, true);
    }
    #endregion

    #region Sales Reports Navigation
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

    private async Task NavigateToSaleReport()
    {
        if (FormFactor.GetFormFactor() == "Web")
            await JSRuntime.InvokeVoidAsync("open", PageRouteNames.ReportSale, "_blank");
        else
            NavigationManager.NavigateTo(PageRouteNames.ReportSale, true);
    }

    private async Task NavigateToSaleItemReport()
    {
        if (FormFactor.GetFormFactor() == "Web")
            await JSRuntime.InvokeVoidAsync("open", PageRouteNames.ReportSaleItem, "_blank");
        else
            NavigationManager.NavigateTo(PageRouteNames.ReportSaleItem, true);
    }

    private async Task NavigateToSaleReturnReport()
    {
        if (FormFactor.GetFormFactor() == "Web")
            await JSRuntime.InvokeVoidAsync("open", PageRouteNames.ReportSaleReturn, "_blank");
        else
            NavigationManager.NavigateTo(PageRouteNames.ReportSaleReturn, true);
    }

    private async Task NavigateToSaleReturnItemReport()
    {
        if (FormFactor.GetFormFactor() == "Web")
            await JSRuntime.InvokeVoidAsync("open", PageRouteNames.ReportSaleReturnItem, "_blank");
        else
            NavigationManager.NavigateTo(PageRouteNames.ReportSaleReturnItem, true);
    }
    #endregion

    private async Task Logout() =>
        await AuthService.Logout(DataStorageService, NavigationManager, NotificationService, VibrationService);
}