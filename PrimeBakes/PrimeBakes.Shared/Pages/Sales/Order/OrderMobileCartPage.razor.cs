using Microsoft.AspNetCore.Components;

using PrimeBakesLibrary.Data;
using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Sales.Order;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Sales.Order;

using Syncfusion.Blazor.Grids;

namespace PrimeBakes.Shared.Pages.Sales.Order;

public partial class OrderMobileCartPage
{
    private UserModel _user;

    private bool _isLoading = true;
    private bool _isProcessing = false;
    private bool _showConfirmDialog = false;
    private bool _showValidationDialog = false;

    private string _orderRemarks = string.Empty;

    private List<OrderItemCartModel> _cart = [];
    private List<(string Field, string Message)> _validationErrors = [];

    private SfGrid<OrderItemCartModel> _sfCartGrid;

    private ToastNotification _toastNotification;

	#region Load Data
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		_user = await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, UserRoles.Order);
        await LoadData();
        _isLoading = false;
        StateHasChanged();
	}

    private async Task LoadData()
    {
        _cart.Clear();

        if (await DataStorageService.LocalExists(StorageFileNames.OrderMobileCartDataFileName))
        {
            var items = System.Text.Json.JsonSerializer.Deserialize<List<OrderItemCartModel>>(await DataStorageService.LocalGetAsync(StorageFileNames.OrderMobileCartDataFileName)) ?? [];
            foreach (var item in items)
                _cart.Add(item);
        }

        _cart = [.. _cart.OrderBy(x => x.ItemName)];

        if (_sfCartGrid is not null)
            await _sfCartGrid.Refresh();

        StateHasChanged();
    }
    #endregion

    #region Products
    private async Task UpdateQuantity(OrderItemCartModel item, decimal newQuantity)
    {
        if (item is null || _isProcessing)
            return;

        item.Quantity = Math.Max(0, newQuantity);

        if (item.Quantity == 0)
            _cart.Remove(item);

        await SaveOrderFile();
    }
    #endregion

    #region Saving
    private async Task SaveOrderFile()
    {
        if (_isProcessing || _isLoading)
            return;

        try
        {
            _isProcessing = true;

            if (!_cart.Any(x => x.Quantity > 0) && await DataStorageService.LocalExists(StorageFileNames.OrderMobileCartDataFileName))
                await DataStorageService.LocalRemove(StorageFileNames.OrderMobileCartDataFileName);
            else
                await DataStorageService.LocalSaveAsync(StorageFileNames.OrderMobileCartDataFileName, System.Text.Json.JsonSerializer.Serialize(_cart.Where(_ => _.Quantity > 0)));

            VibrationService.VibrateHapticClick();
        }
        catch (Exception ex)
        {
            await _toastNotification.ShowAsync("An Error Occurred While Saving Cart Data", ex.Message, ToastType.Error);
        }
        finally
        {
            if (_sfCartGrid is not null)
                await _sfCartGrid?.Refresh();

            _isProcessing = false;
            StateHasChanged();
        }
    }

    private bool ValidateForm()
    {
        _validationErrors.Clear();

        if (_cart.Count == 0)
            _validationErrors.Add(("Cart", "The Cart is Empty. Please Add Items to the Cart Before Saving the Order."));

        if (_cart.Any(item => item.Quantity <= 0))
            _validationErrors.Add(("Quantity", "All items in the cart must have a quantity greater than zero."));

        if (_user.LocationId <= 1)
            _validationErrors.Add(("Location", "Please select a valid location for the order."));

        if (_validationErrors.Count != 0)
        {
            _showValidationDialog = true;
            StateHasChanged();
            return false;
        }

        return true;
    }

    private async Task SaveOrder()
    {
        if (_isProcessing || _isLoading)
            return;

        try
        {
            _isProcessing = true;
            StateHasChanged();
            await _toastNotification.ShowAsync("Processing", "Saving transaction...", ToastType.Info);

            await SaveOrderFile();

            if (!ValidateForm())
            {
                _isProcessing = false;
                return;
            }

            var order = new OrderModel
            {
                Id = 0,
                LocationId = _user.LocationId,
                CompanyId = int.Parse((await SettingsData.LoadSettingsByKey(SettingsKeys.PrimaryCompanyLinkingId)).Value),
                TransactionDateTime = await CommonData.LoadCurrentDateTime(),
                FinancialYearId = (await FinancialYearData.LoadFinancialYearByDateTime(await CommonData.LoadCurrentDateTime())).Id,
                CreatedAt = await CommonData.LoadCurrentDateTime(),
                CreatedBy = _user.Id,
                TotalItems = _cart.Count,
                TotalQuantity = _cart.Sum(x => x.Quantity),
				CreatedFromPlatform = FormFactor.GetFormFactor() + FormFactor.GetPlatform(),
                Remarks = string.IsNullOrWhiteSpace(_orderRemarks.Trim()) ? null : _orderRemarks,
                SaleId = null,
                Status = true,
            };

            order.TransactionNo = await GenerateCodes.GenerateOrderTransactionNo(order);

            order.Id = await OrderData.SaveOrderTransaction(order, _cart);
            await DeleteLocalFiles();
            var (pdfStream, fileName) = await OrderData.GenerateAndDownloadInvoice(order.Id);
            await SaveAndViewService.SaveAndView(fileName, pdfStream);
            await DeleteLocalFiles();
            NavigationManager.NavigateTo(PageRouteNames.OrderMobileConfirmation, true);
        }
        catch (Exception ex)
        {
            await _toastNotification.ShowAsync("An Error Occurred While Saving Order", ex.Message, ToastType.Error);
        }
        finally
        {
            _isProcessing = false;
        }
    }
    #endregion

    #region Utilities
    private async Task DeleteLocalFiles()
    {
        await DataStorageService.LocalRemove(StorageFileNames.OrderMobileCartDataFileName);
        await DataStorageService.LocalRemove(StorageFileNames.OrderMobileDataFileName);
    }

    private void OpenConfirmDialog()
    {
        if (!ValidateForm())
            return;

        _showConfirmDialog = true;
        StateHasChanged();
    }

    private void CloseConfirmDialog()
    {
        _showConfirmDialog = false;
        StateHasChanged();
    }

    private void CloseValidationDialog()
    {
        _showValidationDialog = false;
        StateHasChanged();
    }

    private async Task ConfirmOrder()
    {
        _showConfirmDialog = false;
        await SaveOrder();
    }
    #endregion
}