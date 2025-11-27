using Microsoft.JSInterop;

using PrimeBakes.Shared.Services;

using PrimeBakesLibrary.Models.Common;

namespace PrimeBakes.Shared.Pages.Sales;

public partial class SalesDashboard
{
    private bool _isLoading = true;

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, UserRoles.Sales, true);
        _isLoading = false;
        StateHasChanged();
    }

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

	private async Task NavigateToProductAdjustment()
	{
		if (FormFactor.GetFormFactor() == "Web")
			await JSRuntime.InvokeVoidAsync("open", PageRouteNames.ProductStockAdjustment, "_blank");
		else
			NavigationManager.NavigateTo(PageRouteNames.ProductStockAdjustment, true);
	}
	
    private async Task NavigateToProduct()
    {
        if (FormFactor.GetFormFactor() == "Web")
            await JSRuntime.InvokeVoidAsync("open", PageRouteNames.AdminProduct, "_blank");
        else
            NavigationManager.NavigateTo(PageRouteNames.AdminProduct, true);
    }

    private async Task NavigateToProductLocation()
    {
        if (FormFactor.GetFormFactor() == "Web")
            await JSRuntime.InvokeVoidAsync("open", PageRouteNames.AdminProductLocation, "_blank");
        else
            NavigationManager.NavigateTo(PageRouteNames.AdminProductLocation, true);
    }

    private async Task NavigateToProductCategory()
    {
        if (FormFactor.GetFormFactor() == "Web")
            await JSRuntime.InvokeVoidAsync("open", PageRouteNames.AdminProductCategory, "_blank");
        else
            NavigationManager.NavigateTo(PageRouteNames.AdminProductCategory, true);
    }

    private async Task NavigateToTax()
    {
        if (FormFactor.GetFormFactor() == "Web")
            await JSRuntime.InvokeVoidAsync("open", PageRouteNames.AdminTax, "_blank");
        else
            NavigationManager.NavigateTo(PageRouteNames.AdminTax, true);
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

	private async Task NavigateToProductStockReport()
	{
		if (FormFactor.GetFormFactor() == "Web")
			await JSRuntime.InvokeVoidAsync("open", PageRouteNames.ReportProductStock, "_blank");
		else
			NavigationManager.NavigateTo(PageRouteNames.ReportProductStock, true);
	}

	private async Task Logout() =>
        await AuthenticationService.Logout(DataStorageService, NavigationManager, NotificationService, VibrationService);
}