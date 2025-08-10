using System.Collections.ObjectModel;

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
	private readonly ObservableCollection<OrderProductCartModel> _cart = [];
	private string _currentSearchText = string.Empty;
	private bool _isRefreshing = false;

	public OrderPage(int userId)
	{
		InitializeComponent();

		_userId = userId;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();

		_allProducts = await CommonData.LoadTableDataByStatus<ProductModel>(TableNames.Product);
		_allProducts.RemoveAll(r => r.LocationId != 1);

		foreach (var product in _allProducts)
			_cart.Add(new()
			{
				ProductId = product.Id,
				ProductName = product.Name,
				Quantity = 0
			});

		var fullPath = Path.Combine(FileSystem.Current.AppDataDirectory, _fileName);
		if (File.Exists(fullPath))
		{
			var items = System.Text.Json.JsonSerializer.Deserialize<List<OrderProductCartModel>>(File.ReadAllText(fullPath)) ?? [];
			foreach (var item in items)
				_cart.Where(p => p.ProductId == item.ProductId).FirstOrDefault().Quantity = item.Quantity;
		}

		RefreshCollectionView();
	}

	#region Collection View

	private void searchTextBox_TextChanged(object sender, TextChangedEventArgs e)
	{
		_currentSearchText = e.NewTextValue ?? string.Empty;
		RefreshCollectionView();
	}

	private void RefreshCollectionView()
	{
		if (_isRefreshing)
			return;

		_isRefreshing = true;

		if (string.IsNullOrEmpty(_currentSearchText))
			itemsCollectionView.ItemsSource = _cart.Take(20).ToList();

		else
			itemsCollectionView.ItemsSource = _cart
				.Where(p => p.ProductName.Contains(_currentSearchText, StringComparison.OrdinalIgnoreCase))
				.Take(20)
				.ToList();

		File.WriteAllText(Path.Combine(FileSystem.Current.AppDataDirectory, _fileName), System.Text.Json.JsonSerializer.Serialize(_cart.Where(_ => _.Quantity > 0)));
		cartItemsLabel.Text = $"{_cart.Sum(_ => _.Quantity)} Items";

		_isRefreshing = false;
	}

	private void AddButton_Clicked(object sender, EventArgs e)
	{
		if ((sender as Button).BindingContext is not OrderProductCartModel product)
			return;

		var existingCartItem = _cart.FirstOrDefault(c => c.ProductId == product.ProductId);
		if (existingCartItem is not null)
			existingCartItem.Quantity += 1;

		else
			_cart.Add(new()
			{
				ProductId = product.ProductId,
				ProductName = product.ProductName,
				Quantity = 1
			});

		RefreshCollectionView();
		HapticFeedback.Default.Perform(HapticFeedbackType.Click);
	}

	private void quantityNumericEntry_ValueChanged(object sender, Syncfusion.Maui.Inputs.NumericEntryValueChangedEventArgs e)
	{
		if ((sender as Syncfusion.Maui.Inputs.SfNumericEntry).BindingContext is not OrderProductCartModel item)
			return;

		if (_isRefreshing)
			return;

		_cart.FirstOrDefault(x => x.ProductId == item.ProductId).Quantity = Convert.ToInt32(e.NewValue);

		File.WriteAllText(Path.Combine(FileSystem.Current.AppDataDirectory, _fileName), System.Text.Json.JsonSerializer.Serialize(_cart.Where(_ => _.Quantity > 0)));
		cartItemsLabel.Text = $"{_cart.Sum(_ => _.Quantity)} Items";

		if (Convert.ToInt32(e.NewValue) == 0)
			RefreshCollectionView();

		HapticFeedback.Default.Perform(HapticFeedbackType.Click);
	}

	private void PlusButton_Clicked(object sender, EventArgs e)
	{
		if ((sender as Button).BindingContext is not OrderProductCartModel item)
			return;

		_cart.FirstOrDefault(x => x.ProductId == item.ProductId).Quantity = _cart.FirstOrDefault(x => x.ProductId == item.ProductId).Quantity + 1;
		RefreshCollectionView();
		HapticFeedback.Default.Perform(HapticFeedbackType.Click);
	}

	private void MinusButton_Clicked(object sender, EventArgs e)
	{
		if ((sender as Button).BindingContext is not OrderProductCartModel item)
			return;

		_cart.FirstOrDefault(x => x.ProductId == item.ProductId).Quantity = Math.Max(0, _cart.FirstOrDefault(x => x.ProductId == item.ProductId).Quantity - 1);
		RefreshCollectionView();
		HapticFeedback.Default.Perform(HapticFeedbackType.Click);
	}
	#endregion

	private async void CartButton_Clicked(object sender, EventArgs e)
	{
		if (_cart.Sum(_ => _.Quantity) == 0)
		{
			await DisplayAlert("Empty Cart", "Your cart is empty. Please add items to your cart before proceeding.", "OK");
			return;
		}

		await File.WriteAllTextAsync(Path.Combine(FileSystem.Current.AppDataDirectory, _fileName), System.Text.Json.JsonSerializer.Serialize(_cart.Where(_ => _.Quantity > 0)));
		await Navigation.PushAsync(new CartPage(_userId, this));

		HapticFeedback.Default.Perform(HapticFeedbackType.Click);
	}
}