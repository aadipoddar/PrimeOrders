using Microsoft.JSInterop;

using PrimeBakes.Shared.Services;

using PrimeBakesLibrary.Models.Common;

namespace PrimeBakes.Shared.Pages.Admin;

public partial class AdminDashboard
{
    private bool _isLoading = true;

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, UserRoles.Admin, true);
        _isLoading = false;
		StateHasChanged();
	}

    #region Raw Materials Navigation
    private async Task NavigateToRawMaterial()
    {
        if (FormFactor.GetFormFactor() == "Web")
            await JSRuntime.InvokeVoidAsync("open", PageRouteNames.AdminRawMaterial, "_blank");
        else
            NavigationManager.NavigateTo(PageRouteNames.AdminRawMaterial, true);
    }

    private async Task NavigateToRawMaterialCategory()
    {
        if (FormFactor.GetFormFactor() == "Web")
            await JSRuntime.InvokeVoidAsync("open", PageRouteNames.AdminRawMaterialCategory, "_blank");
        else
            NavigationManager.NavigateTo(PageRouteNames.AdminRawMaterialCategory, true);
    }
    #endregion

    #region Products Navigation
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
    #endregion

    #region Operations Navigation
    private async Task NavigateToLocation()
    {
        if (FormFactor.GetFormFactor() == "Web")
            await JSRuntime.InvokeVoidAsync("open", PageRouteNames.AdminLocation, "_blank");
        else
            NavigationManager.NavigateTo(PageRouteNames.AdminLocation, true);
    }

    private async Task NavigateToUser()
    {
        if (FormFactor.GetFormFactor() == "Web")
            await JSRuntime.InvokeVoidAsync("open", PageRouteNames.AdminUser, "_blank");
        else
            NavigationManager.NavigateTo(PageRouteNames.AdminUser, true);
    }

    private async Task NavigateToSettings()
    {
        if (FormFactor.GetFormFactor() == "Web")
            await JSRuntime.InvokeVoidAsync("open", PageRouteNames.AdminSettings, "_blank");
        else
            NavigationManager.NavigateTo(PageRouteNames.AdminSettings, true);
    }

    private async Task NavigateToKitchen()
    {
        if (FormFactor.GetFormFactor() == "Web")
            await JSRuntime.InvokeVoidAsync("open", PageRouteNames.AdminKitchen, "_blank");
        else
            NavigationManager.NavigateTo(PageRouteNames.AdminKitchen, true);
    }

    private async Task NavigateToTax()
    {
        if (FormFactor.GetFormFactor() == "Web")
            await JSRuntime.InvokeVoidAsync("open", PageRouteNames.AdminTax, "_blank");
        else
            NavigationManager.NavigateTo(PageRouteNames.AdminTax, true);
    }
    #endregion

    #region Accounting Master Data Navigation
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
        await AuthenticationService.Logout(DataStorageService, NavigationManager, NotificationService, VibrationService);
}