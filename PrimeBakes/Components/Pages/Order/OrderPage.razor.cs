using Microsoft.AspNetCore.Components;

using PrimeBakes.Services;

using PrimeOrdersLibrary.Data.Common;
using PrimeOrdersLibrary.DataAccess;
using PrimeOrdersLibrary.Models.Common;
using PrimeOrdersLibrary.Models.Order;
using PrimeOrdersLibrary.Models.Product;

using Syncfusion.Blazor.Grids;

namespace PrimeBakes.Components.Pages.Order;

public partial class OrderPage
{
	[Inject] public NavigationManager NavManager { get; set; }

	private UserModel _user;
	private bool _isLoading = true;

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
		var allProducts = await CommonData.LoadTableDataByStatus<ProductModel>(TableNames.Product);
		allProducts.RemoveAll(r => r.LocationId != 1);

		foreach (var product in allProducts)
			_cart.Add(new()
			{
				ProductId = product.Id,
				ProductName = product.Name,
				Quantity = 0
			});

		_cart.Sort((x, y) => string.Compare(x.ProductName, y.ProductName, StringComparison.Ordinal));

		if (_sfGrid is not null)
			await _sfGrid.Refresh();

		StateHasChanged();
	}

	private async Task AddToCart(OrderProductCartModel item)
	{
		if (item != null)
		{
			item.Quantity = 1;
			await _sfGrid.Refresh();
			StateHasChanged();
		}
	}

	private async Task UpdateQuantity(OrderProductCartModel item, decimal newQuantity)
	{
		if (item != null)
		{
			item.Quantity = Math.Max(0, newQuantity);
			await _sfGrid.Refresh();
			StateHasChanged();
		}
	}

	private void GoToCart()
	{
		if (_cart.Sum(x => x.Quantity) > 0)
		{
			// Navigate to cart page - adjust the route as needed for your Blazor app
			NavManager.NavigateTo("/Cart");
		}
	}
}