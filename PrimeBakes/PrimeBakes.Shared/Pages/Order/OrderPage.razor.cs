using PrimeBakes.Shared.Services;

using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Product;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Order;
using PrimeBakesLibrary.Models.Product;

using Syncfusion.Blazor.DropDowns;
using Syncfusion.Blazor.Grids;

namespace PrimeBakes.Shared.Pages.Order;

public partial class OrderPage
{
	private UserModel _user;
	private bool _isLoading = true;

	private int _selectedCategoryId = 0;

	private List<ProductCategoryModel> _productCategories = [];
	private readonly List<OrderProductCartModel> _cart = [];

	private SfGrid<OrderProductCartModel> _sfGrid;

	protected override async Task OnInitializedAsync()
	{
		var authResult = await AuthService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, UserRoles.Order);
		_user = authResult.User;
		await LoadData();
		_isLoading = false;
	}

	private async Task LoadData()
	{
		_productCategories = await CommonData.LoadTableDataByStatus<ProductCategoryModel>(TableNames.ProductCategory);
		_productCategories.Add(new()
		{
			Id = 0,
			Name = "All Categories"
		});
		_productCategories.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.Ordinal));
		_selectedCategoryId = 0;

		var mainLocationProducts = await ProductData.LoadProductByLocation(1);
		var userLocationProducts = await ProductData.LoadProductByLocation(_user.LocationId);
		var allProducts = mainLocationProducts.Where(x => userLocationProducts.Any(y => y.ProductId == x.ProductId)).ToList();
		foreach (var product in allProducts)
			_cart.Add(new()
			{
				ProductCategoryId = product.ProductCategoryId,
				ProductId = product.ProductId,
				ProductName = product.Name,
				Quantity = 0
			});

		_cart.Sort((x, y) => string.Compare(x.ProductName, y.ProductName, StringComparison.Ordinal));

		if (await DataStorageService.LocalExists(StorageFileNames.OrderCartDataFileName))
		{
			var items = System.Text.Json.JsonSerializer.Deserialize<List<OrderProductCartModel>>(await DataStorageService.LocalGetAsync(StorageFileNames.OrderCartDataFileName)) ?? [];
			foreach (var item in items)
				_cart.Where(p => p.ProductId == item.ProductId).FirstOrDefault().Quantity = item.Quantity;
		}

		if (_sfGrid is not null)
			await _sfGrid.Refresh();

		StateHasChanged();
	}

	private async Task OnProductCategoryChanged(ChangeEventArgs<int, ProductCategoryModel> args)
	{
		if (args is null || args.Value <= 0)
			_selectedCategoryId = 0;
		else
			_selectedCategoryId = args.Value;

		await _sfGrid.Refresh();
		StateHasChanged();
	}

	private async Task AddToCart(OrderProductCartModel item)
	{
		if (item is null)
			return;

		item.Quantity = 1;
		await SaveOrderFile();
	}

	private async Task UpdateQuantity(OrderProductCartModel item, decimal newQuantity)
	{
		if (item is null)
			return;

		item.Quantity = Math.Max(0, newQuantity);
		await SaveOrderFile();
	}

	private async Task SaveOrderFile()
	{
		if (!_cart.Any(x => x.Quantity > 0) && await DataStorageService.LocalExists(StorageFileNames.OrderCartDataFileName))
			await DataStorageService.LocalRemove(StorageFileNames.OrderCartDataFileName);
		else
			await DataStorageService.LocalSaveAsync(StorageFileNames.OrderCartDataFileName, System.Text.Json.JsonSerializer.Serialize(_cart.Where(_ => _.Quantity > 0)));

		VibrationService.VibrateHapticClick();
		await _sfGrid.Refresh();
		StateHasChanged();
	}

	private async Task GoToCart()
	{
		await SaveOrderFile();

		if (_cart.Sum(x => x.Quantity) <= 0 || await DataStorageService.LocalExists(StorageFileNames.OrderCartDataFileName) == false)
			return;

		VibrationService.VibrateWithTime(500);
		_cart.Clear();

		NavigationManager.NavigateTo("/Order/Cart");
	}
}