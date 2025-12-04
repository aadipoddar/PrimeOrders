using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Inventory;
using PrimeBakesLibrary.Data.Sales.Product;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Inventory;
using PrimeBakesLibrary.Models.Sales.Product;

using Syncfusion.Blazor.DropDowns;
using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Notifications;

namespace PrimeBakes.Shared.Pages.Inventory;

public partial class RecipePage
{
    private bool _isLoading = true;
    private bool _isProcessing = false;

    private decimal _selectedRawMaterialQuantity;

    private ProductLocationOverviewModel _selectedProduct;
    private RawMaterialModel _selectedRawMaterial;
    private RecipeModel _recipe;

    private List<ProductLocationOverviewModel> _products = [];
    private List<RawMaterialModel> _rawMaterials = [];
    private readonly List<RecipeItemCartModel> _recipeItems = [];

    private SfAutoComplete<RawMaterialModel?, RawMaterialModel> _sfItemAutoComplete;
    private SfGrid<RecipeItemCartModel> _sfCartGrid;

    private string _errorTitle = string.Empty;
    private string _errorMessage = string.Empty;

    private string _successTitle = string.Empty;
    private string _successMessage = string.Empty;

    private SfToast _sfSuccessToast;
    private SfToast _sfErrorToast;

    #region Load Data
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
            return;

        await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, UserRoles.Inventory, true);
        await LoadData();
        _isLoading = false;
        StateHasChanged();
    }

    private async Task LoadData()
    {
        try
        {
            _products = await ProductData.LoadProductByLocation(1);
            _rawMaterials = await CommonData.LoadTableData<RawMaterialModel>(TableNames.RawMaterial);
            _selectedProduct = _products.FirstOrDefault();
        }
        catch (Exception ex)
        {
            await ShowToast("An Error Occurred While Loading Data", ex.Message, "error");
        }
    }

    private async Task OnProductChanged(ChangeEventArgs<ProductLocationOverviewModel, ProductLocationOverviewModel> args)
    {
        try
        {
            if (args is null)
                _selectedProduct = _products.FirstOrDefault();
            else
                _selectedProduct = args.Value;

            await LoadRecipe();
            StateHasChanged();
        }
        catch (Exception ex)
        {
            _errorMessage = $"Error changing product: {ex.Message}";
            await _sfErrorToast?.ShowAsync();
        }
    }

    private async Task LoadRecipe()
    {
        if (_isProcessing || _selectedProduct is null || _selectedProduct.ProductId == 0 || _isLoading)
            return;

        try
        {
            _isProcessing = true;

            _recipeItems.Clear();
            if (_sfCartGrid is not null)
                await _sfCartGrid?.Refresh();

            _recipe = await RecipeData.LoadRecipeByProduct(_selectedProduct.ProductId);
            if (_recipe is null)
                return;

            var recipeDetails = await RecipeData.LoadRecipeDetailByRecipe(_recipe.Id);
            if (recipeDetails is null || recipeDetails.Count == 0)
                return;

            _recipeItems.Clear();

            foreach (var detail in recipeDetails)
            {
                var rawMaterial = await CommonData.LoadTableDataById<RawMaterialModel>(TableNames.RawMaterial, detail.RawMaterialId);

                _recipeItems.Add(new()
                {
                    ItemId = detail.RawMaterialId,
                    ItemName = rawMaterial.Name,
                    Quantity = detail.Quantity
                });
            }

            await ShowToast("Recipe Loaded", $"Recipe loaded for {_selectedProduct.Name} with {_recipeItems.Count} items", "success");
        }
        catch (Exception ex)
        {
            await ShowToast("Error Loading Recipe", ex.Message, "error");
        }
        finally
        {
            if (_sfCartGrid is not null)
                await _sfCartGrid?.Refresh();

            _isProcessing = false;
            StateHasChanged();
        }
    }
    #endregion

    #region Cart
    private async Task AddItemToCart()
    {
        if (_selectedRawMaterial is null || _selectedRawMaterial.Id == 0 || _selectedRawMaterialQuantity <= 0)
            return;

        var existingRecipe = _recipeItems.FirstOrDefault(r => r.ItemId == _selectedRawMaterial.Id);
        if (existingRecipe is not null)
            existingRecipe.Quantity += _selectedRawMaterialQuantity;
        else
            _recipeItems.Add(new()
            {
                ItemId = _selectedRawMaterial.Id,
                ItemName = _selectedRawMaterial.Name,
                Quantity = _selectedRawMaterialQuantity,
            });

        _selectedRawMaterial = null;
        _selectedRawMaterialQuantity = 0;

        _sfItemAutoComplete?.FocusAsync();

        if (_sfCartGrid is not null)
            await _sfCartGrid?.Refresh();

        StateHasChanged();
    }

    private async Task EditCartItem(RecipeItemCartModel cartItem)
    {
        _selectedRawMaterial = _rawMaterials.FirstOrDefault(r => r.Id == cartItem.ItemId);
        _selectedRawMaterialQuantity = cartItem.Quantity;
        _recipeItems.Remove(cartItem);

        _sfItemAutoComplete?.FocusAsync();

        if (_sfCartGrid is not null)
            await _sfCartGrid?.Refresh();

        StateHasChanged();
    }

    private async Task RemoveItemFromCart(RecipeItemCartModel cartItem)
    {
        _recipeItems.Remove(cartItem);

        if (_sfCartGrid is not null)
            await _sfCartGrid?.Refresh();

        StateHasChanged();
    }
    #endregion

    #region Saving
    private async Task DeleteRecipe()
    {
        if (_isProcessing || _recipe is null || _recipe.Id == 0 || _isLoading)
            return;

        try
        {
            _recipe.Status = false;
            await RecipeData.InsertRecipe(_recipe);
            NavigationManager.NavigateTo(PageRouteNames.Recipe, true);
        }
        catch (Exception ex)
        {
            await ShowToast("Error Deleting Recipe", ex.Message, "error");
        }
        finally
        {
            if (_sfCartGrid is not null)
                await _sfCartGrid?.Refresh();

            _isProcessing = false;
            StateHasChanged();
        }
    }

    private async Task OnSaveButtonClick()
    {
        if (_isProcessing)
            return;

        try
        {
            _isProcessing = true;
            StateHasChanged();

            if (_selectedProduct is null || _selectedProduct.Id == 0)
            {
                await ShowToast("No Product Selected", "Please select a product to save the recipe for", "error");
                return;
            }

            if (_recipeItems.Count == 0)
            {
                await ShowToast("No Raw Materials Added", "Please add at least one raw material to the recipe", "error");
                return;
            }

            await ShowToast("Processing Transaction", "Please wait while the transaction is being saved...", "success");

            await RecipeData.SaveRecipe(new()
            {
                Id = _recipe?.Id ?? 0,
                ProductId = _selectedProduct.ProductId,
                Status = true,
            }, _recipeItems);

            await ShowToast("Recipe Saved", $"Recipe saved successfully for {_selectedProduct.Name} with {_recipeItems.Count} items!", "success");
            NavigationManager.NavigateTo(PageRouteNames.Recipe, true);
        }
        catch (Exception ex)
        {
            await ShowToast("Error Saving Recipe", ex.Message, "error");
        }
        finally
        {
            if (_sfCartGrid is not null)
                await _sfCartGrid?.Refresh();

            _isProcessing = false;
            StateHasChanged();
        }
    }
    #endregion

    #region Utilities
    private async Task ResetPage(Microsoft.AspNetCore.Components.Web.MouseEventArgs args) =>
        NavigationManager.NavigateTo(PageRouteNames.Recipe, true);

    private async Task ShowToast(string title, string message, string type)
    {
        VibrationService.VibrateWithTime(200);

        if (type == "error")
        {
            _errorTitle = title;
            _errorMessage = message;
            await _sfErrorToast.ShowAsync(new()
            {
                Title = _errorTitle,
                Content = _errorMessage
            });
        }

        else if (type == "success")
        {
            _successTitle = title;
            _successMessage = message;
            await _sfSuccessToast.ShowAsync(new()
            {
                Title = _successTitle,
                Content = _successMessage
            });
        }
    }
    #endregion
}