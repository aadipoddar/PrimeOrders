using Microsoft.AspNetCore.Components;

using PrimeBakes.Services;

using PrimeOrdersLibrary.Data.Common;
using PrimeOrdersLibrary.DataAccess;
using PrimeOrdersLibrary.Models.Common;
using PrimeOrdersLibrary.Models.Order;
using PrimeOrdersLibrary.Models.Product;

using Syncfusion.Blazor.DropDowns;
using Syncfusion.Blazor.Grids;

namespace PrimeBakes.Components.Pages.Order;

public partial class OrderPage
{
	[Inject] public NavigationManager NavManager { get; set; }

	private UserModel _user;
	private bool _isLoading = true;

	private const string _fileName = "orderCart.json";

	private int _selectedCategoryId = 0;

	private List<ProductCategoryModel> _productCategories = [];
	private readonly List<OrderProductCartModel> _cart = [];

	private SfGrid<OrderProductCartModel> _sfGrid;

	protected override async Task OnInitializedAsync()
	{
		_user = await AuthService.AuthenticateCurrentUser(NavManager);

		await LoadData();
		_isLoading = false;
	}

	private async Task LoadData()
	{
		_productCategories = await CommonData.LoadTableDataByStatus<ProductCategoryModel>(TableNames.ProductCategory);
		_productCategories.RemoveAll(r => r.LocationId != 1);

		_productCategories.Add(new()
		{
			Id = 0,
			Name = "All Categories"
		});

		_productCategories.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.Ordinal));

		_selectedCategoryId = 0;

		var allProducts = await CommonData.LoadTableDataByStatus<ProductModel>(TableNames.Product);
		allProducts.RemoveAll(r => r.LocationId != 1);

		foreach (var product in allProducts)
			_cart.Add(new()
			{
				ProductCategoryId = product.ProductCategoryId,
				ProductId = product.Id,
				ProductName = product.Name,
				Quantity = 0
			});

		_cart.Sort((x, y) => string.Compare(x.ProductName, y.ProductName, StringComparison.Ordinal));

		var fullPath = Path.Combine(FileSystem.Current.AppDataDirectory, _fileName);
		if (File.Exists(fullPath))
		{
			var items = System.Text.Json.JsonSerializer.Deserialize<List<OrderProductCartModel>>(await File.ReadAllTextAsync(fullPath)) ?? [];
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
		await _sfGrid.Refresh();
		StateHasChanged();

		await File.WriteAllTextAsync(Path.Combine(FileSystem.Current.AppDataDirectory, _fileName), System.Text.Json.JsonSerializer.Serialize(_cart.Where(_ => _.Quantity > 0)));
		HapticFeedback.Default.Perform(HapticFeedbackType.Click);
	}

	private async Task UpdateQuantity(OrderProductCartModel item, decimal newQuantity)
	{
		if (item is null)
			return;

		item.Quantity = Math.Max(0, newQuantity);
		await _sfGrid.Refresh();
		StateHasChanged();

		if (_cart.Where(x => x.Quantity > 0).Count() == 0 && File.Exists(Path.Combine(FileSystem.Current.AppDataDirectory, _fileName)))
			File.Delete(Path.Combine(FileSystem.Current.AppDataDirectory, _fileName));
		else
			await File.WriteAllTextAsync(Path.Combine(FileSystem.Current.AppDataDirectory, _fileName), System.Text.Json.JsonSerializer.Serialize(_cart.Where(_ => _.Quantity > 0)));

		HapticFeedback.Default.Perform(HapticFeedbackType.Click);
	}

	private async Task GoToCart()
	{
		if (_cart.Sum(x => x.Quantity) <= 0)
			return;

		await File.WriteAllTextAsync(Path.Combine(FileSystem.Current.AppDataDirectory, _fileName), System.Text.Json.JsonSerializer.Serialize(_cart.Where(_ => _.Quantity > 0)));
		_cart.Clear();

		if (Vibration.Default.IsSupported)
			Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(500));

		NavManager.NavigateTo("/OrderCart");
	}
}