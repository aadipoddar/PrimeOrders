
using PrimeBakes.Shared.Services;

using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Inventory;
using PrimeBakesLibrary.Data.Product;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Inventory;
using PrimeBakesLibrary.Models.Product;

using Syncfusion.Blazor.DropDowns;
using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Notifications;

namespace PrimeBakes.Shared.Pages.Inventory;

public partial class RecipePage
{
	private bool _isLoading = true;

	private decimal _selectedRawMaterialQuantity;

	private ProductLocationOverviewModel _selectedProduct;
	private RawMaterialModel _selectedRawMaterial;
	private RecipeModel _recipe;

	private List<ProductLocationOverviewModel> _products = [];
	private List<RawMaterialModel> _rawMaterials = [];
	private readonly List<ItemRecipeModel> _recipeItems = [];

	private SfGrid<ItemRecipeModel> _sfGrid;
	private SfToast _sfToast;
	private SfToast _sfErrorToast;
	private SfToast _sfUpdateToast;

	private string _successMessage = string.Empty;
	private string _errorMessage = string.Empty;
	private string _infoMessage = string.Empty;

	#region Load Data
	protected override async Task OnInitializedAsync()
	{
		try
		{
			await AuthService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, UserRoles.Inventory, true);

			_products = await ProductData.LoadProductByLocation(1);
			_rawMaterials = await CommonData.LoadTableData<RawMaterialModel>(TableNames.RawMaterial);

			_selectedProduct = _products.FirstOrDefault();

			_isLoading = false;
		}
		catch (Exception ex)
		{
			_isLoading = false;
			_errorMessage = $"Error loading data: {ex.Message}";
			await _sfErrorToast?.ShowAsync();
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
		try
		{
			if (_sfGrid is null || _selectedProduct is null || _selectedProduct.ProductId == 0)
				return;

			_recipeItems.Clear();
			await _sfGrid?.Refresh();

			_recipe = await RecipeData.LoadRecipeByProduct(_selectedProduct.ProductId);
			if (_recipe is null)
			{
				StateHasChanged();
				return;
			}

			var recipeDetails = await RecipeData.LoadRecipeDetailByRecipe(_recipe.Id);
			if (recipeDetails is null || recipeDetails.Count == 0)
			{
				StateHasChanged();
				return;
			}

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

			_infoMessage = $"Recipe loaded for {_selectedProduct.Name} with {_recipeItems.Count} items";
			await _sfUpdateToast?.ShowAsync();
			if (_sfGrid is not null)
				await _sfGrid?.Refresh();
			StateHasChanged();
		}
		catch (Exception ex)
		{
			_errorMessage = $"Error loading recipe: {ex.Message}";
			await _sfErrorToast?.ShowAsync();
		}
	}
	#endregion

	#region Cart
	private async Task AddRawMaterialToRecipe()
	{
		try
		{
			if (_selectedRawMaterial is null || _selectedRawMaterial.Id == 0 || _selectedRawMaterialQuantity <= 0)
			{
				_errorMessage = "Please select a raw material and enter a valid quantity";
				await _sfErrorToast?.ShowAsync();
				return;
			}

			var existingRecipe = _recipeItems.FirstOrDefault(r => r.ItemId == _selectedRawMaterial.Id);
			if (existingRecipe is not null)
			{
				existingRecipe.Quantity += _selectedRawMaterialQuantity;
				_successMessage = $"Updated quantity for {_selectedRawMaterial.Name}";
			}
			else
			{
				_recipeItems.Add(new()
				{
					ItemId = _selectedRawMaterial.Id,
					ItemName = (await CommonData.LoadTableDataById<RawMaterialModel>(TableNames.RawMaterial, _selectedRawMaterial.Id)).Name,
					Quantity = _selectedRawMaterialQuantity,
				});
				_successMessage = $"Added {_selectedRawMaterial.Name} to recipe";
			}

			await _sfToast?.ShowAsync();

			// Reset selection
			_selectedRawMaterial = null;
			_selectedRawMaterialQuantity = 0;

			if (_sfGrid is not null)
				await _sfGrid?.Refresh();
			StateHasChanged();
		}
		catch (Exception ex)
		{
			_errorMessage = $"Error adding raw material: {ex.Message}";
			await _sfErrorToast?.ShowAsync();
		}
	}

	public async Task RowSelectHandler(RowSelectEventArgs<ItemRecipeModel> args)
	{
		try
		{
			if (args.Data is not null)
			{
				var itemName = args.Data.ItemName;
				_recipeItems.Remove(args.Data);
				_successMessage = $"Removed {itemName} from recipe";
				await _sfToast?.ShowAsync();
			}

			if (_sfGrid is not null)
				await _sfGrid?.Refresh();
			StateHasChanged();
		}
		catch (Exception ex)
		{
			_errorMessage = $"Error removing item: {ex.Message}";
			await _sfErrorToast?.ShowAsync();
		}
	}

	private async Task RemoveItem(ItemRecipeModel item)
	{
		try
		{
			if (item is not null)
			{
				var itemName = item.ItemName;
				_recipeItems.Remove(item);
				_successMessage = $"Removed {itemName} from recipe";
				await _sfToast?.ShowAsync();
			}

			if (_sfGrid is not null)
				await _sfGrid?.Refresh();
			StateHasChanged();
		}
		catch (Exception ex)
		{
			_errorMessage = $"Error removing item: {ex.Message}";
			await _sfErrorToast?.ShowAsync();
		}
	}
	#endregion

	#region Saving
	private async Task OnSaveButtonClick()
	{
		try
		{
			if (_sfGrid is not null)
				await _sfGrid?.Refresh();

			if (_selectedProduct is null || _selectedProduct.Id == 0)
			{
				_errorMessage = "Please select a product";
				await _sfErrorToast?.ShowAsync();
				return;
			}

			if (_recipeItems.Count == 0)
			{
				_errorMessage = "Please add at least one raw material to the recipe";
				await _sfErrorToast?.ShowAsync();
				return;
			}

			// Disable existing recipe details if recipe exists
			if (_recipe is not null)
			{
				var recipeDetails = await RecipeData.LoadRecipeDetailByRecipe(_recipe.Id);

				foreach (var detail in recipeDetails)
				{
					detail.Status = false;
					await RecipeData.InsertRecipeDetail(detail);
				}
			}

			// Insert or update recipe
			var recipeId = await RecipeData.InsertRecipe(new()
			{
				Id = _recipe?.Id ?? 0,
				ProductId = _selectedProduct.ProductId,
				Status = true,
			});

			// Insert new recipe details
			foreach (var rawMaterialRecipe in _recipeItems)
			{
				await RecipeData.InsertRecipeDetail(new()
				{
					Id = 0,
					RecipeId = recipeId,
					RawMaterialId = rawMaterialRecipe.ItemId,
					Quantity = rawMaterialRecipe.Quantity,
					Status = true,
				});
			}

			_successMessage = $"Recipe saved successfully for {_selectedProduct.Name} with {_recipeItems.Count} items!";
			await _sfToast?.ShowAsync();

			// Reload recipe to reflect saved state
			await Task.Delay(500);
			await LoadRecipe();
		}
		catch (Exception ex)
		{
			_errorMessage = $"Error saving recipe: {ex.Message}";
			await _sfErrorToast?.ShowAsync();
		}
	}
	#endregion
}