using PrimeBakes.Shared.Services;

using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Inventory;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Inventory;

using Syncfusion.Blazor.DropDowns;
using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Popups;

namespace PrimeBakes.Shared.Pages.Inventory.Stock;

public partial class RawMaterialStockAdjustmentPage
{
    private UserModel _user;
    private bool _isLoading = true;
    private bool _isSaving = false;

    private int _selectedCategoryId = 0;
    private bool _adjustmentConfirmationDialogVisible = false;
    private bool _validationErrorDialogVisible = false;

    private List<RawMaterialCategoryModel> _rawMaterialCategories = [];
    private readonly List<StockAdjustmentRawMaterialCartModel> _cart = [];
    private readonly List<ValidationError> _validationErrors = [];
    private readonly HashSet<int> _selectedRawMaterialIds = [];

    public class ValidationError
    {
        public string Field { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    private SfGrid<StockAdjustmentRawMaterialCartModel> _sfGrid;

    private SfDialog _sfAdjustmentConfirmationDialog;
    private SfDialog _sfValidationErrorDialog;

    #region Load Data
    protected override async Task OnInitializedAsync()
    {
        var authResult = await AuthService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, UserRoles.Inventory, true);
        _user = authResult.User;
        await LoadData();
        _isLoading = false;
    }

    private async Task LoadData()
    {
        _rawMaterialCategories = await CommonData.LoadTableDataByStatus<RawMaterialCategoryModel>(TableNames.RawMaterialCategory);
        _rawMaterialCategories.Add(new()
        {
            Id = 0,
            Name = "All Categories"
        });
        _rawMaterialCategories.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.Ordinal));
        _selectedCategoryId = 0;

        var allProducts = await CommonData.LoadTableDataByStatus<RawMaterialModel>(TableNames.RawMaterial);

        foreach (var product in allProducts)
            _cart.Add(new()
            {
                RawMaterialCategoryId = product.RawMaterialCategoryId,
                RawMaterialId = product.Id,
                RawMaterialName = product.Name,
                Quantity = 0,
                Rate = product.MRP,
                Total = 0
            }); _cart.Sort((x, y) => string.Compare(x.RawMaterialName, y.RawMaterialName, StringComparison.Ordinal));

        if (await DataStorageService.LocalExists(StorageFileNames.RawMaterialStockAdjustmentCartDataFileName))
        {
            var items = System.Text.Json.JsonSerializer.Deserialize<List<StockAdjustmentRawMaterialCartModel>>(await DataStorageService.LocalGetAsync(StorageFileNames.RawMaterialStockAdjustmentCartDataFileName)) ?? [];
            foreach (var item in items)
            {
                var cartItem = _cart.Where(p => p.RawMaterialId == item.RawMaterialId).FirstOrDefault();
                if (cartItem != null)
                {
                    cartItem.Quantity = item.Quantity;
                    _selectedRawMaterialIds.Add(item.RawMaterialId); // Mark as selected
                }
            }
        }

        if (_sfGrid is not null)
            await _sfGrid.Refresh();

        StateHasChanged();
    }
    #endregion

    #region Raw Material
    private async Task OnRawMaterialCategoryChanged(ChangeEventArgs<int, RawMaterialCategoryModel> args)
    {
        if (args is null || args.Value <= 0)
            _selectedCategoryId = 0;
        else
            _selectedCategoryId = args.Value;

        await _sfGrid.Refresh();
        StateHasChanged();
    }

    private async Task AddToCart(StockAdjustmentRawMaterialCartModel item)
    {
        if (item is null)
            return;

        item.Quantity = 1;
        _selectedRawMaterialIds.Add(item.RawMaterialId);
        await SaveAdjustmentFile();
    }

    private async Task UpdateQuantity(StockAdjustmentRawMaterialCartModel item, decimal newQuantity)
    {
        if (item is null)
            return;

        item.Quantity = newQuantity; // Allow negative quantities for stock reductions
        _selectedRawMaterialIds.Add(item.RawMaterialId); // Mark as selected when quantity is modified
        await SaveAdjustmentFile();
    }

    private async Task RemoveFromCart(StockAdjustmentRawMaterialCartModel item)
    {
        if (item is null)
            return;

        item.Quantity = 0;
        _selectedRawMaterialIds.Remove(item.RawMaterialId);
        await SaveAdjustmentFile();
    }
    #endregion

    #region Dialog Methods
    private void CloseConfirmationDialog()
    {
        _adjustmentConfirmationDialogVisible = false;
        StateHasChanged();
    }
    #endregion

    #region Saving
    private async Task SaveAdjustmentFile()
    {
        if (_selectedRawMaterialIds.Count == 0 && await DataStorageService.LocalExists(StorageFileNames.RawMaterialStockAdjustmentCartDataFileName))
            await DataStorageService.LocalRemove(StorageFileNames.RawMaterialStockAdjustmentCartDataFileName);
        else
            await DataStorageService.LocalSaveAsync(StorageFileNames.RawMaterialStockAdjustmentCartDataFileName, System.Text.Json.JsonSerializer.Serialize(_cart.Where(_ => _selectedRawMaterialIds.Contains(_.RawMaterialId))));

        VibrationService.VibrateHapticClick();
        await _sfGrid.Refresh();
        StateHasChanged();
    }

    private bool ValidateForm()
    {
        _validationErrors.Clear();

        var itemsToAdjust = _cart.Where(x => _selectedRawMaterialIds.Contains(x.RawMaterialId)).ToList();

        if (itemsToAdjust.Count == 0)
        {
            _validationErrors.Add(new()
            {
                Field = "Adjustment Items",
                Message = "Please select at least one raw material for adjustment."
            });
        }

        // Check for extremely negative quantities that might be unintentional
        var extremeNegativeItems = itemsToAdjust.Where(x => x.Quantity < -1000).ToList();
        if (extremeNegativeItems.Count != 0)
        {
            _validationErrors.Add(new()
            {
                Field = "Large Negative Quantities",
                Message = $"Some items have very large negative quantities. Please verify: {string.Join(", ", extremeNegativeItems.Select(x => $"{x.RawMaterialName} ({x.Quantity})"))}"
            });
        }

        return _validationErrors.Count == 0;
    }

    private async Task ConfirmAdjustment()
    {
        if (_isSaving)
            return;

        _isSaving = true;
        StateHasChanged();

        try
        {
            await SaveAdjustmentFile();

            if (!ValidateForm())
            {
                _validationErrorDialogVisible = true;
                _adjustmentConfirmationDialogVisible = false;
                return;
            }

            await StockData.SaveRawMaterialStockAdjustment([.. _cart.Where(x => _selectedRawMaterialIds.Contains(x.RawMaterialId))]);
            await DeleteCart();
            await SendLocalNotification();
            NavigationManager.NavigateTo("/Inventory/RawMaterialStockAdjustment/Confirmed", true);
        }
        catch (Exception ex)
        {
            _validationErrors.Add(new()
            {
                Field = "System Error",
                Message = $"An error occurred while saving the adjustment: {ex.Message}"
            });
            _validationErrorDialogVisible = true;
            _adjustmentConfirmationDialogVisible = false;
        }
        finally
        {
            _isSaving = false;
            StateHasChanged();
        }
    }

    private async Task DeleteCart()
    {
        _cart.Clear();
        await DataStorageService.LocalRemove(StorageFileNames.RawMaterialStockAdjustmentCartDataFileName);
    }

    private async Task SendLocalNotification()
    {
        await NotificationService.ShowLocalNotification(
            100,
             "Raw Material Stock Adjustment Saved",
             "Stock Adjusted.",
               $"Raw Material Stock Adjustment has been successfully saved on {DateTime.Now:dd/MM/yy hh:mm tt}. Please check the Stock Adjustment report for details.");
    }
    #endregion
}