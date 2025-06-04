using PrimeOrdersLibrary.Data.Inventory;
using PrimeOrdersLibrary.Data.Product;

using Syncfusion.Blazor.DropDowns;
using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Notifications;

namespace PrimeOrders.Components.Pages.Inventory;

public partial class RecipiesPage
{
	[Inject] public NavigationManager NavManager { get; set; }
	[Inject] public IJSRuntime JS { get; set; }

	private bool IsLoading { get; set; } = true;

	private int _selectedProductCategoryId = 0;
	private int _selectedProductId = 0;
	private int _selectedRawMaterialCategoryId = 0;
	private int _selectedRawMaterialId = 0;

	private double _selectedRawMaterialQuantity = 1;

	private List<ProductCategoryModel> _productCategories = [];
	private List<ProductModel> _products = [];
	private List<RawMaterialCategoryModel> _rawMaterialCategories = [];
	private List<RawMaterialModel> _rawMaterials = [];

	private readonly List<RawMaterialRecipeModel> _rawMaterialRecipies = [];

	private RecipeModel _recipe;

	private SfGrid<RawMaterialRecipeModel> _sfGrid;
	private SfToast _sfToast;
	private SfToast _sfUpdateToast;

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		IsLoading = true;

		if (firstRender && !await ValidatePassword())
			NavManager.NavigateTo("/Login");

		if (firstRender)
			await LoadComboBox();

		IsLoading = false;
		StateHasChanged();

		if (firstRender)
			await LoadRecipe();
	}

	private async Task<bool> ValidatePassword()
	{
		var userId = await JS.InvokeAsync<string>("getCookie", "UserId");
		var password = await JS.InvokeAsync<string>("getCookie", "Passcode");

		if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(password))
			return false;

		var user = await CommonData.LoadTableDataById<UserModel>(TableNames.User, int.Parse(userId));
		if (user is null || !BCrypt.Net.BCrypt.EnhancedVerify(user.Passcode.ToString(), password))
			return false;

		return true;
	}

	private async Task LoadComboBox()
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

	private async void RawMaterialCategoryComboBoxValueChangeHandler(ChangeEventArgs<int, RawMaterialCategoryModel> args)
	{
		_selectedRawMaterialCategoryId = args.Value;
		_rawMaterials = await RawMaterialData.LoadRawMaterialByRawMaterialCategory(_selectedRawMaterialCategoryId);
		_selectedRawMaterialId = _rawMaterials.Count > 0 ? _rawMaterials[0].Id : 0;
	}

	private async void ProductCategoryComboBoxValueChangeHandler(ChangeEventArgs<int, ProductCategoryModel> args)
	{
		_selectedRawMaterialId = args.Value;
		_products = await ProductData.LoadProductByProductCategory(_selectedProductCategoryId);
		_selectedProductId = _products.Count > 0 ? _products[0].Id : 0;
		await LoadRecipe();
	}

	private async void ProductComboBoxValueChangeHandler(ChangeEventArgs<int, ProductModel> args)
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

			_rawMaterialRecipies.Add(new RawMaterialRecipeModel()
			{
				RawMaterialId = detail.RawMaterialId,
				RawMaterialName = rawMaterial.Name,
				RawMaterialCategoryId = rawMaterial.RawMaterialCategoryId,
				Quantity = detail.Quantity
			});
		}

		await _sfUpdateToast.ShowAsync();
		await _sfGrid?.Refresh();
		StateHasChanged();
	}

	private async void OnAddButtonClick()
	{
		var existingRecipe = _rawMaterialRecipies.FirstOrDefault(r => r.RawMaterialId == _selectedRawMaterialId && r.RawMaterialCategoryId == _selectedRawMaterialCategoryId);
		if (existingRecipe is not null)
			existingRecipe.Quantity += (decimal)_selectedRawMaterialQuantity;

		else
			_rawMaterialRecipies.Add(new()
			{
				RawMaterialCategoryId = _selectedRawMaterialCategoryId,
				RawMaterialId = _selectedRawMaterialId,
				RawMaterialName = (await CommonData.LoadTableDataById<RawMaterialModel>(TableNames.RawMaterial, _selectedRawMaterialId)).Name,
				Quantity = (decimal)_selectedRawMaterialQuantity,
			});

		await _sfGrid.Refresh();
		StateHasChanged();
	}

	public void RowSelectHandler(RowSelectEventArgs<RawMaterialRecipeModel> args)
	{
		if (args.Data is not null)
			_rawMaterialRecipies.Remove(args.Data);

		_sfGrid.Refresh();
		StateHasChanged();
	}

	private async void OnSaveButtonClick()
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
				RawMaterialId = rawMaterialRecipe.RawMaterialId,
				Quantity = rawMaterialRecipe.Quantity,
				Status = true,
			});

		await _sfToast.ShowAsync();
	}

	public void ClosedHandler(ToastCloseArgs args) =>
		NavManager.NavigateTo(NavManager.Uri, forceLoad: true);

	private void NavigateTo(string route) =>
		NavManager.NavigateTo(route);
}