using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;

using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Order;
using PrimeBakesLibrary.Data.Product;
using PrimeBakesLibrary.Data.Sale;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Exporting.Sale;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Order;
using PrimeBakesLibrary.Models.Product;
using PrimeBakesLibrary.Models.Sale;

using PrimeOrders.Common;

namespace PrimeOrders.Sale;

/// <summary>
/// Interaction logic for SaleWindow.xaml
/// </summary>
public partial class SaleWindow : Window
{
	private bool _isSaving = false;

	private readonly UserModel _user;
	private CustomerModel _customer = new();

	private readonly ObservableCollection<SaleProductCartModel> _cart = [];

	#region Load Data
	public SaleWindow(UserModel user)
	{
		InitializeComponent();

		_user = user;
		cartDataGrid.ItemsSource = _cart;
	}

	private async void Window_Loaded(object sender, RoutedEventArgs e)
	{
		if (_user.LocationId == 1)
		{
			partyOrderSection.Visibility = Visibility.Visible;

			var parties = await CommonData.LoadTableDataByStatus<LedgerModel>(TableNames.Ledger);
			partyAutoCompleteTextBox.AutoCompleteSource = parties.OrderBy(p => p.Name).ToList();
			partyAutoCompleteTextBox.ValueMemberPath = nameof(LedgerModel.Id);
			partyAutoCompleteTextBox.SearchItemPath = nameof(LedgerModel.Name);
		}

		saleDateTimePicker.DateTime = DateTime.Now;
		paymentModeComboBox.ItemsSource = PaymentModeData.GetPaymentModes();
		paymentModeComboBox.DisplayMemberPath = nameof(PaymentModeModel.Name);
		paymentModeComboBox.SelectedValuePath = nameof(PaymentModeModel.Id);
		paymentModeComboBox.SelectedIndex = 0;

		await LoadProducts();
		await LoadExistingSale();
		await SaveSaleFile();

		partyAutoCompleteTextBox.Focus();
	}

	private async Task LoadProducts()
	{
		try
		{
			List<SaleProductCartModel> allCart = [];

			var locationProducts = await ProductData.LoadProductByLocation(_user.LocationId);
			var selectedParty = partyAutoCompleteTextBox.SelectedItem as LedgerModel;

			if (selectedParty is not null && selectedParty.Id > 0 && selectedParty.LocationId is not null)
			{
				var partyLocationProducts = await ProductData.LoadProductByLocation(selectedParty.LocationId.Value);
				locationProducts = [.. locationProducts.Where(p => partyLocationProducts.Any(plp => plp.ProductId == p.ProductId))];
			}

			var taxes = await CommonData.LoadTableData<TaxModel>(TableNames.Tax);

			foreach (var product in locationProducts)
			{
				var productTax = taxes.FirstOrDefault(t => t.Id == product.TaxId) ?? new();

				allCart.Add(new()
				{
					ProductId = product.ProductId,
					ProductName = product.Name,
					ProductCategoryId = product.ProductCategoryId,
					Rate = product.Rate,
					Quantity = 0,
					BaseTotal = 0,
					DiscPercent = 0,
					DiscAmount = 0,
					AfterDiscount = 0,
					CGSTPercent = productTax.Extra ? productTax.CGST : 0,
					CGSTAmount = 0,
					SGSTPercent = productTax.Extra ? productTax.SGST : 0,
					SGSTAmount = 0,
					IGSTPercent = productTax.Extra ? productTax.IGST : 0,
					IGSTAmount = 0,
					Total = 0,
					NetRate = 0
				});
			}

			selectedProductAutoCompleteTextBox.AutoCompleteSource = allCart.OrderBy(p => p.ProductName).ToList();
			selectedProductAutoCompleteTextBox.ValueMemberPath = nameof(SaleProductCartModel.ProductId);
			selectedProductAutoCompleteTextBox.SearchItemPath = nameof(SaleProductCartModel.ProductName);
			selectedProductAutoCompleteTextBox.SelectedItem = null;
			selectedProductQuantityTextBox.Value = 0;
			selectedProductDiscountPercentTextBox.PercentValue = 0;
		}
		catch (Exception ex)
		{
			MessageBox.Show($"Error loading products: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			Close();
		}
	}

	private async Task LoadExistingSale()
	{
		try
		{
			var saleFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), StorageFileNames.SaleDataFileName);
			if (File.Exists(saleFilePath))
			{
				var saleData = System.Text.Json.JsonSerializer.Deserialize<SaleModel>(await File.ReadAllTextAsync(saleFilePath));
				if (saleData is not null)
				{
					billNoTextBox.Text = saleData.BillNo;
					saleDateTimePicker.DateTime = saleData.SaleDateTime;

					remarksTextBox.Text = saleData.Remarks;
					discountReasonTextBox.Text = saleData.DiscReason;

					_customer = new CustomerModel();
					if (saleData.CustomerId is not null && saleData.CustomerId > 0)
					{
						var customer = await CommonData.LoadTableDataById<CustomerModel>(TableNames.Customer, saleData.CustomerId.Value);
						if (customer is not null)
						{
							_customer = customer;
							customerNumberTextBox.Text = _customer.Number;
							customerNameTextBox.Text = _customer.Name;
						}
					}

					if (saleData.PartyId is not null)
					{
						partyAutoCompleteTextBox.SelectedItem = partyAutoCompleteTextBox.AutoCompleteSource.Cast<LedgerModel>().FirstOrDefault(x => x.Id == saleData.PartyId.Value);

						if (saleData.OrderId is not null)
						{
							await LoadProducts();
							orderPanel.Visibility = Visibility.Visible;
							var orders = await OrderData.LoadOrderByLocation(((LedgerModel)partyAutoCompleteTextBox.SelectedItem).LocationId.Value);
							orderAutoCompleteTextBox.AutoCompleteSource = orders;
							orderAutoCompleteTextBox.ValueMemberPath = nameof(OrderModel.Id);
							orderAutoCompleteTextBox.SearchItemPath = nameof(OrderModel.OrderNo);
							orderAutoCompleteTextBox.SelectedItem = orderAutoCompleteTextBox.AutoCompleteSource.Cast<OrderModel>().FirstOrDefault(x => x.Id == saleData.OrderId.Value);
						}
					}

					discountPercentTextBox.PercentValue = double.Parse(saleData.DiscPercent.ToString());
				}

				await LoadExistingCart();
			}
		}
		catch (Exception ex)
		{
			MessageBox.Show($"Error loading existing sale data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), StorageFileNames.SaleDataFileName));
		}
	}

	private async Task LoadExistingCart()
	{
		try
		{
			var saleCartFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), StorageFileNames.SaleCartDataFileName);
			if (File.Exists(saleCartFilePath))
			{
				var cartData = System.Text.Json.JsonSerializer.Deserialize<List<SaleProductCartModel>>(await File.ReadAllTextAsync(saleCartFilePath));
				if (cartData is not null)
				{
					_cart.Clear();
					foreach (var item in cartData)
						_cart.Add(item);
				}
			}
		}
		catch (Exception ex)
		{
			MessageBox.Show($"Error loading existing sale cart data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), StorageFileNames.SaleCartDataFileName));
		}
	}
	#endregion

	#region Party and Order
	private async void partyAutoCompleteTextBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		try
		{
			orderPanel.Visibility = Visibility.Collapsed;
			orderAutoCompleteTextBox.AutoCompleteSource = null;
			orderAutoCompleteTextBox.SelectedItem = null;
			discountPercentTextBox.PercentValue = 0;

			var selectedParty = partyAutoCompleteTextBox.SelectedItem as LedgerModel;

			if (selectedParty is not null)
			{
				if (selectedParty.LocationId is not null && selectedParty.LocationId > 1)
				{
					await LoadProducts();

					var location = await CommonData.LoadTableDataById<LocationModel>(TableNames.Location, selectedParty.LocationId.Value);
					discountPercentTextBox.PercentValue = double.Parse(location.Discount.ToString());

					orderPanel.Visibility = Visibility.Visible;
					var orders = await OrderData.LoadOrderByLocation(selectedParty.LocationId.Value);
					orderAutoCompleteTextBox.AutoCompleteSource = orders;
					orderAutoCompleteTextBox.ValueMemberPath = nameof(OrderModel.Id);
					orderAutoCompleteTextBox.SearchItemPath = nameof(OrderModel.OrderNo);
				}

			}
			else
			{
			}

			await SaveSaleFile();
		}
		catch (Exception ex)
		{
			MessageBox.Show($"Error loading orders for selected party: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			Close();
		}
	}

	private async void orderAutoCompleteTextBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		try
		{
			_cart.Clear();

			if (orderAutoCompleteTextBox.SelectedItem is null || partyAutoCompleteTextBox.SelectedItem is null)
			{
				await SaveSaleFile();
				return;
			}

			var selectedOrder = (OrderModel)orderAutoCompleteTextBox.SelectedItem;
			var orderItems = await OrderData.LoadOrderDetailByOrder(selectedOrder.Id);

			var locationProducts = await ProductData.LoadProductByLocation(_user.LocationId);
			var selectedParty = partyAutoCompleteTextBox.SelectedItem as LedgerModel;

			if (selectedParty is not null && selectedParty.Id > 0 && selectedParty.LocationId is not null)
			{
				var partyLocationProducts = await ProductData.LoadProductByLocation(selectedParty.LocationId.Value);
				locationProducts = [.. locationProducts.Where(p => partyLocationProducts.Any(plp => plp.ProductId == p.ProductId))];
			}

			var taxes = await CommonData.LoadTableData<TaxModel>(TableNames.Tax);

			foreach (var item in orderItems)
			{
				var product = locationProducts.FirstOrDefault(p => p.ProductId == item.ProductId);

				if (product is null)
				{
					MessageBox.Show($"Product with ID {item.ProductId} not found for the current location and party.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
					continue;
				}

				var productTax = taxes.FirstOrDefault(t => t.Id == product.TaxId) ?? new();

				_cart.Add(new()
				{
					ProductId = item.ProductId,
					ProductName = product.Name,
					ProductCategoryId = product.ProductCategoryId,
					Rate = product.Rate,
					Quantity = item.Quantity,
					BaseTotal = 0,
					DiscPercent = 0,
					DiscAmount = 0,
					AfterDiscount = 0,
					CGSTPercent = productTax.CGST,
					CGSTAmount = 0,
					SGSTPercent = productTax.SGST,
					SGSTAmount = 0,
					IGSTPercent = productTax.IGST,
					IGSTAmount = 0,
					Total = 0,
					NetRate = 0
				});
			}
		}
		catch (Exception ex)
		{
			MessageBox.Show($"Error loading selected order items: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			orderAutoCompleteTextBox.SelectedItem = null;
			_cart.Clear();
		}
		finally
		{
			await SaveSaleFile();
		}
	}

	private async void saleDateTimePicker_DateTimeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
		await SaveSaleFile();

	private async void discountPercentTextBox_PercentValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		discountPercentTextBox.Text = Math.Clamp(discountPercentTextBox.PercentValue.Value, 0, 100).ToString();
		await SaveSaleFile();
	}
	#endregion

	#region Customer Methods
	private async void customerNumberTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
	{
		try
		{
			_customer = new()
			{
				Number = customerNumberTextBox.Text
			};

			// Remove non-digit characters
			for (int i = 0; i < _customer.Number.Length; i++)
				if (!char.IsDigit(_customer.Number[i]))
				{
					_customer.Number = _customer.Number.Remove(i, 1);
					i--;
				}

			if (string.IsNullOrEmpty(_customer.Number))
			{
				_customer = new();
				return;
			}

			if (_customer.Number.Length > 10)
				_customer.Number = _customer.Number[..10];

			// Try to find existing customer
			var existingCustomer = await CustomerData.LoadCustomerByNumber(_customer.Number);
			if (existingCustomer is not null && existingCustomer.Id > 0)
				_customer = existingCustomer;
			else
				_customer = new() { Number = _customer.Number };

			customerNameTextBox.Text = _customer.Name;
		}
		catch (Exception ex)
		{
			MessageBox.Show($"Error loading customer data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			_customer = new();
			return;
		}
		finally
		{
			await SaveSaleFile();
		}
	}

	private async Task SaveCustomer()
	{
		try
		{
			if (!string.IsNullOrEmpty(_customer.Number) && !string.IsNullOrEmpty(customerNameTextBox.Text))
			{
				_customer.Name = customerNameTextBox.Text;

				// Validate and clean phone number
				for (int i = 0; i < _customer.Number.Length; i++)
					if (!char.IsDigit(_customer.Number[i]))
					{
						_customer.Number = _customer.Number.Remove(i, 1);
						i--;
					}

				if (_customer.Number.Length > 10)
					_customer.Number = _customer.Number[..10];

				// Check if customer already exists
				var existingCustomer = await CustomerData.LoadCustomerByNumber(_customer.Number);
				if (existingCustomer is not null && existingCustomer.Id > 0)
					_customer.Id = existingCustomer.Id;

				// Save or update customer
				_customer.Id = await CustomerData.InsertCustomer(_customer);
			}
		}
		catch (Exception ex)
		{
			MessageBox.Show($"Error saving customer data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			return;
		}
		finally
		{
			await SaveSaleFile();
		}
	}
	#endregion

	#region Validation
	private void numberTextBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e) =>
		WindowsHelper.ValidateIntegerInput(sender, e);

	private void decimalTextBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e) =>
		WindowsHelper.ValidateDecimalInput(sender, e);
	#endregion

	#region Cart
	private void selectedProductAutoCompleteTextBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (selectedProductAutoCompleteTextBox.SelectedItem is not null)
		{
			var product = (SaleProductCartModel)selectedProductAutoCompleteTextBox.SelectedItem;
			selectedProductRateTextBox.Value = double.Parse(product.Rate.ToString());
			selectedProductQuantityTextBox.Value = 1;
			selectedProductDiscountPercentTextBox.PercentValue = 0;
		}

		else
		{
			selectedProductRateTextBox.Value = 0;
			selectedProductQuantityTextBox.Value = 0;
			selectedProductDiscountPercentTextBox.PercentValue = 0;
		}

		UpdateSelectedProductFinancialDetails();
	}

	private void selectedProductQuantityTextBox_ValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
		UpdateSelectedProductFinancialDetails();

	private void selectedProductRateTextBox_ValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
		UpdateSelectedProductFinancialDetails();

	private void selectedProductDiscountPercentTextBox_PercentValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
		UpdateSelectedProductFinancialDetails();

	private void UpdateSelectedProductFinancialDetails()
	{
		if (selectedProductRateTextBox is null ||
			selectedProductQuantityTextBox is null ||
			selectedProductDiscountPercentTextBox is null)
			return;

		selectedProductQuantityTextBox.Value ??= 0;
		selectedProductDiscountPercentTextBox.PercentValue ??= 0;

		if (selectedProductAutoCompleteTextBox.SelectedItem is null)
			return;

		var product = (SaleProductCartModel)selectedProductAutoCompleteTextBox.SelectedItem;
		var rate = decimal.Parse(selectedProductRateTextBox.Value.ToString());
		var quantity = decimal.Parse(selectedProductQuantityTextBox.Value.ToString());
		var discPercent = decimal.Parse(selectedProductDiscountPercentTextBox.PercentValue.ToString());
		var baseTotal = rate * quantity;
		var discAmount = baseTotal * discPercent / 100;
		var afterDiscount = baseTotal - discAmount;
		var cgstAmount = afterDiscount * product.CGSTPercent / 100;
		var sgstAmount = afterDiscount * product.SGSTPercent / 100;
		var igstAmount = afterDiscount * product.IGSTPercent / 100;
		var totalTax = cgstAmount + sgstAmount + igstAmount;
		var total = afterDiscount + totalTax;

		selectedProductDiscountAmountTextBox.Text = discAmount.FormatIndianCurrency();
		selectedProductTaxTextBox.Text = totalTax.FormatIndianCurrency();
		selectedProductTotalTextBox.Text = total.FormatIndianCurrency();

		selectedProductDiscountAmountTextBox.ToolTip = $"Base Total: {baseTotal.FormatIndianCurrency()}\nDiscount ({discPercent}%): {discAmount.FormatIndianCurrency()}\nAfter Discount: {afterDiscount.FormatIndianCurrency()}";
		selectedProductTaxTextBox.ToolTip = $"CGST ({product.CGSTPercent}%): {cgstAmount.FormatIndianCurrency()}\nSGST ({product.SGSTPercent}%: {sgstAmount.FormatIndianCurrency()}\nIGST ({product.IGSTPercent}%: {igstAmount.FormatIndianCurrency()}";
		selectedProductTotalTextBox.ToolTip = $"Base Total: {baseTotal.FormatIndianCurrency()}\nAfter Discount: {afterDiscount.FormatIndianCurrency()}\nTotal Tax: {totalTax.FormatIndianCurrency()}";
	}

	private async void addProductButton_Click(object sender, RoutedEventArgs e)
	{
		if (selectedProductAutoCompleteTextBox.SelectedItem is null ||
			selectedProductQuantityTextBox.Value is null ||
			selectedProductQuantityTextBox.Value <= 0 ||
			selectedProductDiscountPercentTextBox.PercentValue is null ||
			selectedProductDiscountPercentTextBox.PercentValue < 0 ||
			selectedProductDiscountPercentTextBox.PercentValue > 100 ||
			_isSaving)
			return;

		var existingCartItem = _cart.FirstOrDefault(x => x.ProductId == ((SaleProductCartModel)selectedProductAutoCompleteTextBox.SelectedItem).ProductId);
		if (existingCartItem is not null)
		{
			_cart.FirstOrDefault(x => x.ProductId == existingCartItem.ProductId).Quantity += decimal.Parse(selectedProductQuantityTextBox.Value.ToString());
			selectedProductAutoCompleteTextBox.SelectedItem = null;
			selectedProductQuantityTextBox.Value = 0;
			selectedProductDiscountPercentTextBox.PercentValue = 0;
			await SaveSaleFile();
		}

		var product = (SaleProductCartModel)selectedProductAutoCompleteTextBox.SelectedItem;
		var rate = decimal.Parse(selectedProductRateTextBox.Value.ToString());
		var quantity = decimal.Parse(selectedProductQuantityTextBox.Value.ToString());
		var discPercent = decimal.Parse(selectedProductDiscountPercentTextBox.PercentValue.ToString());

		_cart.Add(new()
		{
			ProductId = product.ProductId,
			ProductName = product.ProductName,
			ProductCategoryId = product.ProductCategoryId,
			Rate = rate,
			Quantity = quantity,
			BaseTotal = 0,
			DiscPercent = discPercent,
			DiscAmount = 0,
			AfterDiscount = 0,
			CGSTPercent = 0,
			CGSTAmount = 0,
			SGSTPercent = 0,
			SGSTAmount = 0,
			IGSTPercent = 0,
			IGSTAmount = 0,
			Total = 0,
			NetRate = 0
		});

		selectedProductAutoCompleteTextBox.SelectedItem = null;
		selectedProductQuantityTextBox.Value = 0;
		selectedProductDiscountPercentTextBox.PercentValue = 0;

		selectedProductAutoCompleteTextBox.Focus();

		await SaveSaleFile();
	}

	private async void cartDataGrid_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
	{
		if (e.OriginalSource is FrameworkElement element && element.DataContext is SaleProductCartModel model)
		{
			_cart.Remove(model);
			selectedProductAutoCompleteTextBox.SelectedItem = model;
			selectedProductQuantityTextBox.Value = double.Parse(model.Quantity.ToString());
			selectedProductRateTextBox.Value = double.Parse(model.Rate.ToString());
			selectedProductDiscountPercentTextBox.PercentValue = double.Parse(model.DiscPercent.ToString());
			UpdateSelectedProductFinancialDetails();
			selectedProductAutoCompleteTextBox.Focus();
		}

		await SaveSaleFile();
	}
	#endregion

	#region Saving
	private async Task UpdateFinancials()
	{
		billNoTextBox.Text = await GenerateCodes.GenerateSaleBillNo(new()
		{
			Id = 0,
			SaleDateTime = saleDateTimePicker.DateTime.Value,
			LocationId = _user.LocationId
		});

		var discountPercent = decimal.Parse(discountPercentTextBox.PercentValue.Value.ToString());

		foreach (var cart in _cart)
		{
			var product = await CommonData.LoadTableDataById<ProductModel>(TableNames.Product, cart.ProductId);
			var productTax = await CommonData.LoadTableDataById<TaxModel>(TableNames.Tax, product.TaxId);

			cart.BaseTotal = cart.Rate * cart.Quantity;
			cart.DiscAmount = cart.BaseTotal * cart.DiscPercent / 100;
			cart.AfterDiscount = cart.BaseTotal - cart.DiscAmount;
			cart.CGSTPercent = productTax.CGST;
			cart.CGSTAmount = cart.AfterDiscount * productTax.CGST / 100;
			cart.SGSTPercent = productTax.SGST;
			cart.SGSTAmount = cart.AfterDiscount * productTax.SGST / 100;
			cart.IGSTPercent = productTax.IGST;
			cart.IGSTAmount = cart.AfterDiscount * productTax.IGST / 100;
			cart.Total = cart.AfterDiscount + cart.CGSTAmount + cart.SGSTAmount + cart.IGSTAmount;
			cart.NetRate = cart.Total / cart.Quantity * (1 - discountPercent / 100);
		}

		var baseTotal = _cart.Sum(x => x.BaseTotal);
		var subTotal = _cart.Sum(x => x.AfterDiscount);
		var productDiscount = baseTotal - subTotal;
		var afterTax = _cart.Sum(x => x.Total);
		var totalTax = afterTax - subTotal;
		var discountAmount = afterTax * discountPercent / 100;
		var roundOff = Math.Round(afterTax - discountAmount) - (afterTax - discountAmount);
		var total = Math.Round(afterTax - discountAmount + roundOff);

		discountAmountTextBox.Text = discountAmount.FormatIndianCurrency();
		baseTotalTextBox.Text = baseTotal.FormatIndianCurrency();
		productDiscountTextBox.Text = productDiscount.FormatIndianCurrency();
		subTotalTextBox.Text = subTotal.FormatIndianCurrency();
		taxAmountTextBox.Text = totalTax.FormatIndianCurrency();
		afterTaxTextBox.Text = afterTax.FormatIndianCurrency();
		roundOffTextBox.Text = roundOff.FormatIndianCurrency();
		totalTextBox.Text = total.FormatIndianCurrency();

		_cart.OrderBy(p => p.ProductName).ToList();
		cartDataGrid.Items.Refresh();
	}

	private async Task SaveSaleFile()
	{
		if (_user is null || _isSaving)
			return;

		_isSaving = true;

		try
		{
			await UpdateFinancials();
			await SaveCustomer();

			await File.WriteAllTextAsync(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), StorageFileNames.SaleDataFileName),
				System.Text.Json.JsonSerializer.Serialize(new SaleModel()
				{
					Id = 0,
					UserId = _user.Id,
					LocationId = _user.LocationId,
					BillNo = billNoTextBox.Text,
					SaleDateTime = saleDateTimePicker.DateTime.Value,
					PartyId = partyAutoCompleteTextBox.SelectedItem is not null ? ((LedgerModel)partyAutoCompleteTextBox.SelectedItem).Id : null,
					OrderId = orderAutoCompleteTextBox.SelectedItem is not null ? ((OrderModel)orderAutoCompleteTextBox.SelectedItem).Id : null,
					DiscPercent = decimal.Parse(discountPercentTextBox.PercentValue.Value.ToString()),
					DiscReason = discountReasonTextBox.Text,
					CustomerId = _customer.Id > 0 ? _customer.Id : null,
					RoundOff = 0,
					Cash = 0,
					Card = 0,
					UPI = 0,
					Credit = 0,
					Remarks = remarksTextBox.Text,
					CreatedAt = DateTime.Now,
					Status = true,
				}));

			await File.WriteAllTextAsync(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), StorageFileNames.SaleCartDataFileName),
				System.Text.Json.JsonSerializer.Serialize(_cart.ToList()));
		}
		catch (Exception ex)
		{
			MessageBox.Show($"Error saving sale data to File: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
		}
		finally
		{
			_isSaving = false;
		}
	}

	private async void saveButton_Click(object sender, RoutedEventArgs e)
	{
		if (_user is null || _isSaving)
			return;

		try
		{
			await SaveSaleFile();

			_isSaving = true;

			var saleModel = new SaleModel()
			{
				Id = 0,
				UserId = _user.Id,
				LocationId = _user.LocationId,
				BillNo = billNoTextBox.Text,
				SaleDateTime = saleDateTimePicker.DateTime.Value,
				PartyId = partyAutoCompleteTextBox.SelectedItem is not null ? ((LedgerModel)partyAutoCompleteTextBox.SelectedItem).Id : null,
				OrderId = orderAutoCompleteTextBox.SelectedItem is not null ? ((OrderModel)orderAutoCompleteTextBox.SelectedItem).Id : null,
				DiscPercent = decimal.Parse(discountPercentTextBox.PercentValue.Value.ToString()),
				DiscReason = discountReasonTextBox.Text,
				CustomerId = _customer.Id > 0 ? _customer.Id : null,
				RoundOff = decimal.Parse(roundOffTextBox.Text.Replace("₹", "").Replace(",", "").Trim()),
				Cash = 0,
				Card = 0,
				UPI = 0,
				Credit = 0,
				Remarks = remarksTextBox.Text,
				CreatedAt = DateTime.Now,
				Status = true,
			};

			var saleId = await SaleData.SaveSale(saleModel, [.. _cart]);
			if (saleId <= 0)
			{
				MessageBox.Show("Error saving sale data.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			var ms = await SaleA4Print.GenerateA4SaleBill(saleId);
			using FileStream stream = new(Path.Combine(Path.GetTempPath(), $"SaleBill{saleId}.pdf"), FileMode.Create, FileAccess.Write);
			ms.Position = 0;
			await ms.CopyToAsync(stream);
			Process.Start(new ProcessStartInfo($"{Path.GetTempPath()}\\SaleBill{saleId}.pdf") { UseShellExecute = true });

			File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), StorageFileNames.SaleDataFileName));
			File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), StorageFileNames.SaleCartDataFileName));

			MessageBox.Show("Sale saved successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
			Close();
		}
		catch (Exception ex)
		{
			MessageBox.Show($"Error saving sale data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
		}
		finally
		{
			_isSaving = false;
		}
	}
	#endregion
}
