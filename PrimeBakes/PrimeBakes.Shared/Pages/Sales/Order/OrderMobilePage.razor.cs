using Microsoft.AspNetCore.Components;

using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Sales.Product;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Sales.Order;
using PrimeBakesLibrary.Models.Sales.Product;

using Syncfusion.Blazor.Grids;

namespace PrimeBakes.Shared.Pages.Sales.Order;

public partial class OrderMobilePage
{
    private UserModel _user;

    private bool _isLoading = true;
    private bool _isProcessing = false;

    private ProductCategoryModel _selectedCategory;

    private List<ProductCategoryModel> _productCategories = [];
    private List<OrderItemCartModel> _cart = [];

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
        await LoadItems();
        await LoadExistingCart();
        await SaveOrderFile();

        if (_sfCartGrid is not null)
            await _sfCartGrid?.Refresh();
    }

    private async Task LoadItems()
    {
        try
        {
            _productCategories = await CommonData.LoadTableDataByStatus<ProductCategoryModel>(TableNames.ProductCategory);
            _productCategories.Add(new()
            {
                Id = 0,
                Name = "All Categories"
            });
            _productCategories = [.. _productCategories.OrderBy(s => s.Name)];
            _selectedCategory = _productCategories.FirstOrDefault(s => s.Id == 0);

            var mainLocationProducts = await ProductData.LoadProductByLocation(1);
            var orderLocationProducts = await ProductData.LoadProductByLocation(_user.LocationId);
            var allProducts = mainLocationProducts.Where(x => orderLocationProducts.Any(y => y.ProductId == x.ProductId)).ToList();
            foreach (var product in allProducts)
                _cart.Add(new()
                {
                    ItemCategoryId = product.ProductCategoryId,
                    ItemId = product.ProductId,
                    ItemName = product.Name,
                    Remarks = null,
                    Quantity = 0
                });
            _cart = [.. _cart.OrderBy(s => s.ItemName)];
        }
        catch (Exception ex)
        {
            await _toastNotification.ShowAsync("An Error Occurred While Loading Items", ex.Message, ToastType.Error);
        }
    }

    private async Task LoadExistingCart()
    {
        try
        {
            if (await DataStorageService.LocalExists(StorageFileNames.OrderMobileCartDataFileName))
            {
                var existingCart = System.Text.Json.JsonSerializer.Deserialize<List<OrderItemCartModel>>(await DataStorageService.LocalGetAsync(StorageFileNames.OrderMobileCartDataFileName));
                foreach (var item in existingCart)
                    _cart.Where(p => p.ItemId == item.ItemId).FirstOrDefault().Quantity = item.Quantity;
            }
        }
        catch (Exception ex)
        {
            await _toastNotification.ShowAsync("An Error Occurred While Loading Existing Cart", ex.Message, ToastType.Error);
            await DeleteLocalFiles();
        }
        finally
        {
            await SaveOrderFile();
        }
    }
    #endregion

    #region Cart
    private async Task AddToCart(OrderItemCartModel item)
    {
        if (item is null)
            return;

        item.Quantity = 1;
        await SaveOrderFile();
    }

    private async Task UpdateQuantity(OrderItemCartModel item, decimal newQuantity)
    {
        if (item is null)
            return;

        item.Quantity = Math.Max(0, newQuantity);
        await SaveOrderFile();
    }

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

    private async Task GoToCart()
    {
        await SaveOrderFile();

        if (_cart.Sum(x => x.Quantity) <= 0 || await DataStorageService.LocalExists(StorageFileNames.OrderMobileCartDataFileName) == false)
            return;

        VibrationService.VibrateWithTime(500);
        _cart.Clear();

        NavigationManager.NavigateTo(PageRouteNames.OrderMobileCart);
    }
    #endregion

    #region Utilities
    private async Task DeleteLocalFiles()
    {
        await DataStorageService.LocalRemove(StorageFileNames.OrderMobileCartDataFileName);
        await DataStorageService.LocalRemove(StorageFileNames.OrderMobileDataFileName);
    }
    #endregion
}