using System.Reflection;

using Microsoft.JSInterop;

using PrimeBakes.Shared.Services;

using PrimeBakesLibrary.Models.Common;

namespace PrimeBakes.Shared.Pages;

public partial class Dashboard : IDisposable
{
    private UserModel _user;
    private bool _isLoading = true;
    private bool _isUpdating = false;
    private int _updateProgress = 0;
    private int _timeRemaining = 0;
    private string _updateStatus = "Preparing update...";
    private System.Timers.Timer _progressTimer;
    private DateTime _updateStartTime;

    #region Device Info
    private string Factor =>
        FormFactor.GetFormFactor();

    private string Platform =>
        FormFactor.GetPlatform();

    private static string AppVersion =>
        Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0.0";
    #endregion

    #region Updating
    private async Task StartUpdateProcess()
    {
        _isUpdating = true;
        _updateProgress = 0;
        _timeRemaining = 0;
        _updateStartTime = DateTime.Now;
        StateHasChanged();

        // Create a progress reporter
        var progress = new Progress<int>(percent =>
        {
            _updateProgress = percent;
            _updateStatus = percent switch
            {
                < 10 => "Preparing update...",
                < 30 => "Downloading update...",
                < 60 => "Installing update...",
                < 90 => "Finalizing installation...",
                _ => "Almost done..."
            };

            // Calculate estimated time remaining
            if (percent > 0)
            {
                var elapsed = (DateTime.Now - _updateStartTime).TotalSeconds;
                var estimatedTotal = elapsed / percent * 100;
                _timeRemaining = Math.Max(0, (int)(estimatedTotal - elapsed));
            }

            InvokeAsync(StateHasChanged);
        });

        // await UpdateService.UpdateAppAsync("aadipoddar", "PrimeOrders", "com.aadisoft.primebakes", progress);

        _isUpdating = false;
        StateHasChanged();
    }

    public void Dispose() =>
        _progressTimer?.Dispose();
    #endregion

    #region Load Data
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
            return;

        try
        {
            if (Factor == "Phone" && Platform.Contains("Android"))
            {
                var hasUpdate = await UpdateService.CheckForUpdatesAsync("aadipoddar", "PrimeOrders", AppVersion);
                if (hasUpdate)
                    await StartUpdateProcess();
            }

            await LoadData();
        }
        catch (Exception)
        {
            await Logout();
        }
        finally
        {
            _isLoading = false;
            StateHasChanged();
        }
    }

    private async Task LoadData()
    {
        _user = await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService);

        if (Factor == "Phone" && Platform.Contains("Android"))
            await NotificationService.RegisterDevicePushNotification(_user.Id.ToString());
    }
    #endregion

    #region Navigation
    private async Task Logout() =>
        await AuthenticationService.Logout(DataStorageService, NavigationManager, NotificationService, VibrationService);

    private async Task NavigateToSales()
    {
        if (FormFactor.GetFormFactor() == "Web")
            await JSRuntime.InvokeVoidAsync("open", PageRouteNames.SalesDashboard, "_blank");
        else
            NavigationManager.NavigateTo(PageRouteNames.SalesDashboard, true);
    }

    private async Task NavigateToInventory()
    {
        if (FormFactor.GetFormFactor() == "Web")
            await JSRuntime.InvokeVoidAsync("open", PageRouteNames.InventoryDashboard, "_blank");
        else
            NavigationManager.NavigateTo(PageRouteNames.InventoryDashboard, true);
    }

    private async Task NavigateToAccounts()
    {
        if (FormFactor.GetFormFactor() == "Web")
            await JSRuntime.InvokeVoidAsync("open", PageRouteNames.AccountsDashboard, "_blank");
        else
            NavigationManager.NavigateTo(PageRouteNames.AccountsDashboard, true);
    }

    private async Task NavigateToAdmin()
    {
        if (FormFactor.GetFormFactor() == "Web")
            await JSRuntime.InvokeVoidAsync("open", PageRouteNames.AdminDashboard, "_blank");
        else
            NavigationManager.NavigateTo(PageRouteNames.AdminDashboard, true);
    }
    #endregion
}