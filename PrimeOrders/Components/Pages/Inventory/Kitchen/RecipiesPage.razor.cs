using Syncfusion.Blazor.DropDowns;
using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Notifications;

namespace PrimeOrders.Components.Pages.Inventory.Kitchen;

public partial class RecipiesPage
{
	[Inject] public NavigationManager NavManager { get; set; }
	[Inject] public IJSRuntime JS { get; set; }

	private bool _isLoading = true;

	private int _selectedProductCategoryId = 0;
	private int _selectedProductId = 0;
	private int _selectedRawMaterialCategoryId = 0;
	private int _selectedRawMaterialId = 0;

	private double _selectedRawMaterialQuantity = 1;

	private List<ProductCategoryModel> _productCategories = [];
	private List<ProductModel> _products = [];
	private List<RawMaterialCategoryModel> _rawMaterialCategories = [];
	private List<RawMaterialModel> _rawMaterials = [];

	private readonly List<ItemRecipeModel> _rawMaterialRecipies = [];

	private RecipeModel _recipe;

	private SfGrid<ItemRecipeModel> _sfGrid;
	private SfToast _sfToast;
	private SfToast _sfUpdateToast;

	#region Load Data
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		_isLoading = true;

		if (!((await AuthService.ValidateUser(JS, NavManager, UserRoles.Inventory, true)).User is not null))
			return;

		await LoadData();
		await LoadRecipe();

		_isLoading = false;
		StateHasChanged();
	}

	private async Task LoadData()
	{
		_productCategories = await CommonData.LoadTableData<ProductCategoryModel>(TableNames.ProductCategory);
		_selectedProductCategoryId = _productCategories.Count > 0 ? _productCategories[0].Id : 0;

		_products = await ProductData.LoadProductByProductCategory(_selectedProductCategoryId);
		_selectedProductId = _products.Count > 0 ? _products[0].Id : 0;

		_rawMaterialCategories = await CommonData.LoadTableData<RawMaterialCategoryModel>(TableNames.RawMaterialCategory);
		_selectedRawMaterialCategoryId = _rawMaterialCategories.Count > 0 ? _rawMaterialCategories[0].Id : 0;

		_rawMaterials = await RawMaterialData.LoadRawMaterialByRawMaterialCategory(_selectedRawMaterialCategoryId);
		_selectedRawMaterialId = _rawMaterials.Count > 0 ? _rawMaterials[0].Id : 0;

		await LoadRecipe();
	}

	private async Task RawMaterialCategoryComboBoxValueChangeHandler(ChangeEventArgs<int, RawMaterialCategoryModel> args)
	{
		_selectedRawMaterialCategoryId = args.Value;
		_rawMaterials = await RawMaterialData.LoadRawMaterialByRawMaterialCategory(_selectedRawMaterialCategoryId);
		_selectedRawMaterialId = _rawMaterials.Count > 0 ? _rawMaterials[0].Id : 0;
	}

	private async Task ProductCategoryComboBoxValueChangeHandler(ChangeEventArgs<int, ProductCategoryModel> args)
	{
		_selectedRawMaterialId = args.Value;
		_products = await ProductData.LoadProductByProductCategory(_selectedProductCategoryId);
		_selectedProductId = _products.Count > 0 ? _products[0].Id : 0;
		await LoadRecipe();
	}

	private async Task ProductComboBoxValueChangeHandler(ChangeEventArgs<int, ProductModel> args)
	{
		_selectedProductId = args.Value;
		await LoadRecipe();
	}

	private async Task LoadRecipe()
	{
		if (_sfGrid is null)
			return;

		_rawMaterialRecipies.Clear();
		await _sfGrid.Refresh();

		_recipe = await RecipeData.LoadRecipeByProduct(_selectedProductId);
		if (_recipe is null)
			return;

		var recipeDetails = await RecipeData.LoadRecipeDetailByRecipe(_recipe.Id);
		if (recipeDetails is null)
			return;

		_rawMaterialRecipies.Clear();

		foreach (var detail in recipeDetails)
		{
			var rawMaterial = await CommonData.LoadTableDataById<RawMaterialModel>(TableNames.RawMaterial, detail.RawMaterialId);

			_rawMaterialRecipies.Add(new ItemRecipeModel()
			{
				ItemId = detail.RawMaterialId,
				ItemName = rawMaterial.Name,
				ItemCategoryId = rawMaterial.RawMaterialCategoryId,
				Quantity = detail.Quantity
			});
		}

		await _sfUpdateToast.ShowAsync();
		await _sfGrid?.Refresh();
		StateHasChanged();
	}
	#endregion

	#region Data Grid
	private async Task OnAddButtonClick()
	{
		var existingRecipe = _rawMaterialRecipies.FirstOrDefault(r => r.ItemId == _selectedRawMaterialId && r.ItemCategoryId == _selectedRawMaterialCategoryId);
		if (existingRecipe is not null)
			existingRecipe.Quantity += (decimal)_selectedRawMaterialQuantity;

		else
			_rawMaterialRecipies.Add(new()
			{
				ItemCategoryId = _selectedRawMaterialCategoryId,
				ItemId = _selectedRawMaterialId,
				ItemName = (await CommonData.LoadTableDataById<RawMaterialModel>(TableNames.RawMaterial, _selectedRawMaterialId)).Name,
				Quantity = (decimal)_selectedRawMaterialQuantity,
			});

		await _sfGrid.Refresh();
		StateHasChanged();
	}

	public void RowSelectHandler(RowSelectEventArgs<ItemRecipeModel> args)
	{
		if (args.Data is not null)
			_rawMaterialRecipies.Remove(args.Data);

		_sfGrid.Refresh();
		StateHasChanged();
	}
	#endregion

	#region Saving
	private async Task OnSaveButtonClick()
	{
		await _sfGrid.Refresh();

		if (_selectedProductId == 0 || _rawMaterialRecipies.Count == 0)
			return;

		if (_recipe is not null)
		{
			var recipeDetails = await RecipeData.LoadRecipeDetailByRecipe(_recipe.Id);

			foreach (var detail in recipeDetails)
			{
				await RecipeData.InsertRecipeDetail(new()
				{
					Id = detail.Id,
					RecipeId = _recipe.Id,
					RawMaterialId = detail.RawMaterialId,
					Quantity = detail.Quantity,
					Status = false,
				});
			}
		}

		var recipeId = await RecipeData.InsertRecipe(new()
		{
			Id = _recipe?.Id ?? 0,
			ProductId = _selectedProductId,
			Status = true,
		});

		foreach (var rawMaterialRecipe in _rawMaterialRecipies)
			await RecipeData.InsertRecipeDetail(new RecipeDetailModel
			{
				Id = 0,
				RecipeId = recipeId,
				RawMaterialId = rawMaterialRecipe.ItemId,
				Quantity = rawMaterialRecipe.Quantity,
				Status = true,
			});

		await _sfToast.ShowAsync();
	}
	#endregion
}