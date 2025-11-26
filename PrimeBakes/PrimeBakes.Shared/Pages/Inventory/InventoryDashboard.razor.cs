using Microsoft.JSInterop;

using PrimeBakes.Shared.Services;

using PrimeBakesLibrary.Models.Common;

namespace PrimeBakes.Shared.Pages.Inventory;

public partial class InventoryDashboard
{
    private bool _isLoading = true;

    protected override async Task OnInitializedAsync()
    {
        _isLoading = true;
        await AuthService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, UserRoles.Inventory, true);
        _isLoading = false;
    }

    private async Task NavigateToPurchase()
    {
        if (FormFactor.GetFormFactor() == "Web")
            await JSRuntime.InvokeVoidAsync("open", PageRouteNames.Purchase, "_blank");
        else
            NavigationManager.NavigateTo(PageRouteNames.Purchase, true);
    }

    private async Task NavigateToPurchaseReturn()
    {
        if (FormFactor.GetFormFactor() == "Web")
            await JSRuntime.InvokeVoidAsync("open", PageRouteNames.PurchaseReturn, "_blank");
        else
            NavigationManager.NavigateTo(PageRouteNames.PurchaseReturn, true);
    }

    private async Task NavigateToKitchenIssue()
    {
        if (FormFactor.GetFormFactor() == "Web")
            await JSRuntime.InvokeVoidAsync("open", PageRouteNames.KitchenIssue, "_blank");
        else
            NavigationManager.NavigateTo(PageRouteNames.KitchenIssue, true);
    }

    private async Task NavigateToKitchenProduction()
    {
        if (FormFactor.GetFormFactor() == "Web")
            await JSRuntime.InvokeVoidAsync("open", PageRouteNames.KitchenProduction, "_blank");
        else
            NavigationManager.NavigateTo(PageRouteNames.KitchenProduction, true);
    }

    private async Task NavigateToRawMaterialAdjustment()
    {
        if (FormFactor.GetFormFactor() == "Web")
            await JSRuntime.InvokeVoidAsync("open", PageRouteNames.RawMaterialStockAdjustment, "_blank");
        else
            NavigationManager.NavigateTo(PageRouteNames.RawMaterialStockAdjustment, true);
    }

    private async Task NavigateToProductAdjustment()
    {
        if (FormFactor.GetFormFactor() == "Web")
            await JSRuntime.InvokeVoidAsync("open", PageRouteNames.ProductStockAdjustment, "_blank");
        else
            NavigationManager.NavigateTo(PageRouteNames.ProductStockAdjustment, true);
    }

    private async Task NavigateToRecipe()
    {
        if (FormFactor.GetFormFactor() == "Web")
            await JSRuntime.InvokeVoidAsync("open", PageRouteNames.Recipe, "_blank");
        else
            NavigationManager.NavigateTo(PageRouteNames.Recipe, true);
    }

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

    private async Task Logout() =>
        await AuthService.Logout(DataStorageService, NavigationManager, NotificationService, VibrationService);
}