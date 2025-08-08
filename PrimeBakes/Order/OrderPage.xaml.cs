using PrimeOrdersLibrary.Data.Common;
using PrimeOrdersLibrary.DataAccess;
using PrimeOrdersLibrary.Models.Order;
using PrimeOrdersLibrary.Models.Product;

namespace PrimeBakes.Order;

public partial class OrderPage : ContentPage
{
	private const string _fileName = "cart.json";
	private readonly int _userId;
	private List<ProductModel> _allProducts;
	private List<ProductModel> _products;
	private readonly List<OrderProductCartModel> _cart = [];

	public OrderPage(int userId)
	{
		InitializeComponent();

		_userId = userId;

		var fullPath = Path.Combine(FileSystem.Current.AppDataDirectory, _fileName);
		if (File.Exists(fullPath))
		{
			var cartData = File.ReadAllText(fullPath);
			_cart.AddRange(System.Text.Json.JsonSerializer.Deserialize<List<OrderProductCartModel>>(cartData) ?? []);
		}
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();

		_allProducts = await CommonData.LoadTableDataByStatus<ProductModel>(TableNames.Product);
		_allProducts.RemoveAll(r => r.LocationId != 1);
		_products = [.. _allProducts.Take(20)];

		itemsCollectionView.ItemsSource = _products;

		cartItemsLabel.Text = $"{_cart.Sum(_ => _.Quantity)} Items";
	}

	private void searchTextBox_TextChanged(object sender, TextChangedEventArgs e)
	{
		_products = [.. _allProducts.Where(p => p.Name.Contains(e.NewTextValue, StringComparison.OrdinalIgnoreCase))];
		_products = [.. _products.Take(20)];
		itemsCollectionView.ItemsSource = _products;
		cartItemsLabel.Text = $"{_cart.Sum(_ => _.Quantity)} Items";
	}

	private void AddButton_Clicked(object sender, EventArgs e)
	{
		if ((sender as Button).Parent.BindingContext is not ProductModel product)
			return;

		var existingCartItem = _cart.FirstOrDefault(c => c.ProductId == product.Id);
		if (existingCartItem is not null)
			existingCartItem.Quantity += 1;
		else
			_cart.Add(new()
			{
				ProductId = product.Id,
				ProductName = product.Name,
				Quantity = 1
			});

		cartItemsLabel.Text = $"{_cart.Sum(_ => _.Quantity)} Items";
	}

	private async void CartButton_Clicked(object sender, EventArgs e)
	{
		if (_cart.Count == 0)
		{
			await DisplayAlert("Empty Cart", "Your cart is empty. Please add items to your cart before proceeding.", "OK");
			return;
		}

		var fullPath = Path.Combine(FileSystem.Current.AppDataDirectory, _fileName);

		if (File.Exists(fullPath))
			File.Delete(fullPath);

		File.WriteAllText(fullPath, System.Text.Json.JsonSerializer.Serialize(_cart));
		await Navigation.PushAsync(new CartPage(_userId));
	}
}