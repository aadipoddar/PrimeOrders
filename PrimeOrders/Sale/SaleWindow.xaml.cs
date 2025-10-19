using System.IO;
using System.Windows;

using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Order;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Order;
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

	private List<SaleProductCartModel> _cart = [];

	#region Load Data
	public SaleWindow(UserModel user)
	{
		InitializeComponent();

		_user = user;
	}

	private async void Window_Loaded(object sender, RoutedEventArgs e)
	{
		if (_user.LocationId == 1)
		{
			partyOrderSection.Visibility = Visibility.Visible;

			var parties = await CommonData.LoadTableDataByStatus<LedgerModel>(TableNames.Ledger);
			partyAutoCompleteTextBox.AutoCompleteSource = parties;
			partyAutoCompleteTextBox.ValueMemberPath = nameof(LedgerModel.Id);
			partyAutoCompleteTextBox.SearchItemPath = nameof(LedgerModel.Name);
		}

		saleDateTimePicker.DateTime = DateTime.Now;
		paymentModeComboBox.ItemsSource = PaymentModeData.GetPaymentModes();
		paymentModeComboBox.DisplayMemberPath = nameof(PaymentModeModel.Name);
		paymentModeComboBox.SelectedValuePath = nameof(PaymentModeModel.Id);
		paymentModeComboBox.SelectedIndex = 0;

		await LoadExistingSale();
		await SaveSaleFile();
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
			}
		}
		catch (Exception ex)
		{
			MessageBox.Show($"Error loading existing sale data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), StorageFileNames.SaleDataFileName));
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
		await SaveSaleFile();
	}

	private async void saleDateTimePicker_DateTimeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
		await SaveSaleFile();
	#endregion

	#region Customer Methods
	private async void customerNumberTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
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
		await SaveSaleFile();
	}

	private async Task SaveCustomer()
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

		await SaveSaleFile();
	}
	#endregion

	#region Validation
	private void numberTextBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e) =>
		WindowsHelper.ValidateIntegerInput(sender, e);

	private void decimalTextBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e) =>
		WindowsHelper.ValidateDecimalInput(sender, e);
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

		var baseTotal = _cart.Sum(x => x.BaseTotal);
		var subTotal = _cart.Sum(x => x.AfterDiscount);
		var productDiscount = baseTotal - subTotal;
		var afterTax = _cart.Sum(x => x.Total);
		var totalTax = afterTax - subTotal;
		var discountPercent = decimal.Parse(discountPercentTextBox.PercentValue.Value.ToString());
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
	#endregion

	private async void discountPercentTextBox_PercentValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		discountPercentTextBox.Text = Math.Clamp(discountPercentTextBox.PercentValue.Value, 0, 100).ToString();
		await SaveSaleFile();
	}
}
