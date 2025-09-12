using Microsoft.AspNetCore.Components;

using PrimeBakes.Services;

using PrimeOrdersLibrary.Data.Common;
using PrimeOrdersLibrary.DataAccess;
using PrimeOrdersLibrary.Models.Common;
using PrimeOrdersLibrary.Models.Product;
using PrimeOrdersLibrary.Models.Sale;

using Syncfusion.Blazor.DropDowns;
using Syncfusion.Blazor.Grids;

namespace PrimeBakes.Components.Pages.Sale;

public partial class SalePage
{
	[Inject] public NavigationManager NavManager { get; set; }

	private UserModel _user;
	private bool _isLoading = true;

	private const string _fileName = "saleCart.json";

	private int _selectedCategoryId = 0;

	private List<ProductCategoryModel> _productCategories = [];
	private readonly List<SaleProductCartModel> _cart = [];

	private SfGrid<SaleProductCartModel> _sfGrid;

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
		{
			var productTax = await CommonData.LoadTableDataById<TaxModel>(TableNames.Tax, product.TaxId);

			_cart.Add(new()
			{
				ProductId = product.Id,
				ProductName = product.Name,
				ProductCategoryId = product.ProductCategoryId,
				Rate = product.Rate,
				Quantity = 0,
				DiscPercent = 0,
				CGSTPercent = productTax.Extra ? productTax.CGST : 0,
				SGSTPercent = productTax.Extra ? productTax.SGST : 0,
				IGSTPercent = productTax.Extra ? productTax.IGST : 0
			});
		}

		UpdateFinancialDetails();

		_cart.Sort((x, y) => string.Compare(x.ProductName, y.ProductName, StringComparison.Ordinal));

		var fullPath = Path.Combine(FileSystem.Current.AppDataDirectory, _fileName);
		if (File.Exists(fullPath))
		{
			var items = System.Text.Json.JsonSerializer.Deserialize<List<SaleProductCartModel>>(await File.ReadAllTextAsync(fullPath)) ?? [];
			foreach (var item in items)
			{
				_cart.Where(p => p.ProductId == item.ProductId).FirstOrDefault().Rate = item.Rate;
				_cart.Where(p => p.ProductId == item.ProductId).FirstOrDefault().Quantity = item.Quantity;
				_cart.Where(p => p.ProductId == item.ProductId).FirstOrDefault().DiscPercent = item.DiscPercent;
				_cart.Where(p => p.ProductId == item.ProductId).FirstOrDefault().CGSTPercent = item.CGSTPercent;
				_cart.Where(p => p.ProductId == item.ProductId).FirstOrDefault().SGSTPercent = item.SGSTPercent;
				_cart.Where(p => p.ProductId == item.ProductId).FirstOrDefault().IGSTPercent = item.IGSTPercent;
			}
		}

		UpdateFinancialDetails();

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

	private async Task AddToCart(SaleProductCartModel item)
	{
		if (item is null)
			return;

		item.Quantity = 1;
		UpdateFinancialDetails();
		await _sfGrid.Refresh();
		StateHasChanged();

		await File.WriteAllTextAsync(Path.Combine(FileSystem.Current.AppDataDirectory, _fileName), System.Text.Json.JsonSerializer.Serialize(_cart.Where(_ => _.Quantity > 0)));
		HapticFeedback.Default.Perform(HapticFeedbackType.Click);
	}

	private async Task UpdateQuantity(SaleProductCartModel item, decimal newQuantity)
	{
		if (item is null)
			return;

		item.Quantity = Math.Max(0, newQuantity);
		UpdateFinancialDetails();
		await _sfGrid.Refresh();
		StateHasChanged();

		if (_cart.Where(x => x.Quantity > 0).Count() == 0 && File.Exists(Path.Combine(FileSystem.Current.AppDataDirectory, _fileName)))
			File.Delete(Path.Combine(FileSystem.Current.AppDataDirectory, _fileName));
		else
			await File.WriteAllTextAsync(Path.Combine(FileSystem.Current.AppDataDirectory, _fileName), System.Text.Json.JsonSerializer.Serialize(_cart.Where(_ => _.Quantity > 0)));

		HapticFeedback.Default.Perform(HapticFeedbackType.Click);
	}

	private void UpdateFinancialDetails()
	{
		foreach (var item in _cart)
		{
			item.BaseTotal = item.Rate * item.Quantity;
			item.DiscAmount = item.BaseTotal * (item.DiscPercent / 100);
			item.AfterDiscount = item.BaseTotal - item.DiscAmount;
			item.CGSTAmount = item.AfterDiscount * (item.CGSTPercent / 100);
			item.SGSTAmount = item.AfterDiscount * (item.SGSTPercent / 100);
			item.IGSTAmount = item.AfterDiscount * (item.IGSTPercent / 100);
			item.Total = item.AfterDiscount + item.CGSTAmount + item.SGSTAmount + item.IGSTAmount;
			item.NetRate = item.Quantity > 0 ? item.Total / item.Quantity : 0;
		}

		_sfGrid?.Refresh();
		StateHasChanged();
	}

	private async Task GoToCart()
	{
		if (_cart.Sum(x => x.Quantity) <= 0)
			return;

		UpdateFinancialDetails();

		await File.WriteAllTextAsync(Path.Combine(FileSystem.Current.AppDataDirectory, _fileName), System.Text.Json.JsonSerializer.Serialize(_cart.Where(_ => _.Quantity > 0)));
		_cart.Clear();

		if (Vibration.Default.IsSupported)
			Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(500));

		NavManager.NavigateTo("/SaleCart");
	}
}